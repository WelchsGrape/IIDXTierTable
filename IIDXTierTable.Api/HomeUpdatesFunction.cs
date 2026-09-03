using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace IIDXTierTable.Api;

public sealed class HomeUpdatesFunction(ILogger<HomeUpdatesFunction> logger)
{
    [Function("GetHomeUpdates")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "home-updates")] HttpRequest request)
    {
        return await JsonFileResponse.CreateAsync(
            "HomeUpdates.json",
            request,
            logger,
            "Home 업데이트 데이터");
    }
}
