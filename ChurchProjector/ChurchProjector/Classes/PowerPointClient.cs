using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ChurchProjector.Shared;
using Serilog;

namespace ChurchProjector.Classes;

public class PowerPointClient
{
    public event Action? SlideShowBegin;
    public event Action<int>? SlideShowNextSlide;
    public event Action? SlideShowEnd;
    public event Action<List<string>>? ImagesGenerated;

    private NamedPipeClientStream? _cmd;
    private NamedPipeClientStream? _evt;
    private CancellationTokenSource? _cts;
    private Task? _listenTask;
    private bool _isRunning;

    public bool IsRunning => OperatingSystem.IsWindows()
                             && _cmd is { IsConnected: true }
                             && _evt is { IsConnected: true }
                             && _isRunning;

    public PowerPointClient()
    {
        if (OperatingSystem.IsWindows())
        {
            _ = EnsurePowerPointServiceIsConnected();
        }
    }

    private async Task EnsurePowerPointServiceIsConnected()
    {
        // Maybe it is already running in the background.
        if (await ConnectAsync())
        {
            return;
        }

        EnsureWorkerRunning();

        // Retry loop (service might need time to boot)
        const int maxAttempts = 10;
        const int delayMs = 300;

        for (var i = 0; i < maxAttempts; i++)
        {
            await Task.Delay(delayMs);

            if (await ConnectAsync())
            {
                break;
            }
        }
    }

    private async Task<bool> ConnectAsync()
    {
        _cmd ??= new NamedPipeClientStream(".", "ppt_cmd", PipeDirection.Out);
        _evt ??= new NamedPipeClientStream(".", "ppt_evt", PipeDirection.In);

        var connectionSucceeded = false;
        if (!_cmd.IsConnected)
        {
            connectionSucceeded = await ConnectPipeWithRetry(_cmd);
        }

        if (!connectionSucceeded)
        {
            return false;
        }

        if (!_evt.IsConnected)
        {
            connectionSucceeded = await ConnectPipeWithRetry(_evt);
        }

        if (!connectionSucceeded)
        {
            return false;
        }

        _cts = new CancellationTokenSource();
        _listenTask = Task.Run(() => Listen(_cts.Token));

        await _listenTask.ContinueWith(async t =>
        {
            _listenTask = null;
            if (t.Exception != null)
            {
                Log.Error(t.Exception, "PowerPoint listener failed");
                await StopPowerPointViewerAsync();
                await EnsurePowerPointServiceIsConnected();
            }
        }, TaskContinuationOptions.OnlyOnFaulted);

        return true;
    }

    private static async Task<bool> ConnectPipeWithRetry(NamedPipeClientStream pipe, int retryCount = 3)
    {
        for (int i = 0; i < retryCount; i++)
        {
            try
            {
                await pipe.ConnectAsync(500);
                return true;
            }
            catch (TimeoutException)
            {
                // Ignore
            }
        }

        return false;
    }

    public async Task StartPowerPointViewerAsync(string file)
    {
        if (_cmd is null || !_cmd.IsConnected)
        {
            return;
        }

        await SendAsync(JsonSerializer.Serialize(new GenericMessage<StartPowerPointViewerCommand>(
            Type: "StartPowerPointViewer",
            Payload: new StartPowerPointViewerCommand(file)
        ), Shared.JsonContext.Default.GenericMessageStartPowerPointViewerCommand));

        _isRunning = true;
    }

    private Process? _workerProcess;

    private void EnsureWorkerRunning()
    {
#if DEBUG
        return;
#endif
        if (_workerProcess is { HasExited: false })
        {
            return;
        }

        var exePath = Path.Combine(AppContext.BaseDirectory, "ChurchProjector.PowerPoint.exe");

        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException($"PowerPointWorker not found: {exePath}");
        }

        _workerProcess = Process.Start(new ProcessStartInfo
        {
            FileName = exePath,
            UseShellExecute = false,
            CreateNoWindow = true
        });

        _workerProcess!.EnableRaisingEvents = true;
        _workerProcess.Exited += (_, _) => { _workerProcess = null; };
    }

    public async Task StopPowerPointViewerAsync()
    {
        if (_cmd is not null && _cmd.IsConnected)
        {
            await SendAsync(JsonSerializer.Serialize(
                new GenericMessage<ShutdownCommand>("Shutdown", new ShutdownCommand()),
                Shared.JsonContext.Default.GenericMessageShutdownCommand));
        }

        if (_cts is not null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
            _cts = null;
        }

        if (_listenTask != null)
        {
            await _listenTask;
        }

        if (_cmd is not null)
        {
            await _cmd.DisposeAsync();
            _cmd = null;
        }

        if (_evt is not null)
        {
            await _evt.DisposeAsync();
            _evt = null;
        }

        _isRunning = false;
    }

    private int _isListening;

    private async Task Listen(CancellationToken token)
    {
        if (_evt is not { IsConnected: true })
        {
            throw new InvalidOperationException("Pipe not connected");
        }

        if (Interlocked.Exchange(ref _isListening, 1) == 1)
        {
            return;
        }

        using var reader = new StreamReader(_evt);

        try
        {
            while (!token.IsCancellationRequested)
            {
                var line = await reader.ReadLineAsync(token);
                if (line == null)
                {
                    break;
                }

                if (JsonSerializer.Deserialize(line, typeof(BaseMessage), Shared.JsonContext.Default) is not BaseMessage
                    msg)
                {
                    continue;
                }

                switch (msg.Type)
                {
                    case "SlideShowBegin":
                        SlideShowBegin?.Invoke();
                        break;

                    case "SlideShowEnd":
                        SlideShowEnd?.Invoke();
                        break;

                    case "SlideShowNextSlide":
                    {
                        if (JsonSerializer.Deserialize(line, typeof(GenericMessage<SlideShowNextSlidePayload>),
                                Shared.JsonContext.Default) is not GenericMessage<SlideShowNextSlidePayload> ssnsp)
                        {
                            continue;
                        }

                        SlideShowNextSlide?.Invoke(ssnsp.Payload.Index);
                        break;
                    }

                    case "ImagesGenerated":
                    {
                        if (JsonSerializer.Deserialize(line, typeof(GenericMessage<ImagesGeneratedPayload>),
                                Shared.JsonContext.Default) is not GenericMessage<ImagesGeneratedPayload> igp)
                        {
                            continue;
                        }

                        Dispatcher.UIThread.Invoke(() => ImagesGenerated?.Invoke(igp.Payload.Files));
                        break;
                    }
                }
            }
        }
        finally
        {
            Interlocked.Exchange(ref _isListening, 0);
        }
    }

    public async Task HidePresentationAsync()
    {
        if (!OperatingSystem.IsWindows() || _cmd is not { IsConnected: true } || _evt is not { IsConnected: true })
        {
            return;
        }

        await SendAsync(JsonSerializer.Serialize(new GenericMessage<HidePresentationCommand>(
            Type: "HidePresentation",
            Payload: new HidePresentationCommand()
        ), Shared.JsonContext.Default.GenericMessageHidePresentationCommand));
    }

    public async Task ClosePresentationAsync()
    {
        if (!OperatingSystem.IsWindows() || _cmd is not { IsConnected: true } || _evt is not { IsConnected: true })
        {
            return;
        }

        await SendAsync(JsonSerializer.Serialize(new GenericMessage<ClosePresentationCommand>(
            Type: "ClosePresentation",
            Payload: new ClosePresentationCommand()
        ), Shared.JsonContext.Default.GenericMessageClosePresentationCommand));

        _isRunning = false;
    }

    public async Task ImagesSetAsync()
    {
        if (!OperatingSystem.IsWindows() || _cmd is not { IsConnected: true } || _evt is not { IsConnected: true })
        {
            return;
        }

        await SendAsync(JsonSerializer.Serialize(new GenericMessage<ImagesSetCommand>(
            Type: "ImagesSet",
            Payload: new ImagesSetCommand()
        ), Shared.JsonContext.Default.GenericMessageImagesSetCommand));
        
        _isRunning = true;
    }

    public async Task SetCurrentSlideAsync(int index, float left, float top, float width, float height)
    {
        if (!OperatingSystem.IsWindows() || _cmd is not { IsConnected: true } || _evt is not { IsConnected: true })
        {
            return;
        }

        await SendAsync(JsonSerializer.Serialize(new GenericMessage<SetCurrentSlideCommand>(
            Type: "SetCurrentSlide",
            Payload: new SetCurrentSlideCommand(index, left, top, width, height)
        ), Shared.JsonContext.Default.GenericMessageSetCurrentSlideCommand));
    }

    private async Task SendAsync(string message)
    {
        var json = message + "\n";
        var bytes = Encoding.UTF8.GetBytes(json);

        if (_cmd is not { IsConnected: true })
        {
            throw new InvalidOperationException("Pipe not connected");
        }

        await _cmd.WriteAsync(bytes);
        await _cmd.FlushAsync();
    }
}