using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RentalHousingManagementConsole.Services;

/// <summary>
/// 백엔드 HTTP 호출 공통 클라이언트. BaseAddress는 EnvConfig에서 로드.
/// </summary>
public sealed class BackendApiClient
{
    private static readonly Lazy<BackendApiClient> _instance = new(() => new BackendApiClient());
    public static BackendApiClient Instance => _instance.Value;

    private readonly HttpClient _http;

    private BackendApiClient()
    {
        _http = new HttpClient
        {
            BaseAddress = EnvConfig.BackendBaseUri,
            Timeout = TimeSpan.FromSeconds(15)
        };
    }

    public void SetBearer(string? token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            _http.DefaultRequestHeaders.Authorization = null;
        }
        else
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }
    }

    public async Task<HttpResponseMessage> GetAsync(string path, CancellationToken ct = default)
        => await _http.GetAsync(path, ct).ConfigureAwait(false);

    public async Task<HttpResponseMessage> PostJsonAsync<T>(string path, T payload, CancellationToken ct = default)
    {
        var json = JsonSerializer.Serialize(payload);
        using var content = new StringContent(json, Encoding.UTF8, "application/json");
        return await _http.PostAsync(path, content, ct).ConfigureAwait(false);
    }

    public async Task<HttpResponseMessage> PostAsync(string path, CancellationToken ct = default)
        => await _http.PostAsync(path, content: null, ct).ConfigureAwait(false);

    public static JsonSerializerOptions JsonOptions { get; } = new(JsonSerializerDefaults.Web);
}
