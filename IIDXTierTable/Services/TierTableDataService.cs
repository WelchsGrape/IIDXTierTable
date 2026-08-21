using System.Net.Http.Json;

namespace IIDXTierTable.Services;

public sealed class TierTableDataService
{
    public IReadOnlyList<TierTableTitleRow> Rows { get; private set; } = [];

    public int CurrentRankCount { get; private set; }

    public bool IsInitialized { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task InitializeAsync(HttpClient http)
    {
        if (IsInitialized)
        {
            return;
        }

        try
        {
            var rows = await http.GetFromJsonAsync<List<TierTableTitleRow>>("SP12TierData.json");
            Rows = [.. (rows ?? []).Where(row => !string.IsNullOrWhiteSpace(row.Title))];
            CurrentRankCount = Rows.Count(row => string.Equals(row.RankTier, "1", StringComparison.Ordinal));
            ErrorMessage = null;
            IsInitialized = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Rows = [];
            CurrentRankCount = 0;
            IsInitialized = false;
        }
    }

}

public sealed class TierTableTitleRow
{
    public string Version { get; init; } = string.Empty;

    public string Title { get; init; } = string.Empty;

    public string MatchTitle { get; init; } = string.Empty;

    public string Difficulty { get; init; } = string.Empty;

    public string NormalType { get; init; } = string.Empty;

    public string NormalTier { get; init; } = string.Empty;

    public string HardType { get; init; } = string.Empty;

    public string HardTier { get; init; } = string.Empty;

    public string RankTier { get; init; } = string.Empty;
}
