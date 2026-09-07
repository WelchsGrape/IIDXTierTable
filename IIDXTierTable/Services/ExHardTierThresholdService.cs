using System.Net.Http.Json;
using Microsoft.AspNetCore.Components;

namespace IIDXTierTable.Services;

public sealed class ExHardTierThresholdService
{
    private readonly bool useLocalData;
    private IReadOnlyList<ExHardTierThreshold> thresholds = [];

    private readonly NavigationManager navigation;

    public ExHardTierThresholdService(IConfiguration configuration, NavigationManager navigation)
    {
        useLocalData = configuration.GetValue<bool>("UseLocalData");
        this.navigation = navigation;
    }

    public IReadOnlyList<ExHardTierThreshold> Thresholds => thresholds;

    public bool IsInitialized { get; private set; }

    public async Task InitializeAsync(HttpClient http)
    {
        if (IsInitialized)
        {
            return;
        }

        var dataUri = useLocalData
            ? new Uri(new Uri(navigation.BaseUri), "data/ExHardTierThresholds.json")
            : new Uri("ex-hard-tier-thresholds", UriKind.Relative);
        var values = await http.GetFromJsonAsync<Dictionary<string, double>>(dataUri)
            ?? throw new InvalidOperationException("EX HARD 서열 기준 데이터를 읽을 수 없습니다.");

        thresholds = values
            .Select(pair => new ExHardTierThreshold(pair.Key, pair.Value))
            .OrderByDescending(threshold => threshold.MinimumPoint)
            .ToArray();

        IsInitialized = true;
    }

    public string GetTier(double point)
    {
        foreach (var threshold in thresholds)
        {
            if (point >= threshold.MinimumPoint)
            {
                return threshold.Tier;
            }
        }

        return thresholds.LastOrDefault()?.Tier ?? string.Empty;
    }
}

public sealed record ExHardTierThreshold(string Tier, double MinimumPoint);
