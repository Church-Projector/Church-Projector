using ChurchProjector.Shared;

namespace ChurchProjector.PowerPoint;

using System.IO.Pipes;
using System.Text;
using System.Text.Json;

class Program
{
    [STAThread]
    static async Task Main()
    {
        var cmdPipe = new NamedPipeServerStream("ppt_cmd", PipeDirection.In);
        var evtPipe = new NamedPipeServerStream("ppt_evt", PipeDirection.Out);

        await cmdPipe.WaitForConnectionAsync();
        await evtPipe.WaitForConnectionAsync();

        var ppt = new PowerPoint
        {
            SlideShowBegin = () => _ = SendAsync(evtPipe, new { type = "SlideShowBegin" }),
            SlideShowEnd = () => _ = SendAsync(evtPipe, new { type = "SlideShowEnd" }),
            SlideShowNextSlide = i =>
                _ = SendAsync(evtPipe, new { type = "SlideShowNextSlide", payload = new { index = i } }),
            ImagesGenerated = files =>
                _ = SendAsync(evtPipe, new { type = "ImagesGenerated", payload = new { files } })
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
                    ppt.SetCurrentSlide(scsc.Payload.Index, scsc.Payload.Left, scsc.Payload.Top,scsc.Payload.Width,scsc.Payload.Height);
                    break;
            }
        }
    }

    static async Task SendAsync(Stream pipe, object obj)
    {
        var json = JsonSerializer.Serialize(obj) + "\n";
        var bytes = Encoding.UTF8.GetBytes(json);
        await pipe.WriteAsync(bytes);
        await pipe.FlushAsync();
    }
}