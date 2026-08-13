using System.Text;

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
            var csv = await http.GetStringAsync("SP12TierData.csv");
            Rows = [.. ParseCsv(csv).Where(row => !string.IsNullOrWhiteSpace(row.Title))];
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

    private static List<TierTableTitleRow> ParseCsv(string csv)
    {
        var rows = new List<TierTableTitleRow>();
        var lines = csv.Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        if (lines.Count == 0)
        {
            return rows;
        }

        var headers = ParseCsvLine(lines[0]);
        var versionIndex = GetColumnIndex(headers, "Version");
        var titleIndex = GetColumnIndex(headers, "Title");
        var matchTitleIndex = GetColumnIndex(headers, "MatchTitle");
        var difficultyIndex = GetColumnIndex(headers, "Difficulty");
        var normalTypeIndex = GetColumnIndex(headers, "NormalType");
        var normalTierIndex = GetColumnIndex(headers, "NormalTier");
        var hardTypeIndex = GetColumnIndex(headers, "HardType");
        var hardTierIndex = GetColumnIndex(headers, "HardTier");
        var rankTierIndex = GetColumnIndex(headers, "RankTier");

        for (var i = 1; i < lines.Count; i++)
        {
            var cells = ParseCsvLine(lines[i]);
            var row = new TierTableTitleRow
            {
                Version = GetCell(cells, versionIndex),
                Title = GetCell(cells, titleIndex),
                MatchTitle = GetCell(cells, matchTitleIndex),
                Difficulty = GetCell(cells, difficultyIndex),
                NormalType = GetCell(cells, normalTypeIndex),
                NormalTier = GetCell(cells, normalTierIndex),
                HardType = GetCell(cells, hardTypeIndex),
                HardTier = GetCell(cells, hardTierIndex),
                RankTier = GetCell(cells, rankTierIndex)
            };

            if (!string.IsNullOrWhiteSpace(row.Title) || !string.IsNullOrWhiteSpace(row.Difficulty) || !string.IsNullOrWhiteSpace(row.HardType) || !string.IsNullOrWhiteSpace(row.HardTier))
            {
                rows.Add(row);
            }
        }

        return rows;
    }

    private static int GetColumnIndex(IReadOnlyList<string> headers, string name)
    {
        for (var i = 0; i < headers.Count; i++)
        {
            if (string.Equals(headers[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    private static string GetCell(IReadOnlyList<string> cells, int index)
    {
        return index >= 0 && index < cells.Count ? cells[index].Trim() : string.Empty;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var ch = line[i];

            if (ch == '"')
            {
                if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                {
                    current.Append('"');
                    i++;
                }
                else
                {
                    inQuotes = !inQuotes;
                }

                continue;
            }

            if (ch == ',' && !inQuotes)
            {
                values.Add(current.ToString());
                current.Clear();
                continue;
            }

            current.Append(ch);
        }

        values.Add(current.ToString());
        return values;
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
