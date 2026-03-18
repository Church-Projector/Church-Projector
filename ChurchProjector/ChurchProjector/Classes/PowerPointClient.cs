using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
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
    public bool IsRunning => OperatingSystem.IsWindows() && _cmd is { IsConnected: true } && _evt is { IsConnected: true };

    public async Task ConnectAsync()
    {
        if (OperatingSystem.IsWindows())
        {
            EnsureWorkerRunning();
        }

        if (_cmd is null)
        {
            _cmd = new NamedPipeClientStream(".", "ppt_cmd", PipeDirection.Out);
        }

        if (_evt is null)
        {
            _evt = new NamedPipeClientStream(".", "ppt_evt", PipeDirection.In);
        }

        if (!_cmd.IsConnected)
        {
            await ConnectPipeWithRetry(_cmd);
        }

        if (!_evt.IsConnected)
        {
            await ConnectPipeWithRetry(_evt);
        }

        _cts = new CancellationTokenSource();
        _listenTask = Task.Run(() => Listen(_cts.Token));

        await _listenTask.ContinueWith(t =>
        {
            if (t.Exception != null)
            {
                Log.Error(t.Exception, "PowerPoint listener failed");
            }
        }, TaskContinuationOptions.OnlyOnFaulted);
    }

    private async Task ConnectPipeWithRetry(NamedPipeClientStream pipe, int timeoutMs = 5000)
    {
        var start = DateTime.UtcNow;

        while (true)
        {
            try
            {
                await pipe.ConnectAsync(500);
                return;
            }
            catch (TimeoutException)
            {
                if ((DateTime.UtcNow - start).TotalMilliseconds > timeoutMs)
                    throw;
            }
        }
    }

    public async Task StartPowerPointViewerAsync(string file)
    {
        if (_cmd is null || !_cmd.IsConnected)
        {
            await ConnectAsync();
        }

        await SendAsync(JsonSerializer.Serialize(new GenericMessage<StartPowerPointViewerCommand>(
            Type: "StartPowerPointViewer",
            Payload: new StartPowerPointViewerCommand(file)
        ), Shared.JsonContext.Default.GenericMessageStartPowerPointViewerCommand));
    }

    private Process? _workerProcess;

    private void EnsureWorkerRunning()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        if (_workerProcess is { HasExited: false })
            return;

        var exePath = Path.Combine(AppContext.BaseDirectory, "PowerPointWorker.exe");

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
    }

    private int _isListening;

    private async Task Listen(CancellationToken token)
    {
        if (_evt is not { IsConnected: true })
        {
            throw new InvalidOperationException("Pipe not connected");
        }

        var reader = new StreamReader(_evt, Encoding.UTF8, leaveOpen: true);
        if (Interlocked.Exchange(ref _isListening, 1) == 1)
        {
            return;
        }

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

                        ImagesGenerated?.Invoke(igp.Payload.Files);
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

    public async Task SetCurrentSlideAsync(int index, float left, float top, float width, float height)
    {
        if (!OperatingSystem.IsWindows() || _cmd is not { IsConnected: true } || _evt is not { IsConnected: true })
        {
            return;
        }

        await SendAsync(JsonSerializer.Serialize(new GenericMessage<SetCurrentSlideCommand>(
            Type: "SetCurrentSlide",
            Payload: new SetCurrentSlideCommand(index, left, top, width, height)
        ), Shared.JsonContext.Default.GenericMessageHidePresentationCommand));
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