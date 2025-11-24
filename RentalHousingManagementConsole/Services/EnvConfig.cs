using System;
using System.Collections.Generic;
using System.IO;

namespace RentalHousingManagementConsole.Services;

/// <summary>
/// .env 파일과 환경변수에서 백엔드 설정을 로드한다.
/// 우선순위: 환경변수 > .env 파일 > 기본값
/// </summary>
public static class EnvConfig
{
    // 키 상수
    private const string KEY_BASE_URL = "BACKEND_BASE_URL";
    private const string KEY_USE_LOCAL = "HM_USE_LOCAL_BACKEND";

    private static readonly Lazy<Dictionary<string, string>> _env = new(LoadEnvInternal);

    /// <summary>
    /// 백엔드 기본 주소(예: https://api.example.com/ 또는 http://127.0.0.1:5000/)
    /// </summary>
    public static Uri BackendBaseUri
    {
        get
        {
            var str = ReadSetting(KEY_BASE_URL);
            if (string.IsNullOrWhiteSpace(str))
            {
                // 기본값: 로컬 개발 서버
                str = "http://127.0.0.1:5000/";
            }

            if (!Uri.TryCreate(AddTrailingSlash(str.Trim()), UriKind.Absolute, out var uri))
                throw new InvalidDataException($"유효하지 않은 BACKEND_BASE_URL: {str}");
            return uri;
        }
    }

    /// <summary>
    /// 로컬 파이썬 백엔드를 앱이 직접 구동할지 여부.
    /// 기본값: false (원격 백엔드 사용)
    /// </summary>
    public static bool UseLocalBackend
    {
        get
        {
            var v = ReadSetting(KEY_USE_LOCAL);
            if (bool.TryParse(v, out var b)) return b;
            // 일부 값 허용
            if (string.Equals(v, "1") || string.Equals(v, "yes", StringComparison.OrdinalIgnoreCase) || string.Equals(v, "y", StringComparison.OrdinalIgnoreCase))
                return true;
            return false;
        }
    }

    private static string AddTrailingSlash(string s)
        => s.EndsWith("/") ? s : s + "/";

    private static string ReadSetting(string key)
    {
        // 1) 환경변수
        var fromEnv = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrWhiteSpace(fromEnv)) return fromEnv!;

        // 2) .env 로드된 캐시
        if (_env.Value.TryGetValue(key, out var value)) return value;

        return string.Empty;
    }

    private static Dictionary<string, string> LoadEnvInternal()
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in GetCandidateEnvPaths())
        {
            try
            {
                if (!File.Exists(path)) continue;
                foreach (var line in File.ReadAllLines(path))
                {
                    var l = line?.Trim();
                    if (string.IsNullOrEmpty(l)) continue;
                    if (l.StartsWith("#")) continue;
                    var idx = l.IndexOf('=');
                    if (idx <= 0) continue;
                    var k = l.Substring(0, idx).Trim();
                    var v = l.Substring(idx + 1).Trim().Trim('"');
                    if (!string.IsNullOrEmpty(k))
                        dict[k] = v;
                }
            }
            catch
            {
                // ignore and continue next path
            }
        }

        return dict;
    }

    private static IEnumerable<string> GetCandidateEnvPaths()
    {
        // 앱 실행 경로
        var baseDir = AppContext.BaseDirectory;
        if (!string.IsNullOrWhiteSpace(baseDir))
            yield return Path.Combine(baseDir, ".env");

        // 저장소 루트 탐색 (HM_Backend 디렉토리 기준)
        var current = new DirectoryInfo(baseDir);
        for (var i = 0; i < 8 && current is not null; i++, current = current.Parent!)
        {
            var envPath = Path.Combine(current.FullName, ".env");
            if (File.Exists(envPath))
            {
                yield return envPath;
            }
        }
    }
}
