using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace RentalHousingManagementConsole.Services;

/// <summary>
/// Avalonia 앱 구동 시 Python Flask 백엔드를 함께 실행/중지하는 서비스.
/// </summary>
public sealed class BackendProcessService : IAsyncDisposable, IDisposable
{
    private Process? _process;
    private readonly HttpClient _http = new();
    private readonly Uri _baseUri;

    public BackendProcessService(string? baseUrl = null)
    {
        _baseUri = new Uri(baseUrl ?? "http://127.0.0.1:5000/");
    }

    /// <summary>
    /// 백엔드 프로세스를 시작한다. 이미 실행 중이면 아무것도 하지 않는다.
    /// </summary>
    public async Task StartAsync(CancellationToken ct = default)
    {
        if (await IsAliveAsync(ct).ConfigureAwait(false))
            return;

        var (pythonExe, workDir, scriptPath) = ResolveBackendPaths();

        var psi = new ProcessStartInfo
        {
            FileName = pythonExe,
            Arguments = Quote(scriptPath),
            WorkingDirectory = workDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        _process = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _process.OutputDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Debug.WriteLine("[PY-OUT] " + e.Data); };
        _process.ErrorDataReceived += (_, e) => { if (!string.IsNullOrWhiteSpace(e.Data)) Debug.WriteLine("[PY-ERR] " + e.Data); };

        if (!_process.Start())
            throw new InvalidOperationException("Python 백엔드 프로세스를 시작하지 못했습니다.");

        _process.BeginOutputReadLine();
        _process.BeginErrorReadLine();

        // 기동 대기: 최대 20초 동안 HTTP로 확인
        var timeout = TimeSpan.FromSeconds(20);
        var sw = Stopwatch.StartNew();
        while (sw.Elapsed < timeout && !ct.IsCancellationRequested)
        {
            if (await IsAliveAsync(ct).ConfigureAwait(false))
                return;
            await Task.Delay(500, ct).ConfigureAwait(false);
        }

        throw new TimeoutException("Python 백엔드가 기동되지 않았습니다 (타임아웃).");
    }

    /// <summary>
    /// 간단한 HTTP GET으로 서버가 살아있는지 확인한다. 어떤 상태코드든 응답만 오면 살아있는 것으로 간주.
    /// </summary>
    public async Task<bool> IsAliveAsync(CancellationToken ct = default)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, _baseUri);
            using var res = await _http.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, ct).ConfigureAwait(false);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static string Quote(string path)
        => path.Contains(' ') ? $"\"{path}\"" : path;

    /// <summary>
    /// 파이썬 실행 파일, 작업 디렉터리(HM_Backend), 실행 스크립트 경로(run_unsecu.py)를 결정한다.
    /// - 환경변수 HM_BACKEND_PYTHON 가 우선.
    /// - 없으면 레포 내 가상환경 HM_Backend\myenv_of_HM\Scripts\python.exe (Windows) / bin/python (Unix) 시도.
    /// </summary>
    private static (string pythonExe, string workDir, string scriptPath) ResolveBackendPaths()
    {
        // 저장소 루트 추정: 실행 파일 기준 상위 디렉터리를 타고 올라가 HM_Backend를 찾는다.
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        DirectoryInfo? repoRoot = null;
        for (var i = 0; i < 10 && current is not null; i++, current = current.Parent!)
        {
            if (Directory.Exists(Path.Combine(current.FullName, "HM_Backend")))
            {
                repoRoot = current;
                break;
            }
        }

        if (repoRoot is null)
            throw new DirectoryNotFoundException("HM_Backend 디렉터리를 찾지 못했습니다. 앱의 실행 위치를 확인하세요.");

        var backendDir = Path.Combine(repoRoot.FullName, "HM_Backend");
        var script = Path.Combine(backendDir, "run_unsecu.py");
        if (!File.Exists(script))
        {
            // 보안 모드 스크립트가 없으면 run.py로 폴백
            script = Path.Combine(backendDir, "run.py");
        }

        string? pythonFromEnv = Environment.GetEnvironmentVariable("HM_BACKEND_PYTHON");
        string pythonExe;

        if (!string.IsNullOrWhiteSpace(pythonFromEnv) && File.Exists(pythonFromEnv))
        {
            pythonExe = pythonFromEnv;
        }
        else
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                pythonExe = Path.Combine(backendDir, "myenv_of_HM", "Scripts", "python.exe");
            }
            else
            {
                pythonExe = Path.Combine(backendDir, "myenv_of_HM", "bin", "python");
            }

            if (!File.Exists(pythonExe))
            {
                // 최후 수단: PATH에 있는 python
                pythonExe = "python";
            }
        }

        return (pythonExe, backendDir, script);
    }

    public void Dispose()
    {
        try
        {
            if (_process is { HasExited: false })
            {
                _process.Kill(true);
            }
        }
        catch { /* ignore */ }
        finally
        {
            _process?.Dispose();
            _http.Dispose();
        }
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        await Task.CompletedTask;
    }
}
