using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using IIDXTierTable.Services;
using Xunit;

namespace IIDXTierTable.Tests;

public sealed class TierTableDataServiceTests
{
    [Fact]
    public async Task InitializeAsync_LoadsRows_FiltersBlankTitlesAndCountsRankOneRows()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, """
            [
              { "Title": "Song 1", "RankTier": "1" },
              { "Title": "Song 2", "RankTier": "2" },
              { "Title": "   ", "RankTier": "1" },
              { "Title": "Song 3", "RankTier": " 1 " }
            ]
            """);
        var service = new TierTableDataService();

        await service.InitializeAsync(CreateClient(handler));

        Assert.True(service.IsInitialized);
        Assert.Null(service.ErrorMessage);
        Assert.Equal(["Song 1", "Song 2", "Song 3"], service.Rows.Select(row => row.Title));
        Assert.Equal(1, service.CurrentRankCount);
    }

    [Fact]
    public async Task InitializeAsync_WhenRequestFails_ClearsStateAndKeepsServiceUninitialized()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.InternalServerError, "error");
        var service = new TierTableDataService();

        await service.InitializeAsync(CreateClient(handler));

        Assert.False(service.IsInitialized);
        Assert.Empty(service.Rows);
        Assert.Equal(0, service.CurrentRankCount);
        Assert.False(string.IsNullOrWhiteSpace(service.ErrorMessage));
    }

    [Fact]
    public async Task InitializeAsync_DoesNotRequestDataMoreThanOnceAfterSuccess()
    {
        var handler = new StubHttpMessageHandler(HttpStatusCode.OK, "[]");
        var service = new TierTableDataService();
        using var http = CreateClient(handler);

        await service.InitializeAsync(http);
        await service.InitializeAsync(http);

        Assert.Equal(1, handler.RequestCount);
    }

    private static HttpClient CreateClient(StubHttpMessageHandler handler)
        => new(handler) { BaseAddress = new Uri("https://example.test/") };

    private sealed class StubHttpMessageHandler(HttpStatusCode statusCode, string content) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(statusCode)
            {
                Content = new StringContent(content)
            });
        }
    }
}
