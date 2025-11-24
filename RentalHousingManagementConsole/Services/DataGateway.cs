using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using SocketIOClient;

namespace RentalHousingManagementConsole.Services;

/// <summary>
/// 백엔드 Socket.IO 게이트웨이. CRUD(read/update/create/delete) 이벤트를 통해 데이터 연동.
/// </summary>
public sealed class DataGateway : IAsyncDisposable
{
    private static readonly Lazy<DataGateway> _instance = new(() => new DataGateway());
    public static DataGateway Instance => _instance.Value;

    private readonly object _gate = new();
    private SocketIO? _client;
    private string? _accessToken;
    // Socket.IO 클라이언트에는 HTTP(S) 베이스 URL을 주고, Path 옵션으로 "/socket.io"를 지정한다.
    private Uri _baseHttpUri = EnvConfig.BackendBaseUri;

    private DataGateway() { }

    public void SetAccessToken(string? token) => _accessToken = token;

    public async Task ConnectIfNeededAsync(CancellationToken ct = default)
    {
        if (_client is { Connected: true }) return;
        lock (_gate)
        {
            _client ??= new SocketIO(_baseHttpUri.ToString(), new SocketIOOptions
            {
                Reconnection = true,
                ReconnectionAttempts = 3,
                ReconnectionDelay = 1000,
                EIO = 4,
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket,
                Path = "/socket.io"
            });
        }

        // 기본 에러 로깅
        _client!.On("error_data", resp =>
        {
            try
            {
                var msg = resp.GetValue<string>();
                System.Diagnostics.Debug.WriteLine("[SIO-ERROR] " + msg);
            }
            catch { /* ignore */ }
        });

        // SocketIOClient v3 ConnectAsync에는 CancellationToken 오버로드가 없음
        await _client.ConnectAsync().ConfigureAwait(false);
    }

    public async Task DisconnectAsync()
    {
        if (_client is null) return;
        try { await _client.DisconnectAsync(); } catch { /* ignore */ }
        try { _client.Dispose(); } catch { /* ignore */ }
        _client = null;
    }

    public async Task<JsonElement?> ReadAsync(string entity, object? where = null, object? option = null, object? property = null, CancellationToken ct = default)
        => await RequestAsync("read_data", entity, where, option, property, data: null, ct).ConfigureAwait(false);

    public async Task<JsonElement?> CreateAsync(string entity, object? data, object? option = null, CancellationToken ct = default)
        => await RequestAsync("create_data", entity, where: null, option, property: null, data, ct).ConfigureAwait(false);

    public async Task<JsonElement?> UpdateAsync(string entity, object? data, object? where = null, CancellationToken ct = default)
        => await RequestAsync("update_data", entity, where, option: null, property: null, data, ct).ConfigureAwait(false);

    public async Task<JsonElement?> DeleteAsync(string entity, object? where = null, CancellationToken ct = default)
        => await RequestAsync("delete_data", entity, where, option: null, property: null, data: null, ct).ConfigureAwait(false);

    private async Task<JsonElement?> RequestAsync(string eventName, string entity, object? where, object? option, object? property, object? data, CancellationToken ct)
    {
        await ConnectIfNeededAsync(ct).ConfigureAwait(false);
        if (_client is null) throw new InvalidOperationException("SocketIO 클라이언트가 초기화되지 않았습니다.");

        var tcs = new TaskCompletionSource<JsonElement?>(TaskCreationOptions.RunContinuationsAsynchronously);
        Action<SocketIOResponse> handler = null!;
        handler = response =>
        {
            try
            {
                var jsonStr = response.GetValue<string>();
                using var doc = JsonDocument.Parse(jsonStr);
                if (doc.RootElement.TryGetProperty("JSON_DATA", out var jsonData))
                {
                    tcs.TrySetResult(jsonData);
                }
                else
                {
                    tcs.TrySetResult(doc.RootElement);
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
            finally
            {
                try { _client?.Off("responsed_data"); } catch { /* ignore */ }
            }
        };
        _client.On("responsed_data", handler);

        var payload = new Dictionary<string, object?>
        {
            ["access_token"] = _accessToken,
            ["entity"] = entity,
            ["where"] = where,
            ["option"] = option,
            ["property"] = property,
            ["data"] = data
        };

        await _client.EmitAsync(eventName, payload).ConfigureAwait(false);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
        cts.CancelAfter(TimeSpan.FromSeconds(10));
        using var reg = cts.Token.Register(() => tcs.TrySetCanceled(cts.Token));

        return await tcs.Task.ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        await DisconnectAsync();
    }
}
