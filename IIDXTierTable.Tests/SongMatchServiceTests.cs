using IIDXTierTable.Models;
using IIDXTierTable.Services;
using Xunit;

namespace IIDXTierTable.Tests;

public sealed class SongMatchServiceTests
{
    [Fact]
    public void GetClearType_UsesMatchTitleWhenProvided()
    {
        var service = new SongMatchService();
        var envelope = new IidxScoreEnvelope
        {
            Songs =
            [
                new IidxSongRecord
                {
                    Title = "ACT0",
                    Difficulties = new Dictionary<string, IidxDifficultyRecord>
                    {
                        ["ANOTHER"] = new() { ClearType = "HARD CLEAR" }
                    }
                }
            ]
        };
        var row = new TierTableTitleRow
        {
            Title = "ACTØ",
            MatchTitle = "ACT0",
            Difficulty = "ANOTHER"
        };

        var lookup = service.BuildClearTypeLookup(envelope);

        Assert.Equal("HARD CLEAR", service.GetClearType(row, lookup));
    }

    [Fact]
    public void GetClearType_TrimsTitleAndDifficulty()
    {
        var service = new SongMatchService();
        var lookup = new Dictionary<string, string>
        {
            [SongMatchService.BuildSongKey("Song", "ANOTHER")] = "CLEAR"
        };
        var row = new TierTableTitleRow
        {
            Title = " Song ",
            Difficulty = " ANOTHER "
        };

        Assert.Equal("CLEAR", service.GetClearType(row, lookup));
    }

    [Fact]
    public void GetClearType_ReturnsNoPlayWhenSongDoesNotMatch()
    {
        var service = new SongMatchService();
        var row = new TierTableTitleRow
        {
            Title = "Unknown Song",
            Difficulty = "ANOTHER"
        };

        Assert.Equal("NO PLAY", service.GetClearType(row, new Dictionary<string, string>()));
    }

    [Fact]
    public void GetClearType_TrimsMatchedClearType()
    {
        var service = new SongMatchService();
        var row = new TierTableTitleRow { Title = "Song", Difficulty = "ANOTHER" };
        var lookup = new Dictionary<string, string>
        {
            [SongMatchService.BuildSongKey("Song", "ANOTHER")] = " HARD CLEAR "
        };

        Assert.Equal("HARD CLEAR", service.GetClearType(row, lookup));
    }

    [Fact]
    public void GetClearType_ReturnsNoPlayWhenMatchedClearTypeIsEmpty()
    {
        var service = new SongMatchService();
        var row = new TierTableTitleRow { Title = "Song", Difficulty = "ANOTHER" };
        var lookup = new Dictionary<string, string>
        {
            [SongMatchService.BuildSongKey("Song", "ANOTHER")] = "   "
        };

        Assert.Equal("NO PLAY", service.GetClearType(row, lookup));
    }

    [Fact]
    public void BuildClearTypeLookup_StoresEachDifficultyIndependently()
    {
        var service = new SongMatchService();
        var envelope = new IidxScoreEnvelope
        {
            Songs =
            [
                new IidxSongRecord
                {
                    Title = "Song",
                    Difficulties = new Dictionary<string, IidxDifficultyRecord>
                    {
                        ["HYPER"] = new() { ClearType = "CLEAR" },
                        ["ANOTHER"] = new() { ClearType = "HARD CLEAR" }
                    }
                }
            ]
        };

        var lookup = service.BuildClearTypeLookup(envelope);

        Assert.Equal("CLEAR", service.GetClearType(
            new TierTableTitleRow { Title = "Song", Difficulty = "HYPER" }, lookup));
        Assert.Equal("HARD CLEAR", service.GetClearType(
            new TierTableTitleRow { Title = "Song", Difficulty = "ANOTHER" }, lookup));
    }
}
