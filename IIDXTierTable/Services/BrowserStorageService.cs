using System.Text.Json;
using Microsoft.JSInterop;

namespace IIDXTierTable.Services;

public sealed class BrowserStorageService(IJSRuntime jsRuntime)
{
    private readonly JsonSerializerOptions _jsonOptions = CreateJsonOptions();

    private readonly Dictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            WriteIndented = false
        };

        var ignoreUnknown = typeof(JsonSerializerOptions).GetProperty("IgnoreUnknownProperties");
        if (ignoreUnknown is not null && ignoreUnknown.PropertyType == typeof(bool) && ignoreUnknown.CanWrite)
        {
            ignoreUnknown.SetValue(options, true);
        }

        return options;
    }

    public async Task SetItemAsync<T>(string key, T value)
    {
        var payload = JsonSerializer.Serialize(value, _jsonOptions);
        await jsRuntime.InvokeVoidAsync("iidxStorage.set", key, payload);
        _cache[key] = new CacheEntry(payload, value);
    }

    public async Task<T?> GetItemAsync<T>(string key)
    {
        if (_cache.TryGetValue(key, out var cached))
        {
            if (cached.Value is T typedValue)
            {
                return typedValue;
            }

            if (!string.IsNullOrWhiteSpace(cached.Payload))
            {
                var cachedValue = JsonSerializer.Deserialize<T>(cached.Payload, _jsonOptions);
                _cache[key] = new CacheEntry(cached.Payload, cachedValue);
                return cachedValue;
            }
        }

        var payload = await jsRuntime.InvokeAsync<string?>("iidxStorage.get", key);
        if (string.IsNullOrWhiteSpace(payload))
        {
            _cache.Remove(key);
            return default;
        }

        var value = JsonSerializer.Deserialize<T>(payload, _jsonOptions);
        _cache[key] = new CacheEntry(payload, value);
        return value;
    }

    public Task RemoveItemAsync(string key)
    {
        _cache.Remove(key);
        return jsRuntime.InvokeVoidAsync("iidxStorage.remove", key).AsTask();
    }

    public Task PrimeCacheAsync<T>(string key)
    {
        return GetItemAsync<T>(key);
    }

    private sealed record CacheEntry(string Payload, object? Value);
}
