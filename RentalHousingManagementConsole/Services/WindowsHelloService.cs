using System;
using System.Runtime.InteropServices;
using System.Security;
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

    [DllImport("credui.dll", CharSet = CharSet.Unicode)]
    private static extern bool CredUnPackAuthenticationBuffer(
        uint dwFlags,
        IntPtr pAuthBuffer,
        uint cbAuthBuffer,
        IntPtr pszUserName,
        ref uint pcchMaxUserName,
        IntPtr pszDomainName,
        ref uint pcchMaxDomainName,
        IntPtr pszPassword,
        ref uint pcchMaxPassword);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool LogonUser(
        string lpszUsername,
        string? lpszDomain,
        IntPtr lpszPassword,
        int dwLogonType,
        int dwLogonProvider,
        out IntPtr phToken);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(IntPtr hObject);

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
    private const uint CRED_PACK_PROTECTED_CREDENTIALS = 0x1;
    private const int LOGON32_LOGON_INTERACTIVE = 2;
    private const int LOGON32_PROVIDER_DEFAULT = 0;

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
                    pszMessageText = "Windows 계정으로 신원을 확인하세요",
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

                if (result != 0)
                {
                    // 사용자가 취소했거나 오류 발생
                    return false;
                }

                // 자격 증명 추출
                uint usernameSize = 256;
                uint domainSize = 256;
                uint passwordSize = 256;

                IntPtr usernamePtr = Marshal.AllocHGlobal((int)usernameSize * sizeof(char));
                IntPtr domainPtr = Marshal.AllocHGlobal((int)domainSize * sizeof(char));
                IntPtr passwordPtr = Marshal.AllocHGlobal((int)passwordSize * sizeof(char));

                try
                {
                    bool unpackResult = CredUnPackAuthenticationBuffer(
                        0,
                        outCredBuffer,
                        outCredSize,
                        usernamePtr,
                        ref usernameSize,
                        domainPtr,
                        ref domainSize,
                        passwordPtr,
                        ref passwordSize);

                    if (outCredBuffer != IntPtr.Zero)
                    {
                        Marshal.FreeCoTaskMem(outCredBuffer);
                    }

                    if (!unpackResult)
                    {
                        return false;
                    }

                    string username = Marshal.PtrToStringUni(usernamePtr) ?? "";
                    string domain = Marshal.PtrToStringUni(domainPtr) ?? "";

                    // Windows 계정으로 실제 인증 시도
                    IntPtr token = IntPtr.Zero;
                    bool logonSuccess = LogonUser(
                        username,
                        string.IsNullOrEmpty(domain) ? null : domain,
                        passwordPtr,
                        LOGON32_LOGON_INTERACTIVE,
                        LOGON32_PROVIDER_DEFAULT,
                        out token);

                    if (token != IntPtr.Zero)
                    {
                        CloseHandle(token);
                    }

                    return logonSuccess;
                }
                finally
                {
                    // 보안을 위해 메모리를 0으로 초기화 후 해제
                    if (usernamePtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(usernamePtr);
                    }
                    if (domainPtr != IntPtr.Zero)
                    {
                        Marshal.FreeHGlobal(domainPtr);
                    }
                    if (passwordPtr != IntPtr.Zero)
                    {
                        // 비밀번호 메모리를 0으로 초기화
                        for (int i = 0; i < (int)passwordSize * sizeof(char); i++)
                        {
                            Marshal.WriteByte(passwordPtr, i, 0);
                        }
                        Marshal.FreeHGlobal(passwordPtr);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Windows 인증 실패: {ex.Message}");
                return false;
            }
        });
    }
}
