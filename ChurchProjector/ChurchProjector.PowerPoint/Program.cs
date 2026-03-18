using ChurchProjector.Shared;

namespace ChurchProjector.PowerPoint;

using System.IO.Pipes;
using System.Text;
using System.Text.Json;

static class Program
{
    [STAThread]
    async static Task Main()
    {
        NamedPipeServerStream cmdPipe = new("ppt_cmd", PipeDirection.In);
        NamedPipeServerStream evtPipe = new("ppt_evt", PipeDirection.Out);

        await cmdPipe.WaitForConnectionAsync();
        await evtPipe.WaitForConnectionAsync();

        var ppt = new PowerPoint
        {
            SlideShowBegin = () => _ = SendAsync(evtPipe, JsonSerializer.Serialize(
                new GenericMessage<SlideShowBeginPayload>(
                    Type: "SlideShowBegin",
                    Payload: new SlideShowBeginPayload()
                ), JsonContext.Default.GenericMessageSlideShowBeginPayload)),
            SlideShowEnd = () => _ = SendAsync(evtPipe, JsonSerializer.Serialize(
                new GenericMessage<SlideShowEndPayload>(
                    Type: "SlideShowEnd",
                    Payload: new SlideShowEndPayload()
                ), JsonContext.Default.GenericMessageSlideShowEndPayload)),
            SlideShowNextSlide = i =>
                _ = SendAsync(evtPipe, JsonSerializer.Serialize(new GenericMessage<SlideShowNextSlidePayload>(
                    Type: "SlideShowNextSlide",
                    Payload: new SlideShowNextSlidePayload(i)
                ), JsonContext.Default.GenericMessageSlideShowNextSlidePayload)),
            ImagesGenerated = files =>
                _ = SendAsync(evtPipe, JsonSerializer.Serialize(new GenericMessage<ImagesGeneratedPayload>(
                    Type: "ImagesGenerated",
                    Payload: new ImagesGeneratedPayload(files)
                ), JsonContext.Default.GenericMessageImagesGeneratedPayload))
        };

        using var reader = new StreamReader(cmdPipe);

        while (true)
        {
            var line = await reader.ReadLineAsync();
            if (line == null) break;

            if (JsonSerializer.Deserialize(line, typeof(BaseMessage), JsonContext.Default) is not BaseMessage
                msg)
            {
                continue;
            }

            switch (msg.Type)
            {
                case "StartPowerPointViewer":
                    if (JsonSerializer.Deserialize(line, typeof(GenericMessage<StartPowerPointViewerCommand>),
                            JsonContext.Default) is not GenericMessage<StartPowerPointViewerCommand> sppvc)
                    {
                        continue;
                    }

                    ppt.StartPowerPointViewer(sppvc.Payload.File);
                    break;
                case "Shutdown":
                    ppt.Stop();
                    Environment.Exit(0);
                    break;
                case "HidePresentation":
                    ppt.HidePresentationAsync();
                    break;
                case "SetCurrentSlide":
                    if (JsonSerializer.Deserialize(line, typeof(GenericMessage<SetCurrentSlideCommand>),
                            JsonContext.Default) is not GenericMessage<SetCurrentSlideCommand> scsc)
                    {
                        continue;
                    }

                    ppt.SetCurrentSlide(scsc.Payload.Index, scsc.Payload.Left, scsc.Payload.Top, scsc.Payload.Width,
                        scsc.Payload.Height);
                    break;
            }
        }

        async Task SendAsync(Stream pipe, string value)
        {
            var json = value + "\n";
            var bytes = Encoding.UTF8.GetBytes(json);

            if (evtPipe is not { IsConnected: true })
            {
                throw new InvalidOperationException("Pipe not connected");
            }

            await pipe.WriteAsync(bytes);
            await pipe.FlushAsync();
        }
    }
}