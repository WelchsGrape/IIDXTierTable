using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace IIDXTierTable.Api;

public sealed class RankPointsFunction
{
    private readonly ILogger<RankPointsFunction> _logger;

    public RankPointsFunction(ILogger<RankPointsFunction> logger)
    {
        _logger = logger;
    }

    [Function("GetRankPoints")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "rank-points")] HttpRequest request)
    {
        return await JsonFileResponse.CreateAsync(
            "RankPoints.json",
            _logger,
            "랭크 포인트 데이터");
    }
}
