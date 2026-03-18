using System.Collections.Generic;
using System.Text.Json.Serialization;
using static ChurchProjector.Classes.Version;

namespace ChurchProjector.Classes;
[JsonSourceGenerationOptions(WriteIndented = true)]
[JsonSerializable(typeof(JsonFile))]
[JsonSerializable(typeof(List<Versions>))]
public partial class JsonContext : JsonSerializerContext
{
}