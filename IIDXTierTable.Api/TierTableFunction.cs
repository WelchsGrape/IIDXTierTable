using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace IIDXTierTable.Api;

public sealed class TierTableFunction
{
    private readonly ILogger<TierTableFunction> _logger;

    public TierTableFunction(ILogger<TierTableFunction> logger)
    {
        _logger = logger;
    }

    [Function("GetTierTable")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tier-table")] HttpRequest request)
    {
        return await JsonFileResponse.CreateAsync(
            "SP12TierData.json",
            _logger,
            "서열표 데이터");
    }
}
