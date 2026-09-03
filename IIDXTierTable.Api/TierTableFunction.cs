using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace IIDXTierTable.Api;

public sealed class TierTableFunction(ILogger<TierTableFunction> logger)
{
    [Function("GetTierTable")]
    public async Task<IActionResult> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tier-table")] HttpRequest request)
    {
        return await JsonFileResponse.CreateAsync(
            "SP12TierData.json",
            request,
            logger,
            "서열표 데이터");
    }
}
