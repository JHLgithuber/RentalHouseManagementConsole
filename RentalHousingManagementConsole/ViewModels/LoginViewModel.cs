using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalHousingManagementConsole.Services;

namespace RentalHousingManagementConsole.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authService;
    private readonly AuthService _authApi = AuthService.Instance;

    [ObservableProperty]
    private string _username = string.Empty;

    [ObservableProperty]
    private string _password = string.Empty;

    [ObservableProperty]
    private bool _isAuthenticating;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _isWindowsHelloAvailable;

    public LoginViewModel()
    {
        _authService = new WindowsHelloService();
        IsWindowsHelloAvailable = _authService.IsAvailable();
    }

    [RelayCommand]
    private async Task LoginWithPassword()
    {
        ErrorMessage = string.Empty;
        
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "사용자명과 비밀번호를 입력하세요.";
            return;
        }

        IsAuthenticating = true;

        try
        {
            // 실제 백엔드 로그인 연동
            var ok = await _authApi.LoginAsync(Username, Password).ConfigureAwait(false);
            if (ok)
            {
                OnLoginSuccess?.Invoke();
            }
            else
            {
                ErrorMessage = _authApi.LastError ?? "로그인에 실패했습니다. 사용자명 또는 비밀번호를 확인하세요.";
            }
        }
        catch (System.Exception ex)
        {
            ErrorMessage = $"로그인 중 오류가 발생했습니다: {ex.Message}";
        }
        finally
        {
            IsAuthenticating = false;
        }
    }

    [RelayCommand]
    private async Task LoginWithWindowsHello()
    {
        ErrorMessage = string.Empty;
        IsAuthenticating = true;

        try
        {
            var success = await _authService.AuthenticateAsync();
            
            if (success)
            {
                OnLoginSuccess?.Invoke();
            }
            else
            {
                ErrorMessage = "Windows Hello 인증에 실패했습니다.";
            }
        }
        finally
        {
            IsAuthenticating = false;
        }
    }

    public event System.Action? OnLoginSuccess;
}
