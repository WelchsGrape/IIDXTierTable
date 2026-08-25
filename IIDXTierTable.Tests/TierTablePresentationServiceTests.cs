using IIDXTierTable.Models;
using IIDXTierTable.Services;
using Xunit;

namespace IIDXTierTable.Tests;

public sealed class TierTablePresentationServiceTests
{
    private readonly TierTablePresentationService service = new(new SongMatchService());

    [Fact]
    public void BuildView_FiltersRowsAfterSelectedVersion_AndKeepsRowsWithoutVersion()
    {
        var rows = new[]
        {
            Row("HAPPY SKY", "Old"),
            Row("Sparkle Shower", "New"),
            Row(string.Empty, "No version")
        };

        var result = service.BuildView(rows, DifficultyMode.Normal, "EPOLIS", "ByTitle", EmptyLookup());

        Assert.Equal(["Old", "No version"], result.VisibleRows.Select(row => row.Title));
    }

    [Fact]
    public void BuildView_FindsUndecidedRowsForSelectedMode()
    {
        var rows = new[]
        {
            Row("EPOLIS", "Normal undecided", normalType: "미결정", normalTier: "S", hardType: "지력", hardTier: "A"),
            Row("EPOLIS", "Hard undecided", normalType: "지력", normalTier: "S", hardType: "미결정", hardTier: "A"),
            Row("EPOLIS", "Normal decided", normalType: "지력", normalTier: "S", hardType: "지력", hardTier: "A")
        };

        var normal = service.BuildView(rows, DifficultyMode.Normal, "EPOLIS", "ByTitle", EmptyLookup());
        var hard = service.BuildView(rows, DifficultyMode.Hard, "EPOLIS", "ByTitle", EmptyLookup());

        Assert.Equal("Normal undecided", Assert.Single(normal.UndecidedRows).Title);
        Assert.Equal("Hard undecided", Assert.Single(hard.UndecidedRows).Title);
    }

    [Fact]
    public void SortSongs_SortsByLampAscendingAndDescending()
    {
        var rows = new[]
        {
            Row("EPOLIS", "Clear", difficulty: "ANOTHER"),
            Row("EPOLIS", "No play", difficulty: "ANOTHER"),
            Row("EPOLIS", "Full combo", difficulty: "ANOTHER")
        };
        var lookup = new Dictionary<string, string>
        {
            [SongMatchService.BuildSongKey("Clear", "ANOTHER")] = "CLEAR",
            [SongMatchService.BuildSongKey("No play", "ANOTHER")] = "NO PLAY",
            [SongMatchService.BuildSongKey("Full combo", "ANOTHER")] = "FULLCOMBO CLEAR"
        };

        var ascending = service.SortSongs(rows, "ByLampAscending", lookup);
        var descending = service.SortSongs(rows, "ByLampDescending", lookup);

        Assert.Equal(["No play", "Clear", "Full combo"], ascending.Select(row => row.Title));
        Assert.Equal(["Full combo", "Clear", "No play"], descending.Select(row => row.Title));
    }

    [Fact]
    public void ChunkSongs_SplitsRowsAndHandlesInvalidSize()
    {
        var rows = Enumerable.Range(1, 5)
            .Select(index => Row("EPOLIS", index.ToString()))
            .ToArray();

        var chunks = service.ChunkSongs(rows, 2);

        Assert.Equal(3, chunks.Count);
        Assert.Equal(["1", "2"], chunks[0].Select(row => row.Title));
        Assert.Equal(["3", "4"], chunks[1].Select(row => row.Title));
        Assert.Equal(["5"], chunks[2].Select(row => row.Title));
        Assert.Empty(service.ChunkSongs(rows, 0));
    }

    [Fact]
    public void BuildChart_CountsNoPlayAndCalculatesPercentages()
    {
        var rows = new[]
        {
            Row("EPOLIS", "Clear", difficulty: "ANOTHER"),
            Row("EPOLIS", "Hard", difficulty: "ANOTHER"),
            Row("EPOLIS", "Unknown", difficulty: "ANOTHER")
        };
        var lookup = new Dictionary<string, string>
        {
            [SongMatchService.BuildSongKey("Clear", "ANOTHER")] = "CLEAR",
            [SongMatchService.BuildSongKey("Hard", "ANOTHER")] = "HARD CLEAR"
        };

        var chart = service.BuildChart(rows, lookup);

        Assert.Equal(1, chart.Single(segment => segment.ClearType == "NO PLAY").Count);
        Assert.Equal(1, chart.Single(segment => segment.ClearType == "CLEAR").Count);
        Assert.Equal(1, chart.Single(segment => segment.ClearType == "HARD CLEAR").Count);
        Assert.Equal(100, chart.Sum(segment => segment.Percentage), precision: 10);
    }

    private static TierTableTitleRow Row(
        string version,
        string title,
        string difficulty = "HYPER",
        string normalType = "지력",
        string normalTier = "S",
        string hardType = "지력",
        string hardTier = "S") => new()
        {
            Version = version,
            Title = title,
            Difficulty = difficulty,
            NormalType = normalType,
            NormalTier = normalTier,
            HardType = hardType,
            HardTier = hardTier
        };

    private static IReadOnlyDictionary<string, string> EmptyLookup()
        => new Dictionary<string, string>();
}
