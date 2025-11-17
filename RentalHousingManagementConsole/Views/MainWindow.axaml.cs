using System;
using System.ComponentModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Avalonia.Threading;
using RentalHousingManagementConsole.ViewModels;

namespace RentalHousingManagementConsole.Views;

public partial class MainWindow : Window
{
    private WindowState? _prevState;
    private SystemDecorations? _prevDecorations;
    private PixelPoint? _prevPosition;
    private double? _prevWidth;
    private double? _prevHeight;

    private bool _applyingFullScreen; // VM 동기화 루프 방지
    private MainViewModel? _vm;

    public MainWindow()
    {
        InitializeComponent();

        // DataContext은 ViewLocator에 의해 설정될 수 있으므로 변경 이벤트를 구독
        DataContextChanged += OnDataContextChanged;

        // 창 상태 변경(예: Alt+Enter 등) 시 VM과 동기화
        this.PropertyChanged += (_, args) =>
        {
            if (args.Property == WindowStateProperty && _vm is not null)
            {
                var isFs = WindowState == WindowState.FullScreen;
                if (_vm.IsFullScreen != isFs)
                {
                    if (_applyingFullScreen)
                        return; // View에서 적용 중일 때는 VM 업데이트를 억제
                    _vm.IsFullScreen = isFs;
                }
            }
        };
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (_vm is not null)
        {
            _vm.PropertyChanged -= OnVmPropertyChanged;
        }

        _vm = DataContext as MainViewModel;
        if (_vm is null) return;
        _vm.PropertyChanged += OnVmPropertyChanged;

        // 초기 상태 동기화
        ApplyFullScreen(_vm.IsFullScreen);
    }

    private void OnVmPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(MainViewModel.IsFullScreen) && _vm is not null)
        {
            ApplyFullScreen(_vm.IsFullScreen);
        }
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (!e.Handled && e.Key == Key.Escape && (_vm?.IsFullScreen == true || WindowState == WindowState.FullScreen))
        {
            // ESC로 전체화면 해제: VM이 있으면 VM을 통해, 없으면 직접 적용
            if (_vm is not null)
            {
                _vm.IsFullScreen = false;
            }
            else
            {
                ApplyFullScreen(false);
            }
            e.Handled = true;
            return;
        }
        base.OnKeyDown(e);
    }

    private void ApplyFullScreen(bool enable)
    {
        if (_applyingFullScreen) return;
        _applyingFullScreen = true;
        try
        {
            // Desktop 환경에서만 의미가 있음: Browser/Android 헤드에서는 이 Window가 사용되지 않음
            if (enable)
            {
                EnterFullScreen();
            }
            else
            {
                ExitFullScreen();
            }
        }
        finally
        {
            _applyingFullScreen = false;
        }
    }

    private void EnterFullScreen()
    {
        if (WindowState == WindowState.FullScreen) return;

        // 현재 상태 스냅샷 저장(최초 진입 시 1회)
        _prevState ??= WindowState;
        _prevDecorations ??= SystemDecorations;
        _prevPosition ??= Position;
        _prevWidth ??= Width;
        _prevHeight ??= Height;

        var wasMaximized = WindowState == WindowState.Maximized;

        // 다중 모니터 보정: 대상 스크린 좌표로 위치를 맞춘 후 전체화면 진입
        void ApplyDecorationsAndFullscreen()
        {
            // 전체화면 진입 순서: 데코 제거 → 위치 보정 → 전체화면
            SystemDecorations = SystemDecorations.None;

            // 현재 창이 위치한 스크린의 좌상단으로 위치 보정
            try
            {
                var screen = Screens.ScreenFromWindow(this) ?? Screens.Primary;
                var bounds = screen.Bounds;
                Position = bounds.Position; // 픽셀 좌표 기준
            }
            catch
            {
                // 스크린 정보가 없으면 보정 생략
            }

            WindowState = WindowState.FullScreen;

            // 전체화면 진입 직후 키 포커스/활성 보정: ESC 등 키 입력을 바로 받을 수 있게 함
            Dispatcher.UIThread.Post(() =>
            {
                try
                {
                    Activate();
                    Focus();
                }
                catch { /* no-op */ }
            });

            // Topmost는 필요시 활성화 가능: 전체화면 동안 다른 창 위에
            // Topmost = true;
        }

        if (wasMaximized)
        {
            // 일부 WM(특히 Wayland)에서 Maximized → FullScreen 전환 시 바로 적용하면 실패할 수 있음
            // 먼저 Normal로 전환하고 다음 UI 틱에 전체화면 적용
            WindowState = WindowState.Normal;
            Dispatcher.UIThread.Post(ApplyDecorationsAndFullscreen);
        }
        else
        {
            ApplyDecorationsAndFullscreen();
        }
    }

    private void ExitFullScreen()
    {
        // 일부 플랫폼에서는 Normal로 먼저 전환 후 데코 복원이 필요
        WindowState = WindowState.Normal;

        var decorationsToRestore = _prevDecorations ?? SystemDecorations.Full;
        // 즉시 적용이 안 되는 플랫폼을 위해 Dispatcher.Post로 다음 틱에 적용 보강
        Dispatcher.UIThread.Post(() =>
        {
            SystemDecorations = decorationsToRestore;

            // 위치/크기 복원(데코 복원 이후 적용)
            if (_prevPosition.HasValue) Position = _prevPosition.Value;
            if (_prevWidth.HasValue) Width = _prevWidth.Value;
            if (_prevHeight.HasValue) Height = _prevHeight.Value;

            // 최종 창 상태 복원(최대화 상태였으면 다시 최대화)
            WindowState = _prevState ?? WindowState.Normal;

            // 스냅샷 초기화
            _prevState = null;
            _prevDecorations = null;
            _prevPosition = null;
            _prevWidth = null;
            _prevHeight = null;

            // 필요 시 Topmost 해제
            // Topmost = false;
        });
    }
}