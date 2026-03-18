namespace ChurchProjector.Shared;

using System.Text.Json.Serialization;

[JsonSerializable(typeof(GenericMessage<StartPowerPointViewerCommand>))]
[JsonSerializable(typeof(GenericMessage<ShutdownCommand>))]
[JsonSerializable(typeof(GenericMessage<HidePresentationCommand>))]
[JsonSerializable(typeof(GenericMessage<SlideShowNextSlidePayload>))]
[JsonSerializable(typeof(GenericMessage<SlideShowBeginPayload>))]
[JsonSerializable(typeof(GenericMessage<SlideShowEndPayload>))]
[JsonSerializable(typeof(GenericMessage<ImagesGeneratedPayload>))]
[JsonSerializable(typeof(BaseMessage))]
public partial class JsonContext : JsonSerializerContext { }

public record BaseMessage(string Type);
public record GenericMessage<TPayload>(string Type, TPayload Payload);

public record StartPowerPointViewerCommand(string File);
public record ShutdownCommand();
public record HidePresentationCommand();
public record SetCurrentSlideCommand(int Index, float Left, float Top, float Width, float Height);

public record SlideShowBeginPayload();
public record SlideShowEndPayload();
public record SlideShowNextSlidePayload(int Index);
public record ImagesGeneratedPayload(List<string> Files);
