using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Data.Core;
using Avalonia.Data.Core.Plugins;
using System.Linq;
using Avalonia.Markup.Xaml;
using RentalHousingManagementConsole.ViewModels;
using RentalHousingManagementConsole.Views;
using RentalHousingManagementConsole.Services;
using System.Threading.Tasks;

namespace RentalHousingManagementConsole;

public partial class App : Application
{
    private BackendProcessService? _backend;
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Avoid duplicate validations from both Avalonia and the CommunityToolkit. 
            // More info: https://docs.avaloniaui.net/docs/guides/development-guides/data-validation#manage-validationplugins
            DisableAvaloniaDataAnnotationValidation();

            // 원격 백엔드 기본 사용. 필요 시 로컬 백엔드 프로세스 옵션 구동
            if (EnvConfig.UseLocalBackend)
            {
                _backend = new BackendProcessService(EnvConfig.BackendBaseUri.ToString());
                _ = Task.Run(async () =>
                {
                    try { await _backend.StartAsync(); }
                    catch { /* TODO: 사용자에게 알림/로그 */ }
                });
            }

            desktop.Exit += (_, _) =>
            {
                _backend?.Dispose();
                _backend = null;
            };
            desktop.MainWindow = new MainWindow
            {
                DataContext = new MainViewModel()
            };
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewPlatform)
        {
            // 단일 뷰 수명주기에서도 옵션에 따라 로컬 백엔드 구동
            if (EnvConfig.UseLocalBackend)
            {
                _backend = new BackendProcessService(EnvConfig.BackendBaseUri.ToString());
                _ = Task.Run(async () =>
                {
                    try { await _backend.StartAsync(); }
                    catch { /* ignore */ }
                });
            }
            singleViewPlatform.MainView = new MainView
            {
                DataContext = new MainViewModel()
            };
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void DisableAvaloniaDataAnnotationValidation()
    {
        // Get an array of plugins to remove
        var dataValidationPluginsToRemove =
            BindingPlugins.DataValidators.OfType<DataAnnotationsValidationPlugin>().ToArray();

        // remove each entry found
        foreach (var plugin in dataValidationPluginsToRemove)
        {
            BindingPlugins.DataValidators.Remove(plugin);
        }
    }
}