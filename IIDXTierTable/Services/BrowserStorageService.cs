using System.Text.Json;
using Microsoft.JSInterop;

namespace IIDXTierTable.Services;

public sealed class BrowserStorageService(IJSRuntime jsRuntime)
{
    private readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = false
    };

    public async Task SetItemAsync<T>(string key, T value)
    {
        var payload = JsonSerializer.Serialize(value, _jsonOptions);
        await jsRuntime.InvokeVoidAsync("iidxStorage.set", key, payload);
    }

    public async Task<T?> GetItemAsync<T>(string key)
    {
        var payload = await jsRuntime.InvokeAsync<string?>("iidxStorage.get", key);
        if (string.IsNullOrWhiteSpace(payload))
        {
            return default;
        }

        return JsonSerializer.Deserialize<T>(payload, _jsonOptions);
    }

    public Task RemoveItemAsync(string key)
    {
        return jsRuntime.InvokeVoidAsync("iidxStorage.remove", key).AsTask();
    }
}
