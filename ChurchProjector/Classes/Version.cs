using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ChurchProjector.Classes;
public static class Version
{
    public static string? GetCurrentVersion()
    {
        return FileVersionInfo.GetVersionInfo(Path.Combine(AppContext.BaseDirectory, "ChurchProjector.exe")).FileVersion;
    }

    public static async Task<string?> GetNewestVersionStringAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            string url = "https://api.github.com/repos/Church-Projector/Church-Projector/releases";
            using HttpClient client = new HttpClient();
            client.DefaultRequestHeaders.Add("User-Agent", "Church-Projector");
            HttpResponseMessage response = await client.GetAsync(url, cancellationToken);
            string json = await response.Content.ReadAsStringAsync(cancellationToken);
            List<Versions>? result = JsonSerializer.Deserialize(json, typeof(List<Versions>), JsonContext.Default) as List<Versions>;
            if (result is not null)
            {
                return GetNewestVersion(result.ConvertAll(x => x.TagName));
            }
        }
        catch
        {
            // Ignore
        }
        return null;
    }
    private static string? GetNewestVersion(IEnumerable<string> versions)
    {
        if (!versions.Any())
        {
            return null;
        }
        if (versions.Count() == 1)
        {
            return versions.First();
        }

        int newest = versions.Select(x => x.Split(".", 2, StringSplitOptions.RemoveEmptyEntries)[0]).Max(int.Parse);

        return $"{newest}.{GetNewestVersion(versions.Where(x => x.StartsWith($"{newest}.")).Select(x => x.Split(".", 2)[1]))}".TrimEnd('.');
    }

    public class Versions
    {
        [JsonPropertyName("tag_name")]
        public string TagName { get; set; }
    }
}
