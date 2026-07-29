using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace ChurchProjector.Classes;

public static class WebsiteService
{
    public const string BaseUrl = "https://church-projector.de";
    public const string ChangelogUrl = $"{BaseUrl}/Changelog/";

    private static readonly HttpClient Client = new()
    {
        BaseAddress = new Uri(BaseUrl),
        Timeout = TimeSpan.FromSeconds(15)
    };

    public static async Task<WebsiteRelease?> GetNewestReleaseAsync(
        CancellationToken cancellationToken = default)
    {
        using HttpResponseMessage response =
            await Client.GetAsync("/api/changelog.json", cancellationToken);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync(cancellationToken);
        List<WebsiteRelease>? releases = JsonSerializer.Deserialize(
            json,
            JsonContext.Default.ListWebsiteRelease);

        return releases?.FirstOrDefault();
    }

    public static async Task<WebsiteRequestResult> SubmitErrorReportAsync(
        ErrorReportRequest report,
        CancellationToken cancellationToken = default)
    {
        string json = JsonSerializer.Serialize(
            report,
            JsonContext.Default.ErrorReportRequest);
        using StringContent content = new(json, Encoding.UTF8, "application/json");
        using HttpResponseMessage response =
            await Client.PostAsync("/api/error-report", content, cancellationToken);

        return await ReadRequestResultAsync(response, cancellationToken);
    }

    public static async Task<WebsiteRequestResult> SubmitFeatureRequestAsync(
        ErrorReportRequest request,
        CancellationToken cancellationToken = default)
    {
        using MultipartFormDataContent content = new()
        {
            { new StringContent("feature"), "category" },
            { new StringContent(string.Empty), "name" },
            { new StringContent(request.ContactEmail), "email" },
            { new StringContent(request.Subject), "subject" },
            { new StringContent(request.Version), "version" },
            { new StringContent(request.Platform), "platform" },
            { new StringContent(request.Description), "description" },
            { new StringContent("accepted"), "privacy" },
            { new StringContent(string.Empty), "website" }
        };
        using HttpResponseMessage response =
            await Client.PostAsync("/api/contact", content, cancellationToken);

        return await ReadRequestResultAsync(response, cancellationToken);
    }

    private static async Task<WebsiteRequestResult> ReadRequestResultAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        string responseJson =
            await response.Content.ReadAsStringAsync(cancellationToken);
        WebsiteMessage? message = null;
        try
        {
            message = JsonSerializer.Deserialize(
                responseJson,
                JsonContext.Default.WebsiteMessage);
        }
        catch (JsonException)
        {
            // The status code still provides a useful result if the website
            // returns an unexpected error page.
        }

        return new WebsiteRequestResult(
            response.IsSuccessStatusCode,
            message?.Message);
    }
}

public sealed class WebsiteRelease
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("publishedAt")]
    public string PublishedAt { get; set; } = string.Empty;

    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("body")]
    public string Body { get; set; } = string.Empty;
}

public sealed class ErrorReportRequest
{
    [JsonPropertyName("subject")]
    public string Subject { get; init; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; init; } = string.Empty;

    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("platform")]
    public string Platform { get; init; } = string.Empty;

    [JsonPropertyName("contactEmail")]
    public string ContactEmail { get; init; } = string.Empty;
}

public sealed class WebsiteMessage
{
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
}

public readonly record struct WebsiteRequestResult(bool IsSuccess, string? Message);
