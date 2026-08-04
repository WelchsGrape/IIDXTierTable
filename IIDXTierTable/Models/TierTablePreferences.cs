namespace IIDXTierTable.Models;

public sealed class TierTablePreferences
{
    public string ViewMode { get; set; } = string.Empty;

    public int ColumnCount { get; set; }

    public bool UseTopHeader { get; set; }

    public string SongSortMode { get; set; } = string.Empty;

    public string HighlightThreshold { get; set; } = string.Empty;
}
