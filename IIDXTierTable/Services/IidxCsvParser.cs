using System.Globalization;
using IIDXTierTable.Models;

namespace IIDXTierTable.Services;

public sealed class IidxCsvParser
{
    private static readonly string[] Header =
    [
        "バージョン", "タイトル", "ジャンル", "アーティスト", "プレー回数",
        "BEGINNER 難易度", "BEGINNER スコア", "BEGINNER PGreat", "BEGINNER Great", "BEGINNER ミスカウント", "BEGINNER クリアタイプ", "BEGINNER DJ LEVEL",
        "NORMAL 難易度", "NORMAL スコア", "NORMAL PGreat", "NORMAL Great", "NORMAL ミスカウント", "NORMAL クリアタイプ", "NORMAL DJ LEVEL",
        "HYPER 難易度", "HYPER スコア", "HYPER PGreat", "HYPER Great", "HYPER ミスカウント", "HYPER クリアタイプ", "HYPER DJ LEVEL",
        "ANOTHER 難易度", "ANOTHER スコア", "ANOTHER PGreat", "ANOTHER Great", "ANOTHER ミスカウント", "ANOTHER クリアタイプ", "ANOTHER DJ LEVEL",
        "LEGGENDARIA 難易度", "LEGGENDARIA スコア", "LEGGENDARIA PGreat", "LEGGENDARIA Great", "LEGGENDARIA ミスカウント", "LEGGENDARIA クリアタイプ", "LEGGENDARIA DJ LEVEL",
        "最終プレー日時"
    ];

    public CsvImportResult Parse(string csv)
    {
        var result = new CsvImportResult();

        if (string.IsNullOrWhiteSpace(csv))
        {
            result.Errors.Add("CSV 입력이 비어 있습니다.");
            return result;
        }

        var lines = csv.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        var nonEmptyLines = lines.Where(static line => !string.IsNullOrWhiteSpace(line)).ToList();

        if (nonEmptyLines.Count < 2)
        {
            result.Errors.Add("헤더와 데이터 행이 필요합니다.");
            return result;
        }

        var actualHeader = ParseCsvLine(nonEmptyLines[0]);
        if (!ValidateHeader(actualHeader, out var headerError))
        {
            result.Errors.Add(headerError);
            return result;
        }

        var songs = new List<IidxSongRecord>();

        for (var i = 1; i < nonEmptyLines.Count; i++)
        {
            var lineNo = i + 1;
            var cells = ParseCsvLine(nonEmptyLines[i]);

            if (cells.Count != Header.Length)
            {
                result.Errors.Add($"{lineNo}행 열 개수 오류: 기대 {Header.Length}, 실제 {cells.Count}");
                continue;
            }

            var song = ParseSong(cells, lineNo, result.Errors);
            if (song is not null)
            {
                songs.Add(song);
            }
        }

        if (result.Errors.Count > 0)
        {
            return result;
        }

        result.Envelope = new IidxScoreEnvelope
        {
            SavedAtUtc = DateTimeOffset.UtcNow,
            Songs = songs
        };
        result.IsSuccess = true;

        return result;
    }

    private static bool ValidateHeader(IReadOnlyList<string> actual, out string error)
    {
        if (actual.Count != Header.Length)
        {
            error = $"헤더 열 개수 오류: 기대 {Header.Length}, 실제 {actual.Count}";
            return false;
        }

        for (var i = 0; i < Header.Length; i++)
        {
            if (!string.Equals(actual[i], Header[i], StringComparison.Ordinal))
            {
                error = $"헤더 불일치: {i + 1}번째 열 기대값 '{Header[i]}', 실제값 '{actual[i]}'";
                return false;
            }
        }

        error = string.Empty;
        return true;
    }

    private static IidxSongRecord? ParseSong(IReadOnlyList<string> cells, int lineNo, ICollection<string> errors)
    {
        var lastIndex = cells.Count - 1;

        var song = new IidxSongRecord
        {
            Version = cells[0].Trim(),
            VersionSortOrder = IidxVersionOrder.Resolve(cells[0].Trim()),
            Title = cells[1].Trim(),
            Genre = cells[2].Trim(),
            Artist = cells[3].Trim(),
            PlayCount = ParseInt(cells[4], lineNo, "プレー回数", errors),
            LastPlayedAtRaw = cells[lastIndex].Trim(),
            LastPlayedAt = ParseDateTime(cells[lastIndex])
        };

        foreach (var difficulty in IidxDifficultyNames.All)
        {
            var startIndex = difficulty switch
            {
                "BEGINNER" => 5,
                "NORMAL" => 12,
                "HYPER" => 19,
                "ANOTHER" => 26,
                "LEGGENDARIA" => 33,
                _ => throw new InvalidOperationException("알 수 없는 난이도입니다.")
            };

            song.Difficulties[difficulty] = new IidxDifficultyRecord
            {
                Level = ParseInt(cells[startIndex], lineNo, $"{difficulty} 難易度", errors),
                Score = ParseInt(cells[startIndex + 1], lineNo, $"{difficulty} スコア", errors),
                PGreat = ParseInt(cells[startIndex + 2], lineNo, $"{difficulty} PGreat", errors),
                Great = ParseInt(cells[startIndex + 3], lineNo, $"{difficulty} Great", errors),
                MissCount = ParseMissCount(cells[startIndex + 4]),
                ClearType = cells[startIndex + 5].Trim(),
                DjLevel = cells[startIndex + 6].Trim()
            };
        }

        if (string.IsNullOrWhiteSpace(song.Title))
        {
            errors.Add($"{lineNo}행 오류: 제목이 비어 있습니다.");
            return null;
        }

        return song;
    }

    private static int ParseInt(string value, int lineNo, string columnName, ICollection<string> errors)
    {
        var trimmed = value.Trim();
        if (int.TryParse(trimmed, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed))
        {
            return parsed;
        }

        errors.Add($"{lineNo}행 '{columnName}' 숫자 파싱 실패: '{value}'");
        return 0;
    }

    private static string ParseMissCount(string value)
    {
        var trimmed = value.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? "---" : trimmed;
    }

    private static DateTime? ParseDateTime(string value)
    {
        var trimmed = value.Trim();
        if (DateTime.TryParseExact(trimmed, "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed;
        }

        return null;
    }

    private static List<string> ParseCsvLine(string line)
    {
        var values = new List<string>();
        var current = new System.Text.StringBuilder();
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
