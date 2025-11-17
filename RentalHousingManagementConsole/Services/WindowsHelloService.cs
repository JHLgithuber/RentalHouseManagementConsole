using System;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace RentalHousingManagementConsole.Services;

public interface IAuthenticationService
{
    Task<bool> AuthenticateAsync();
    bool IsAvailable();
}

public class WindowsHelloService : IAuthenticationService
{
    // Windows Credential UI API
    [DllImport("credui.dll", CharSet = CharSet.Unicode)]
    private static extern int CredUIPromptForWindowsCredentials(
        ref CREDUI_INFO pUiInfo,
        int dwAuthError,
        ref uint pulAuthPackage,
        IntPtr pvInAuthBuffer,
        uint ulInAuthBufferSize,
        out IntPtr ppvOutAuthBuffer,
        out uint pulOutAuthBufferSize,
        ref bool pfSave,
        uint dwFlags);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct CREDUI_INFO
    {
        public int cbSize;
        public IntPtr hwndParent;
        public string pszMessageText;
        public string pszCaptionText;
        public IntPtr hbmBanner;
    }

    private const uint CREDUIWIN_GENERIC = 0x1;
    private const uint CREDUIWIN_ENUMERATE_CURRENT_USER = 0x200;

    public bool IsAvailable()
    {
        // Windows 플랫폼이고 Windows 10 이상인지 확인
        return OperatingSystem.IsWindows() && OperatingSystem.IsWindowsVersionAtLeast(10);
    }

    public async Task<bool> AuthenticateAsync()
    {
        if (!IsAvailable())
        {
            return false;
        }

        return await Task.Run(() =>
        {
            try
            {
                var credui = new CREDUI_INFO
                {
                    cbSize = Marshal.SizeOf(typeof(CREDUI_INFO)),
                    hwndParent = IntPtr.Zero,
                    pszCaptionText = "임대주택 관리 시스템",
                    pszMessageText = "Windows Hello 또는 PIN으로 신원을 확인하세요",
                    hbmBanner = IntPtr.Zero
                };

                uint authPackage = 0;
                IntPtr outCredBuffer = IntPtr.Zero;
                uint outCredSize = 0;
                bool save = false;

                // Windows 인증 프롬프트 표시 (Windows Hello, PIN, 비밀번호 포함)
                int result = CredUIPromptForWindowsCredentials(
                    ref credui,
                    0,
                    ref authPackage,
                    IntPtr.Zero,
                    0,
                    out outCredBuffer,
                    out outCredSize,
                    ref save,
                    CREDUIWIN_GENERIC | CREDUIWIN_ENUMERATE_CURRENT_USER);

                // 메모리 해제
                if (outCredBuffer != IntPtr.Zero)
                {
                    Marshal.FreeCoTaskMem(outCredBuffer);
                }

                // 0은 성공, 1223은 사용자 취소
                return result == 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Windows 인증 실패: {ex.Message}");
                return false;
            }
        });
    }
}
