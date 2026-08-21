using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace IIDXTierTable.Api;

internal static class JsonFileResponse
{
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
            var json = await File.ReadAllTextAsync(path);
            request.HttpContext.Response.Headers.CacheControl = "public, max-age=300";
            return new ContentResult
            {
                Content = json,
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
}
