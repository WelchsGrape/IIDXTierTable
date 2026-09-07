using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace IIDXTierTable.Api;

public sealed class ExHardTierThresholdsFunction(ILogger<ExHardTierThresholdsFunction> logger)
{
    [Function("GetExHardTierThresholds")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "ex-hard-tier-thresholds")] HttpRequest request)
    {
        return await JsonFileResponse.CreateAsync(
            "ExHardTierThresholds.json",
            request,
            logger,
            "EX HARD 서열 기준 데이터");
    }
}
