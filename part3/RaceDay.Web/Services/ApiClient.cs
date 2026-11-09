using System.Net.Http.Headers;
using System.Text.Json;

namespace RaceDay.Web.Services;

public class ApiResult<T>
{
    public bool Success { get; init; }
    public T? Data { get; init; }
    public int StatusCode { get; init; }
    public string Message { get; init; } = string.Empty;
    public List<string> Errors { get; init; } = new();
    public bool IsUnauthorized => StatusCode == 401;
}

public record FileData(byte[] Bytes, string ContentType);

/// <summary>
/// The only way the MVC app reaches data. It calls the RaceDay API and forwards the
/// user's API session cookie, which is kept in the MVC session.
/// </summary>
public class ApiClient(HttpClient http, IHttpContextAccessor accessor)
{
    private const string ApiCookieName = "RaceDay.Session";
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);

    private ISession Session => accessor.HttpContext!.Session;

    public Task<ApiResult<T>> GetAsync<T>(string url) =>
        SendAsync<T>(HttpMethod.Get, url, null, ReadJson<T>);

    public Task<ApiResult<T>> PostAsync<T>(string url, object? body) =>
        SendAsync<T>(HttpMethod.Post, url, JsonBody(body), ReadJson<T>);

    public Task<ApiResult<T>> PutAsync<T>(string url, object? body) =>
        SendAsync<T>(HttpMethod.Put, url, JsonBody(body), ReadJson<T>);

    public Task<ApiResult<bool>> PostNoContentAsync(string url) =>
        SendAsync<bool>(HttpMethod.Post, url, null, _ => Task.FromResult(true));

    public Task<ApiResult<bool>> DeleteAsync(string url) =>
        SendAsync<bool>(HttpMethod.Delete, url, null, _ => Task.FromResult(true));

    public Task<ApiResult<T>> PostFileAsync<T>(string url, IFormFile file)
    {
        var form = new MultipartFormDataContent();
        var fileContent = new StreamContent(file.OpenReadStream());
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        form.Add(fileContent, "file", file.FileName);
        return SendAsync<T>(HttpMethod.Post, url, form, ReadJson<T>);
    }

    public Task<ApiResult<FileData>> GetFileAsync(string url) =>
        SendAsync<FileData>(HttpMethod.Get, url, null, async response => new FileData(
            await response.Content.ReadAsByteArrayAsync(),
            response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream"));

    private static HttpContent? JsonBody(object? body) =>
        body is null ? null : JsonContent.Create(body, body.GetType(), null, Json);

    private static async Task<T> ReadJson<T>(HttpResponseMessage response) =>
        (await response.Content.ReadFromJsonAsync<T>(Json))!;

    private async Task<ApiResult<T>> SendAsync<T>(
        HttpMethod method, string url, HttpContent? content, Func<HttpResponseMessage, Task<T>> read)
    {
        using var request = new HttpRequestMessage(method, url) { Content = content };

        var apiCookie = Session.GetString(SessionKeys.ApiCookie);
        if (!string.IsNullOrEmpty(apiCookie))
            request.Headers.TryAddWithoutValidation("Cookie", $"{ApiCookieName}={apiCookie}");

        HttpResponseMessage response;
        try
        {
            response = await http.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            const string message = "We couldn't reach the RaceDay service. Please try again in a moment.";
            return new ApiResult<T> { Success = false, StatusCode = 0, Message = message, Errors = { message } };
        }

        using (response)
        {
            CaptureSessionCookie(response);

            if (response.IsSuccessStatusCode)
            {
                return new ApiResult<T>
                {
                    Success = true,
                    StatusCode = (int)response.StatusCode,
                    Data = await read(response)
                };
            }

            var errors = await ReadErrorsAsync(response);
            return new ApiResult<T>
            {
                Success = false,
                StatusCode = (int)response.StatusCode,
                Message = errors[0],
                Errors = errors
            };
        }
    }

    /// <summary>Keeps the API's session cookie in the MVC session (and drops it on logout).</summary>
    private void CaptureSessionCookie(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Set-Cookie", out var headers)) return;

        var prefix = ApiCookieName + "=";
        foreach (var header in headers)
        {
            if (!header.StartsWith(prefix, StringComparison.Ordinal)) continue;

            var value = header[prefix.Length..];
            var end = value.IndexOf(';');
            if (end >= 0) value = value[..end];

            if (string.IsNullOrEmpty(value))
                Session.Remove(SessionKeys.ApiCookie);
            else
                Session.SetString(SessionKeys.ApiCookie, value);
        }
    }

    private static async Task<List<string>> ReadErrorsAsync(HttpResponseMessage response)
    {
        var errors = new List<string>();
        try
        {
            var text = await response.Content.ReadAsStringAsync();
            if (!string.IsNullOrWhiteSpace(text))
            {
                using var document = JsonDocument.Parse(text);
                var root = document.RootElement;
                if (root.ValueKind == JsonValueKind.Object)
                {
                    if (root.TryGetProperty("message", out var message) && message.ValueKind == JsonValueKind.String)
                    {
                        errors.Add(message.GetString()!);
                    }
                    else if (root.TryGetProperty("errors", out var fieldErrors) && fieldErrors.ValueKind == JsonValueKind.Object)
                    {
                        foreach (var field in fieldErrors.EnumerateObject())
                            foreach (var item in field.Value.EnumerateArray())
                                if (item.GetString() is { } detail) errors.Add(detail);
                    }
                }
            }
        }
        catch (Exception ex) when (ex is JsonException or InvalidOperationException)
        {
            // Fall through to the friendly default below.
        }

        if (errors.Count == 0) errors.Add(DefaultMessage((int)response.StatusCode));
        return errors;
    }

    private static string DefaultMessage(int status) => status switch
    {
        401 => "Please log in to continue.",
        403 => "You don't have permission to do that.",
        404 => "We couldn't find what you were looking for.",
        409 => "That conflicts with something that already exists.",
        503 => "That feature isn't available right now.",
        >= 500 => "Something went wrong on our side. Please try again.",
        _ => "That request couldn't be completed."
    };
}
