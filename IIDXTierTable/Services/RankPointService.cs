using System.Text.Json;

namespace IIDXTierTable.Services;

public sealed class RankPointService
{
    private readonly Dictionary<int, Dictionary<string, int>> pointsByRankTier = new();

    public bool IsInitialized { get; private set; }

    public async Task InitializeAsync(HttpClient http)
    {
        if (IsInitialized)
        {
            return;
        }

        var json = await http.GetStringAsync("rank-points");
        var points = JsonSerializer.Deserialize<Dictionary<int, Dictionary<string, int>>>(json)
            ?? throw new InvalidOperationException("RankPoints.json을 읽을 수 없습니다.");

        pointsByRankTier.Clear();
        foreach (var (rankTier, clearTypePoints) in points)
        {
            pointsByRankTier[rankTier] = new Dictionary<string, int>(clearTypePoints, StringComparer.OrdinalIgnoreCase);
        }

        IsInitialized = true;
    }

    public int GetPoint(string rankTier, string clearType)
    {
        if (!int.TryParse(rankTier.Trim(), out var parsedRankTier)
            || !pointsByRankTier.TryGetValue(parsedRankTier, out var points)
            || !points.TryGetValue(clearType.Trim(), out var point))
        {
            return 0;
        }

        return point;
    }
}
