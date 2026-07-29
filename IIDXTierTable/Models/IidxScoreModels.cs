namespace IIDXTierTable.Models;

public static class IidxStorageKeys
{
    public const string Scores = "iidx.scores.v1";
    public const string TierTableHardOptions = "iidx.tier-table-hard.options.v1";
    public const string TierTableNormalOptions = "iidx.tier-table-normal.options.v1";
}

public static class IidxDifficultyNames
{
    public static readonly string[] All = ["BEGINNER", "NORMAL", "HYPER", "ANOTHER", "LEGGENDARIA"];
}

public static class IidxVersionOrder
{
    private static readonly IReadOnlyDictionary<string, int> Map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
    {
        ["1st&substream"] = 1,
        ["2nd style"] = 2,
        ["3rd style"] = 3,
        ["4th style"] = 4,
        ["5th style"] = 5,
        ["6th style"] = 6,
        ["7th style"] = 7,
        ["8th style"] = 8,
        ["9th style"] = 9,
        ["10th style"] = 10,
        ["IIDX RED"] = 11,
        ["HAPPY SKY"] = 12,
        ["DistorteD"] = 13,
        ["GOLD"] = 14,
        ["DJ TROOPERS"] = 15,
        ["EMPRESS"] = 16,
        ["SIRIUS"] = 17,
        ["Resort Anthem"] = 18,
        ["Lincle"] = 19,
        ["tricoro"] = 20,
        ["SPADA"] = 21,
        ["PENDUAL"] = 22,
        ["copula"] = 23,
        ["SINOBUZ"] = 24,
        ["CANNON BALLERS"] = 25,
        ["Rootage"] = 26,
        ["HEROIC VERSE"] = 27,
        ["BISTROVER"] = 28,
        ["CastHour"] = 29,
        ["RESIDENT"] = 30,
        ["EPOLIS"] = 31,
        ["Pinky Crush"] = 32,
        ["Sparkle Shower"] = 33
    };

    public static int Resolve(string version)
    {
        if (Map.TryGetValue(version, out var order))
        {
            return order;
        }

        return int.MaxValue;
    }
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

    public int VersionSortOrder { get; set; }

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
