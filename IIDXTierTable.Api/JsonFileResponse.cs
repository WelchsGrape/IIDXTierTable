using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace IIDXTierTable.Api;

internal static class JsonFileResponse
{
    private static readonly ConcurrentDictionary<string, CachedJson> JsonCache = new(StringComparer.OrdinalIgnoreCase);

    public static async Task<IActionResult> CreateAsync(
        string fileName,
        HttpRequest request,
        ILogger logger,
        string dataDescription)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Data", fileName);

        if (!File.Exists(path))
        {
            logger.LogError("{DataDescription} 파일을 찾을 수 없습니다: {Path}", dataDescription, path);
            return new NotFoundObjectResult(new { error = "DATA_NOT_FOUND" });
        }

        try
        {
            var cachedJson = await GetCachedJsonAsync(path);
            request.HttpContext.Response.Headers.CacheControl = "public, max-age=300, must-revalidate";
            request.HttpContext.Response.Headers.ETag = cachedJson.ETag;
            request.HttpContext.Response.Headers["X-Data-Version"] = cachedJson.Version;

            if (string.Equals(request.Headers.IfNoneMatch.ToString(), cachedJson.ETag, StringComparison.Ordinal))
            {
                return new StatusCodeResult(StatusCodes.Status304NotModified);
            }

            return new ContentResult
            {
                Content = cachedJson.Json,
                ContentType = "application/json; charset=utf-8",
                StatusCode = StatusCodes.Status200OK
            };
        }
        catch (IOException exception)
        {
            logger.LogError(exception, "{DataDescription} 파일을 읽을 수 없습니다.", dataDescription);
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    private static async Task<CachedJson> GetCachedJsonAsync(string path)
    {
        if (JsonCache.TryGetValue(path, out var cachedJson))
        {
            return cachedJson;
        }

        var json = await File.ReadAllTextAsync(path);
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(json))).ToLowerInvariant();
        cachedJson = new CachedJson(json, $"\"{hash}\"", hash[..16]);
        return JsonCache.GetOrAdd(path, cachedJson);
    }

    private sealed record CachedJson(string Json, string ETag, string Version);
}
