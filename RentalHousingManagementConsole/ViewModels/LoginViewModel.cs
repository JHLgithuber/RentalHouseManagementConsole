using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalHousingManagementConsole.Services;

namespace RentalHousingManagementConsole.ViewModels;

public partial class LoginViewModel : ViewModelBase
{
    private readonly IAuthenticationService _authService;

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
            // 시뮬레이션: 실제로는 서버나 로컬 DB에서 인증
            await Task.Delay(500);

            // 데모 목적: admin/admin으로 로그인
            if (Username == "admin" && Password == "admin")
            {
                OnLoginSuccess?.Invoke();
            }
            else
            {
                ErrorMessage = "사용자명 또는 비밀번호가 올바르지 않습니다.";
            }
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
