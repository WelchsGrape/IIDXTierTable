namespace IIDXTierTable.Models;

public static class IidxStorageKeys
{
    public const string Scores = "iidx.scores.v1";
}

public static class IidxDifficultyNames
{
    public static readonly string[] All = ["BEGINNER", "NORMAL", "HYPER", "ANOTHER", "LEGGENDARIA"];
}

public sealed class IidxScoreEnvelope
{
    public string SchemaVersion { get; set; } = "v1";

    public DateTimeOffset SavedAtUtc { get; set; }

    public List<IidxSongRecord> Songs { get; set; } = [];
}

public sealed class IidxSongRecord
{
    public string Version { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string Genre { get; set; } = string.Empty;

    public string Artist { get; set; } = string.Empty;

    public int PlayCount { get; set; }

    public Dictionary<string, IidxDifficultyRecord> Difficulties { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public string LastPlayedAtRaw { get; set; } = string.Empty;

    public DateTime? LastPlayedAt { get; set; }
}

public sealed class IidxDifficultyRecord
{
    public int Level { get; set; }

    public int Score { get; set; }

    public int PGreat { get; set; }

    public int Great { get; set; }

    public string MissCount { get; set; } = "---";

    public string ClearType { get; set; } = "NO PLAY";

    public string DjLevel { get; set; } = "---";
}

public sealed class CsvImportResult
{
    public bool IsSuccess { get; set; }

    public IidxScoreEnvelope? Envelope { get; set; }

    public List<string> Errors { get; set; } = [];
}
