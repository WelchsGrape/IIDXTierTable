using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Net;

namespace IIDXTierTable.Api
{
    public class Function1
    {
        private readonly ILogger _logger;

        public Function1(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<Function1>();
        }

        [Function("Health")]
        public HttpResponseData Health([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "health")] HttpRequestData req)
        {
            _logger.LogInformation("Health check requested.");

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString("{\"status\":\"ok\"}");
            return response;
        }

        [Function("TierTable")]
        public HttpResponseData TierTable([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "tier-table")] HttpRequestData req)
        {
            _logger.LogInformation("Tier table data requested.");

            var csvPath = Path.Combine(AppContext.BaseDirectory, "SP12TierData.csv");
            if (!File.Exists(csvPath))
            {
                return ErrorResponse(req, HttpStatusCode.InternalServerError, "서열표 데이터 파일을 찾을 수 없습니다.");
            }

            var rows = ParseTierTableCsv(File.ReadAllText(csvPath, Encoding.UTF8));
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonSerializer.Serialize(rows));
            return response;
        }

        [Function("RankPoints")]
        public HttpResponseData RankPoints([HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "rank-points")] HttpRequestData req)
        {
            _logger.LogInformation("Rank points data requested.");

            var jsonPath = Path.Combine(AppContext.BaseDirectory, "RankPoints.json");
            if (!File.Exists(jsonPath))
            {
                return ErrorResponse(req, HttpStatusCode.InternalServerError, "랭크 포인트 데이터 파일을 찾을 수 없습니다.");
            }

            var json = File.ReadAllText(jsonPath, Encoding.UTF8);
            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(json);
            return response;
        }

        [Function("ImportScores")]
        public async Task<HttpResponseData> ImportScores([HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "scores/import")] HttpRequestData req)
        {
            _logger.LogInformation("Score CSV import requested.");

            using var reader = new StreamReader(req.Body, Encoding.UTF8);
            var csv = await reader.ReadToEndAsync();
            var result = ScoreCsvParser.Parse(csv);

            if (!result.IsSuccess || result.Envelope is null)
            {
                var errorResponse = req.CreateResponse(HttpStatusCode.UnprocessableEntity);
                errorResponse.Headers.Add("Content-Type", "application/json; charset=utf-8");
                errorResponse.WriteString(JsonSerializer.Serialize(result));
                return errorResponse;
            }

            var response = req.CreateResponse(HttpStatusCode.OK);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonSerializer.Serialize(result.Envelope));
            return response;
        }

        private static HttpResponseData ErrorResponse(HttpRequestData req, HttpStatusCode statusCode, string message)
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "application/json; charset=utf-8");
            response.WriteString(JsonSerializer.Serialize(new { error = message }));
            return response;
        }

        private static List<TierTableRow> ParseTierTableCsv(string csv)
        {
            var lines = csv.Replace("\r\n", "\n")
                .Split('\n')
                .Where(line => !string.IsNullOrWhiteSpace(line))
                .ToList();
            var rows = new List<TierTableRow>();
            if (lines.Count == 0)
            {
                return rows;
            }

            var headers = ParseCsvLine(lines[0]);
            var indexes = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < headers.Count; i++)
            {
                indexes[headers[i]] = i;
            }

            for (var i = 1; i < lines.Count; i++)
            {
                var cells = ParseCsvLine(lines[i]);
                var row = new TierTableRow
                {
                    Version = GetCell(cells, indexes, "Version"),
                    Title = GetCell(cells, indexes, "Title"),
                    MatchTitle = GetCell(cells, indexes, "MatchTitle"),
                    Difficulty = GetCell(cells, indexes, "Difficulty"),
                    NormalType = GetCell(cells, indexes, "NormalType"),
                    NormalTier = GetCell(cells, indexes, "NormalTier"),
                    HardType = GetCell(cells, indexes, "HardType"),
                    HardTier = GetCell(cells, indexes, "HardTier"),
                    RankTier = GetCell(cells, indexes, "RankTier")
                };

                if (!string.IsNullOrWhiteSpace(row.Title))
                {
                    rows.Add(row);
                }
            }

            return rows;
        }

        private static string GetCell(IReadOnlyList<string> cells, IReadOnlyDictionary<string, int> indexes, string name)
        {
            return indexes.TryGetValue(name, out var index) && index < cells.Count
                ? cells[index].Trim()
                : string.Empty;
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
                }
                else if (ch == ',' && !inQuotes)
                {
                    values.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(ch);
                }
            }

            values.Add(current.ToString());
            return values;
        }

        private sealed class TierTableRow
        {
            public string Version { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string MatchTitle { get; set; } = string.Empty;
            public string Difficulty { get; set; } = string.Empty;
            public string NormalType { get; set; } = string.Empty;
            public string NormalTier { get; set; } = string.Empty;
            public string HardType { get; set; } = string.Empty;
            public string HardTier { get; set; } = string.Empty;
            public string RankTier { get; set; } = string.Empty;
        }

        private static class ScoreCsvParser
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

            public static ImportResult Parse(string csv)
            {
                var result = new ImportResult();
                if (string.IsNullOrWhiteSpace(csv))
                {
                    result.Errors.Add("CSV 입력이 비어 있습니다.");
                    return result;
                }

                var lines = csv.Replace("\r\n", "\n").Split('\n').Where(line => !string.IsNullOrWhiteSpace(line)).ToList();
                if (lines.Count < 2)
                {
                    result.Errors.Add("헤더와 데이터 행이 필요합니다.");
                    return result;
                }

                var actualHeader = ParseCsvLine(lines[0]);
                if (actualHeader.Count != Header.Length)
                {
                    result.Errors.Add($"헤더 열 개수 오류: 기대 {Header.Length}, 실제 {actualHeader.Count}");
                    return result;
                }

                for (var i = 0; i < Header.Length; i++)
                {
                    if (!string.Equals(actualHeader[i], Header[i], StringComparison.Ordinal))
                    {
                        result.Errors.Add($"헤더 불일치: {i + 1}번째 열 기대값 '{Header[i]}', 실제값 '{actualHeader[i]}'");
                        return result;
                    }
                }

                var songs = new List<SongRecord>();
                for (var i = 1; i < lines.Count; i++)
                {
                    var cells = ParseCsvLine(lines[i]);
                    if (cells.Count != Header.Length)
                    {
                        result.Errors.Add($"{i + 1}행 열 개수 오류: 기대 {Header.Length}, 실제 {cells.Count}");
                        continue;
                    }

                    var song = ParseSong(cells, i + 1, result.Errors);
                    if (song is not null)
                    {
                        songs.Add(song);
                    }
                }

                if (result.Errors.Count == 0)
                {
                    result.IsSuccess = true;
                    result.Envelope = new ScoreEnvelope { SavedAtUtc = DateTimeOffset.UtcNow, Songs = songs };
                }

                return result;
            }

            private static SongRecord? ParseSong(IReadOnlyList<string> cells, int lineNo, ICollection<string> errors)
            {
                var song = new SongRecord
                {
                    Version = cells[0].Trim(), Title = cells[1].Trim(), Genre = cells[2].Trim(), Artist = cells[3].Trim(),
                    PlayCount = ParseInt(cells[4], lineNo, "プレー回数", errors), LastPlayedAtRaw = cells[^1].Trim(),
                    LastPlayedAt = DateTime.TryParseExact(cells[^1].Trim(), "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null,
                    VersionSortOrder = ResolveVersion(cells[0])
                };

                var starts = new[] { 5, 12, 19, 26, 33 };
                var names = new[] { "BEGINNER", "NORMAL", "HYPER", "ANOTHER", "LEGGENDARIA" };
                for (var i = 0; i < starts.Length; i++)
                {
                    var start = starts[i];
                    song.Difficulties[names[i]] = new DifficultyRecord
                    {
                        Level = ParseInt(cells[start], lineNo, $"{names[i]} 難易度", errors), Score = ParseInt(cells[start + 1], lineNo, $"{names[i]} スコア", errors),
                        PGreat = ParseInt(cells[start + 2], lineNo, $"{names[i]} PGreat", errors), Great = ParseInt(cells[start + 3], lineNo, $"{names[i]} Great", errors),
                        MissCount = string.IsNullOrWhiteSpace(cells[start + 4]) ? "---" : cells[start + 4].Trim(), ClearType = cells[start + 5].Trim(), DjLevel = cells[start + 6].Trim()
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
                if (int.TryParse(value.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)) return parsed;
                errors.Add($"{lineNo}행 '{columnName}' 숫자 파싱 실패: '{value}'");
                return 0;
            }

            private static int ResolveVersion(string value) => int.TryParse(value.Trim(), out var result) ? result : int.MaxValue;

            private static List<string> ParseCsvLine(string line)
            {
                var values = new List<string>(); var current = new StringBuilder(); var inQuotes = false;
                for (var i = 0; i < line.Length; i++)
                {
                    var ch = line[i];
                    if (ch == '"')
                    {
                        if (inQuotes && i + 1 < line.Length && line[i + 1] == '"') { current.Append('"'); i++; }
                        else inQuotes = !inQuotes;
                    }
                    else if (ch == ',' && !inQuotes) { values.Add(current.ToString()); current.Clear(); }
                    else current.Append(ch);
                }
                values.Add(current.ToString()); return values;
            }
        }

        private sealed class ImportResult
        {
            public bool IsSuccess { get; set; }
            public ScoreEnvelope? Envelope { get; set; }
            public List<string> Errors { get; set; } = new List<string>();
        }

        private sealed class ScoreEnvelope
        {
            public string SchemaVersion { get; set; } = "v1";
            public DateTimeOffset SavedAtUtc { get; set; }
            public List<SongRecord> Songs { get; set; } = new List<SongRecord>();
        }

        private sealed class SongRecord
        {
            public string Version { get; set; } = string.Empty; public int VersionSortOrder { get; set; }
            public string Title { get; set; } = string.Empty; public string Genre { get; set; } = string.Empty; public string Artist { get; set; } = string.Empty;
            public int PlayCount { get; set; } public Dictionary<string, DifficultyRecord> Difficulties { get; set; } = new Dictionary<string, DifficultyRecord>(StringComparer.OrdinalIgnoreCase);
            public string LastPlayedAtRaw { get; set; } = string.Empty; public DateTime? LastPlayedAt { get; set; }
        }

        private sealed class DifficultyRecord
        {
            public int Level { get; set; } public int Score { get; set; } public int PGreat { get; set; } public int Great { get; set; }
            public string MissCount { get; set; } = "---"; public string ClearType { get; set; } = "NO PLAY"; public string DjLevel { get; set; } = "---";
        }
    }
}
