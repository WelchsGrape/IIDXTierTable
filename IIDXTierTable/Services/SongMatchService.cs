using IIDXTierTable.Models;

namespace IIDXTierTable.Services;

public sealed class SongMatchService
{
    public static readonly string[] ClearTypeLabels = ["NO PLAY", "FAILED", "ASSIST CLEAR", "EASY CLEAR", "CLEAR", "HARD CLEAR", "EX HARD CLEAR", "FULLCOMBO CLEAR"];

    public IReadOnlyDictionary<string, string> BuildClearTypeLookup(IidxScoreEnvelope envelope)
    {
        var clearTypeBySong = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var song in envelope.Songs)
        {
            foreach (var difficulty in song.Difficulties)
            {
                clearTypeBySong[BuildSongKey(song.Title, difficulty.Key)] = difficulty.Value.ClearType;
            }
        }

        return clearTypeBySong;
    }

    public string GetClearType(
        TierTableTitleRow row,
        IReadOnlyDictionary<string, string> clearTypeBySong)
    {
        var title = string.IsNullOrWhiteSpace(row.MatchTitle) ? row.Title : row.MatchTitle;
        var key = BuildSongKey(title, row.Difficulty);

        if (clearTypeBySong.TryGetValue(key, out var clearType)
            && !string.IsNullOrWhiteSpace(clearType))
        {
            return clearType.Trim();
        }

        return "NO PLAY";
    }

    public static string BuildSongKey(string title, string difficulty)
    {
        return string.Concat(title.Trim(), "\u001f", difficulty.Trim());
    }
}
