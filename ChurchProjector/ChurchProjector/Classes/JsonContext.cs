using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChurchProjector.Classes;
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(JsonFile))]
[JsonSerializable(typeof(List<WebsiteRelease>))]
[JsonSerializable(typeof(ErrorReportRequest))]
[JsonSerializable(typeof(WebsiteMessage))]
public partial class JsonContext : JsonSerializerContext
{
}
