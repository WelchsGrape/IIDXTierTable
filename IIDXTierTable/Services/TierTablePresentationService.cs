using IIDXTierTable.Models;

namespace IIDXTierTable.Services;

public sealed class TierTablePresentationService
{
    private static readonly string[] TierOrder = ["S+", "S", "A+", "A", "B+", "B", "C", "D", "E", "F"];
    private readonly SongMatchService songMatcher;

    public TierTablePresentationService(SongMatchService songMatcher)
    {
        this.songMatcher = songMatcher;
    }

    public TierTableViewData BuildView(
        IEnumerable<TierTableTitleRow> rows,
        DifficultyMode mode,
        string selectedTimelineVersion,
        string songSortMode,
        IReadOnlyDictionary<string, string> clearTypeBySong)
    {
        var visibleRows = rows
            .Where(row => IsVisibleForSelectedTimelineVersion(row, selectedTimelineVersion))
            .ToList();
        var undecidedRows = SortSongs(
            visibleRows.Where(row => IsUndecidedRow(row, mode)),
            songSortMode,
            clearTypeBySong);
        var groupedRows = visibleRows
            .Where(row => !string.IsNullOrWhiteSpace(GetTypeName(row, mode))
                && !string.IsNullOrWhiteSpace(GetTierName(row, mode)))
            .GroupBy(row => GetTypeName(row, mode), StringComparer.OrdinalIgnoreCase)
            .OrderBy(group => GetTypeOrder(group.Key))
            .ThenBy(group => group.Key)
            .Select(group => new TierTableTypeGroup(
                group.Key,
                group
                    .GroupBy(row => GetTierName(row, mode), StringComparer.OrdinalIgnoreCase)
                    .OrderBy(tier => GetTierOrder(tier.Key))
                    .ThenBy(tier => tier.Key)
                    .Select(tier => new TierTableTierGroup(
                        tier.Key,
                        SortSongs(tier, songSortMode, clearTypeBySong)))
                    .ToList()))
            .ToList();

        return new TierTableViewData(visibleRows, groupedRows, undecidedRows);
    }

    public IReadOnlyList<TierTableTitleRow> SortSongs(
        IEnumerable<TierTableTitleRow> songs,
        string songSortMode,
        IReadOnlyDictionary<string, string> clearTypeBySong)
    {
        var ordered = songs
            .OrderBy(row => GetTitleSortCategory(row.Title))
            .ThenBy(row => row.Title, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Difficulty);

        if (string.Equals(songSortMode, "ByLampAscending", StringComparison.OrdinalIgnoreCase))
        {
            ordered = songs
                .OrderBy(row => GetClearTypeOrder(GetClearType(row, clearTypeBySong)))
                .ThenBy(row => GetTitleSortCategory(row.Title))
                .ThenBy(row => row.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(row => row.Difficulty);
        }
        else if (string.Equals(songSortMode, "ByLampDescending", StringComparison.OrdinalIgnoreCase))
        {
            ordered = songs
                .OrderByDescending(row => GetClearTypeOrder(GetClearType(row, clearTypeBySong)))
                .ThenBy(row => GetTitleSortCategory(row.Title))
                .ThenBy(row => row.Title, StringComparer.OrdinalIgnoreCase)
                .ThenBy(row => row.Difficulty);
        }

        return ordered.ToList();
    }

    public IReadOnlyList<List<TierTableTitleRow>> ChunkSongs(
        IReadOnlyList<TierTableTitleRow> songs,
        int chunkSize)
    {
        var chunks = new List<List<TierTableTitleRow>>();
        if (chunkSize <= 0 || songs.Count == 0)
        {
            return chunks;
        }

        for (var i = 0; i < songs.Count; i += chunkSize)
        {
            var size = Math.Min(chunkSize, songs.Count - i);
            chunks.Add(songs.Skip(i).Take(size).ToList());
        }

        return chunks;
    }

    public IReadOnlyList<TierTableChartSegment> BuildChart(
        IReadOnlyList<TierTableTitleRow> rows,
        IReadOnlyDictionary<string, string> clearTypeBySong)
    {
        var counts = SongMatchService.ClearTypeLabels
            .Select(clearType => new TierTableChartSegment(
                clearType,
                rows.Count(row => string.Equals(GetClearType(row, clearTypeBySong), clearType, StringComparison.OrdinalIgnoreCase)),
                0))
            .ToList();
        var total = counts.Sum(item => item.Count);

        return total == 0
            ? counts
            : counts.Select(item => item with { Percentage = item.Count * 100.0 / total }).ToList();
    }

    public string GetClearType(TierTableTitleRow row, IReadOnlyDictionary<string, string> clearTypeBySong)
    {
        return songMatcher.GetClearType(row, clearTypeBySong);
    }

    public string GetTypeName(TierTableTitleRow row, DifficultyMode mode)
    {
        return mode == DifficultyMode.Hard ? row.HardType : row.NormalType;
    }

    public string GetTierName(TierTableTitleRow row, DifficultyMode mode)
    {
        return mode == DifficultyMode.Hard ? row.HardTier : row.NormalTier;
    }

    public static int GetClearTypeOrder(string clearType) => clearType switch
    {
        "NO PLAY" => 0,
        "FAILED" => 1,
        "ASSIST CLEAR" => 2,
        "EASY CLEAR" => 3,
        "CLEAR" => 4,
        "HARD CLEAR" => 5,
        "EX HARD CLEAR" => 6,
        "FULLCOMBO CLEAR" => 7,
        _ => -1
    };

    public static int GetTierOrder(string tierName)
    {
        var index = Array.IndexOf(TierOrder, tierName);
        return index >= 0 ? index : int.MaxValue;
    }

    public static bool IsTierBoundary(string tierName) => GetTierOrder(tierName) > 0;

    private static bool IsVisibleForSelectedTimelineVersion(TierTableTitleRow row, string selectedTimelineVersion)
    {
        if (string.IsNullOrWhiteSpace(selectedTimelineVersion)
            || string.Equals(selectedTimelineVersion, "All", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(row.Version))
        {
            return true;
        }

        return IidxVersionOrder.Resolve(row.Version) <= IidxVersionOrder.Resolve(selectedTimelineVersion);
    }

    private static bool IsUndecidedRow(TierTableTitleRow row, DifficultyMode mode)
    {
        var typeName = mode == DifficultyMode.Hard ? row.HardType : row.NormalType;
        var tierName = mode == DifficultyMode.Hard ? row.HardTier : row.NormalTier;
        return string.IsNullOrWhiteSpace(typeName)
            || string.IsNullOrWhiteSpace(tierName)
            || string.Equals(typeName, "미결정", StringComparison.OrdinalIgnoreCase)
            || string.Equals(tierName, "미결정", StringComparison.OrdinalIgnoreCase);
    }

    private static int GetTypeOrder(string typeName) => typeName switch
    {
        "지력" => 0,
        "개인차" => 1,
        _ => 2
    };

    private static int GetTitleSortCategory(string title)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            return 1;
        }

        var firstChar = title.TrimStart()[0];
        return char.IsAsciiLetter(firstChar) ? 0 : 1;
    }
}

public sealed record TierTableViewData(
    IReadOnlyList<TierTableTitleRow> VisibleRows,
    IReadOnlyList<TierTableTypeGroup> GroupedRows,
    IReadOnlyList<TierTableTitleRow> UndecidedRows);

public sealed record TierTableTypeGroup(
    string TypeName,
    IReadOnlyList<TierTableTierGroup> Tiers);

public sealed record TierTableTierGroup(
    string TierName,
    IReadOnlyList<TierTableTitleRow> Songs);

public sealed record TierTableChartSegment(string ClearType, int Count, double Percentage);
