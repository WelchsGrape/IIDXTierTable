using System.Net.Http.Json;

namespace IIDXTierTable.Services;

public sealed class HomeUpdateDataService
{
    public IReadOnlyList<HomeUpdateSection> Sections { get; private set; } = [];

    public bool IsInitialized { get; private set; }

    public string? ErrorMessage { get; private set; }

    public async Task InitializeAsync(HttpClient http)
    {
        if (IsInitialized)
        {
            return;
        }

        try
        {
            var sections = await http.GetFromJsonAsync<List<HomeUpdateSection>>("home-updates");
            Sections = [.. (sections ?? []).Where(section => !string.IsNullOrWhiteSpace(section.Title))];
            ErrorMessage = null;
            IsInitialized = true;
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            Sections = [];
            IsInitialized = false;
        }
    }
}

/// <summary>Home 화면에 표시할 업데이트 섹션입니다.</summary>
public sealed record HomeUpdateSection
{
    public string Title { get; init; } = string.Empty;

    public string Date { get; init; } = string.Empty;

    public IReadOnlyList<HomeUpdateItem> Items { get; init; } = [];
}

/// <summary>Home 업데이트 섹션에 표시할 항목입니다.</summary>
public sealed record HomeUpdateItem
{
    public string Title { get; init; } = string.Empty;

    public string? Detail { get; init; }
}
