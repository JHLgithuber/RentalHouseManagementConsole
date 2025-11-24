using System;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace RentalHousingManagementConsole.Services;

public sealed class AuthService
{
    private static readonly Lazy<AuthService> _instance = new(() => new AuthService());
    public static AuthService Instance => _instance.Value;

    private readonly BackendApiClient _api = BackendApiClient.Instance;

    private AuthService() { }

    public string? AccessToken { get; private set; }
    public string? RefreshToken { get; private set; }
    public string? Permission { get; private set; }
    public string? LastError { get; private set; }

    public bool IsAuthenticated => !string.IsNullOrEmpty(AccessToken);

    public async Task<bool> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        LastError = null;
        try
        {
            var res = await _api.PostJsonAsync("login", new { username, password }, ct).ConfigureAwait(false);
            if (!res.IsSuccessStatusCode)
            {
                AccessToken = null;
                RefreshToken = null;
                Permission = null;
                string body = string.Empty;
                try { body = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false); } catch { /* ignore */ }
                LastError = res.StatusCode == System.Net.HttpStatusCode.Unauthorized
                    ? "로그인에 실패했습니다. 사용자명 또는 비밀번호를 확인하세요."
                    : $"로그인 요청 실패: {(int)res.StatusCode} {res.ReasonPhrase}. {body}";
                return false;
            }

            var json = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var dto = JsonSerializer.Deserialize<LoginResponse>(json, BackendApiClient.JsonOptions);
            if (dto is null || string.IsNullOrWhiteSpace(dto.access_token))
            {
                LastError = "로그인 응답을 해석하지 못했습니다.";
                return false;
            }

            AccessToken = dto.access_token;
            RefreshToken = dto.refresh_token;
            Permission = dto.permission;
            _api.SetBearer(AccessToken);
            return true;
        }
        catch (HttpRequestException ex)
        {
            LastError = $"백엔드에 연결할 수 없습니다: {EnvConfig.BackendBaseUri} ({ex.Message}). 설정의 BACKEND_BASE_URL을 확인하거나 HM_USE_LOCAL_BACKEND=true로 로컬 서버를 구동하세요.";
            AccessToken = null; RefreshToken = null; Permission = null;
            return false;
        }
        catch (TaskCanceledException)
        {
            LastError = $"요청 시간이 초과되었습니다: {EnvConfig.BackendBaseUri}. 서버 상태를 확인해 주세요.";
            AccessToken = null; RefreshToken = null; Permission = null;
            return false;
        }
        catch (JsonException)
        {
            LastError = "서버 응답 형식이 올바르지 않습니다.";
            AccessToken = null; RefreshToken = null; Permission = null;
            return false;
        }
    }

    public async Task<bool> RefreshAsync(CancellationToken ct = default)
    {
        LastError = null;
        try
        {
            if (string.IsNullOrWhiteSpace(RefreshToken)) return false;

            using var req = new HttpRequestMessage(HttpMethod.Post, "refresh");
            // Flask-Backend expects Authorization header with Bearer refresh_token
            req.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", RefreshToken);

            var http = new HttpClient { BaseAddress = EnvConfig.BackendBaseUri };
            var res = await http.SendAsync(req, ct).ConfigureAwait(false);
            if (!res.IsSuccessStatusCode)
            {
                LastError = $"토큰 갱신 실패: {(int)res.StatusCode} {res.ReasonPhrase}";
                return false;
            }

            var json = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var dto = JsonSerializer.Deserialize<RefreshResponse>(json, BackendApiClient.JsonOptions);
            if (dto is null || string.IsNullOrWhiteSpace(dto.access_token))
            {
                LastError = "토큰 갱신 응답을 해석하지 못했습니다.";
                return false;
            }

            AccessToken = dto.access_token;
            _api.SetBearer(AccessToken);
            return true;
        }
        catch (HttpRequestException ex)
        {
            LastError = $"백엔드에 연결할 수 없습니다(토큰 갱신): {EnvConfig.BackendBaseUri} ({ex.Message}).";
            return false;
        }
        catch (TaskCanceledException)
        {
            LastError = $"토큰 갱신 요청 시간이 초과되었습니다: {EnvConfig.BackendBaseUri}.";
            return false;
        }
        catch (JsonException)
        {
            LastError = "토큰 갱신 응답 형식이 올바르지 않습니다.";
            return false;
        }
    }

    /// <summary>
    /// 보호 API(/protected) 호출로 현재 토큰이 유효한지 확인한다.
    /// 401이 반환되면 Refresh 토큰으로 재발급 시도 후 한 번 더 재시도한다.
    /// </summary>
    public async Task<(bool ok, string? currentUser, string? permission)> CheckProtectedAsync(CancellationToken ct = default)
    {
        LastError = null;
        try
        {
            var res = await _api.PostAsync("protected", ct).ConfigureAwait(false);
            if (res.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                var refreshed = await RefreshAsync(ct).ConfigureAwait(false);
                if (!refreshed) return (false, null, null);
                res = await _api.PostAsync("protected", ct).ConfigureAwait(false);
            }

            if (!res.IsSuccessStatusCode)
            {
                LastError = $"보호 API 호출 실패: {(int)res.StatusCode} {res.ReasonPhrase}";
                return (false, null, null);
            }

            var json = await res.Content.ReadAsStringAsync(ct).ConfigureAwait(false);
            var dto = JsonSerializer.Deserialize<ProtectedResponse>(json, BackendApiClient.JsonOptions);
            return dto is null ? (false, null, null) : (true, dto.logged_in_as, dto.permission);
        }
        catch (HttpRequestException ex)
        {
            LastError = $"백엔드에 연결할 수 없습니다(보호 API): {EnvConfig.BackendBaseUri} ({ex.Message}).";
            return (false, null, null);
        }
        catch (TaskCanceledException)
        {
            LastError = $"보호 API 요청 시간이 초과되었습니다: {EnvConfig.BackendBaseUri}.";
            return (false, null, null);
        }
        catch (JsonException)
        {
            LastError = "보호 API 응답 형식이 올바르지 않습니다.";
            return (false, null, null);
        }
    }

    private record LoginResponse(
        [property: JsonPropertyName("access_token")] string access_token,
        [property: JsonPropertyName("refresh_token")] string refresh_token,
        [property: JsonPropertyName("permission")] string permission
    );

    private record RefreshResponse(
        [property: JsonPropertyName("access_token")] string access_token,
        [property: JsonPropertyName("current_user")] string current_user
    );

    private record ProtectedResponse(
        [property: JsonPropertyName("logged_in_as")] string logged_in_as,
        [property: JsonPropertyName("permission")] string permission
    );
}
