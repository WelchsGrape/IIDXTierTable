using System.Net;
using System.Net.Http;
using IIDXTierTable.Services;
using Xunit;

namespace IIDXTierTable.Tests;

public sealed class RankPointServiceTests
{
    [Fact]
    public async Task InitializeAsync_LoadsPointsAndGetPointIsCaseInsensitive()
    {
        var handler = new StubHttpMessageHandler("{\"1\":{\"HARD CLEAR\":100,\"EX HARD CLEAR\":200}}");
        var service = new RankPointService();

        await service.InitializeAsync(new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") });

        Assert.True(service.IsInitialized);
        Assert.Equal(100, service.GetPoint(" 1 ", "hard clear"));
        Assert.Equal(200, service.GetPoint("1", "EX HARD CLEAR"));
    }

    [Fact]
    public async Task InitializeAsync_DoesNotRequestPointsMoreThanOnce()
    {
        var handler = new StubHttpMessageHandler("{\"1\":{\"CLEAR\":100}}");
        var service = new RankPointService();
        using var http = new HttpClient(handler) { BaseAddress = new Uri("https://example.test/") };

        await service.InitializeAsync(http);
        await service.InitializeAsync(http);

        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public void GetPoint_ReturnsZeroForUnknownValuesOrBeforeInitialization()
    {
        var service = new RankPointService();

        Assert.Equal(0, service.GetPoint("1", "CLEAR"));
        Assert.Equal(0, service.GetPoint("invalid", "CLEAR"));
    }

    private sealed class StubHttpMessageHandler : HttpMessageHandler
    {
        private readonly string response;

        public StubHttpMessageHandler(string response)
        {
            this.response = response;
        }

        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(response)
            });
        }
    }
}
