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
    private TaskCompletionSource<bool>? _connectionTcs;
    // Socket.IO 클라이언트에는 HTTP(S) 베이스 URL을 주고, Path 옵션으로 "/socket.io"를 지정한다.
    private Uri _baseHttpUri = EnvConfig.BackendBaseUri;

    private DataGateway() { }

    public void SetAccessToken(string? token) => _accessToken = token;

    public async Task ConnectIfNeededAsync(CancellationToken ct = default)
    {
        if (_client is { Connected: true }) return;

        bool needsConnection = false;
        lock (_gate)
        {
            if (_client is null)
            {
                needsConnection = true;
                _connectionTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

                _client = new SocketIO(_baseHttpUri.ToString(), new SocketIOOptions
                {
                    Reconnection = true,
                    ReconnectionAttempts = 5,
                    ReconnectionDelay = 2000,
                    EIO = 4,  // Engine.IO v4 as required by backend
                    Transport = SocketIOClient.Transport.TransportProtocol.Polling,  // Start with polling, auto-upgrade to WebSocket
                    Path = "/socket.io"
                });

                // OnConnected 이벤트 핸들러 등록
                _client.OnConnected += (sender, e) =>
                {
                    Console.WriteLine($"=== OnConnected Event Fired ===");
                    Console.WriteLine($"Connected: {_client.Connected}");
                    Console.WriteLine($"ID: {_client.Id}");
                    Console.WriteLine("================================");
                    _connectionTcs?.TrySetResult(true);
                };

                // OnDisconnected 이벤트 핸들러
                _client.OnDisconnected += (sender, e) =>
                {
                    Console.WriteLine($"=== OnDisconnected Event ===");
                    Console.WriteLine($"Reason: {e}");
                    Console.WriteLine("============================");
                };

                // 기본 에러 로깅
                _client.On("error_data", resp =>
                {
                    try
                    {
                        var msg = resp.GetValue<string>();
                        Console.WriteLine("=== SocketIO Error ===");
                        Console.WriteLine($"[SIO-ERROR] {msg}");
                        Console.WriteLine("======================");
                        System.Diagnostics.Debug.WriteLine("[SIO-ERROR] " + msg);
                    }
                    catch { /* ignore */ }
                });
            }
        }

        if (!needsConnection) return;

        Console.WriteLine($"=== Connecting to SocketIO Server ===");
        Console.WriteLine($"Base URI: {_baseHttpUri}");
        Console.WriteLine($"EIO: 4, Transport: Polling -> WebSocket");
        Console.WriteLine("=====================================");

        // SocketIOClient v3 ConnectAsync에는 CancellationToken 오버로드가 없음
        try
        {
            await _client!.ConnectAsync().ConfigureAwait(false);

            // OnConnected 이벤트를 대기 (최대 10초)
            using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            using var registration = cts.Token.Register(() => _connectionTcs?.TrySetCanceled());

            await _connectionTcs!.Task.ConfigureAwait(false);

            Console.WriteLine($"=== SocketIO Fully Connected ===");
            Console.WriteLine($"Connected: {_client.Connected}");
            Console.WriteLine($"ID: {_client.Id}");
            Console.WriteLine("================================");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"=== SocketIO Connection Failed ===");
            Console.WriteLine($"Error: {ex.Message}");
            Console.WriteLine($"StackTrace: {ex.StackTrace}");
            Console.WriteLine("===================================");
            throw;
        }
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

                // 디버깅: 수신한 원본 JSON 출력
                Console.WriteLine("=== SocketIO Response Received ===");
                Console.WriteLine($"Event: {eventName}");
                Console.WriteLine($"Entity: {entity}");
                Console.WriteLine($"Raw JSON: {jsonStr}");
                Console.WriteLine("==================================");

                using var doc = JsonDocument.Parse(jsonStr);
                if (doc.RootElement.TryGetProperty("JSON_DATA", out var jsonData))
                {
                    Console.WriteLine($"JSON_DATA found, type: {jsonData.ValueKind}");
                    if (jsonData.ValueKind == JsonValueKind.Array)
                    {
                        Console.WriteLine($"Array length: {jsonData.GetArrayLength()}");
                        // 첫 번째 항목 샘플 출력
                        if (jsonData.GetArrayLength() > 0)
                        {
                            var firstItem = jsonData[0];
                            Console.WriteLine($"First item sample: {firstItem}");
                        }
                    }
                    // JsonElement를 Clone()해서 반환 (JsonDocument가 dispose되어도 유효)
                    tcs.TrySetResult(jsonData.Clone());
                }
                else
                {
                    Console.WriteLine("JSON_DATA property not found, returning root element");
                    // JsonElement를 Clone()해서 반환
                    tcs.TrySetResult(doc.RootElement.Clone());
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== SocketIO Response Error ===");
                Console.WriteLine($"Error: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
                Console.WriteLine("===============================");
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

        // 디버깅: 요청 페이로드 출력
        Console.WriteLine("=== SocketIO Request Sent ===");
        Console.WriteLine($"Event: {eventName}");
        Console.WriteLine($"Payload: {JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true })}");
        Console.WriteLine("=============================");

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
