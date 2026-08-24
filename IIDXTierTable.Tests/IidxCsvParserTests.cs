using System.Reflection;
using System.Text;
using IIDXTierTable.Services;
using Xunit;

namespace IIDXTierTable.Tests;

public sealed class IidxCsvParserTests
{
    private const string FixtureResourceName = "IIDXTierTable.Tests.Fixtures.0948-8899_sp_score.csv";

    [Fact]
    public void Parse_OriginalCsv_SucceedsAndReadsAllSongs()
    {
        var result = new IidxCsvParser().Parse(ReadFixture());

        Assert.True(result.IsSuccess);
        Assert.Empty(result.Errors);
        Assert.NotNull(result.Envelope);
        Assert.Equal(1927, result.Envelope!.Songs.Count);
        Assert.Equal("v1", result.Envelope.SchemaVersion);
    }

    [Fact]
    public void Parse_OriginalCsv_PreservesQuotesInTitles()
    {
        var result = new IidxCsvParser().Parse(ReadFixture());

        var songs = result.Envelope!.Songs;

        Assert.Contains(songs, song => song.Title == "Anisakis -somatic mutation type\"Forza\"-");
        Assert.Contains(songs, song => song.Title == "Life Is A Game ft.DD\"ナカタ\"Metal");
        Assert.Contains(songs, song => song.Title == "ピアノ独奏無言歌 \"灰燼\"");
    }

    [Fact]
    public void Parse_OriginalCsv_ReadsRepresentativeSongValues()
    {
        var result = new IidxCsvParser().Parse(ReadFixture());

        var song = Assert.Single(result.Envelope!.Songs, song =>
            song.Title == "Anisakis -somatic mutation type\"Forza\"-");
        var another = song.Difficulties["ANOTHER"];

        Assert.Equal("DJ TROOPERS", song.Version);
        Assert.Equal(42, song.PlayCount);
        Assert.Equal(2648, another.Score);
        Assert.Equal(1222, another.PGreat);
        Assert.Equal(204, another.Great);
        Assert.Equal("3", another.MissCount);
        Assert.Equal("FULLCOMBO CLEAR", another.ClearType);
        Assert.Equal("AAA", another.DjLevel);
        Assert.Equal("2026-04-13 16:27", song.LastPlayedAtRaw);
        Assert.Equal(new DateTime(2026, 4, 13, 16, 27, 0), song.LastPlayedAt);
    }

    [Fact]
    public void Parse_OriginalCsv_PreservesDashMissCount()
    {
        var result = new IidxCsvParser().Parse(ReadFixture());

        var song = Assert.Single(result.Envelope!.Songs, song => song.Title == "22DUNK");

        Assert.Equal("---", song.Difficulties["BEGINNER"].MissCount);
    }

    [Fact]
    public void Parse_OriginalCsv_HandlesCrLfLineEndings()
    {
        var csv = ReadFixture()
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Replace("\n", "\r\n", StringComparison.Ordinal);

        var result = new IidxCsvParser().Parse(csv);

        Assert.True(result.IsSuccess);
        Assert.Equal(1927, result.Envelope!.Songs.Count);
    }

    [Fact]
    public void Parse_EmptyInput_ReturnsError()
    {
        var result = new IidxCsvParser().Parse(string.Empty);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Envelope);
        Assert.Contains("CSV 입력이 비어 있습니다.", result.Errors);
    }

    private static string ReadFixture()
    {
        using var stream = Assembly.GetExecutingAssembly()
            .GetManifestResourceStream(FixtureResourceName);
        Assert.NotNull(stream);
        using var reader = new StreamReader(stream!, Encoding.UTF8);
        return reader.ReadToEnd();
    }
}
