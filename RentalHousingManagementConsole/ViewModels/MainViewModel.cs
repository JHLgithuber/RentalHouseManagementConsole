using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace RentalHousingManagementConsole.ViewModels;

public enum PageType
{
    Dashboard,
    UnitManagement,
    RentPayment,
    UtilityBills,
    Statistics,
    Settings
}

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isPaneOpen;

    [ObservableProperty]
    private bool _isFullScreen;

    // 현재 활성화된 페이지를 나타내는 열거형
    [ObservableProperty]
    private PageType _currentPage = PageType.Dashboard;

    // 선택된 세대 정보
    [ObservableProperty]
    private UnitTileViewModel? _selectedUnit;

    // 다이얼로그 표시 여부
    [ObservableProperty]
    private bool _isDetailDialogOpen;

    [ObservableProperty]
    private bool _isEditDialogOpen;

    [RelayCommand]
    private void ToggleFullScreen()
    {
        IsFullScreen = !IsFullScreen;
    }

    // Flat list remains for potential usages
    public ObservableCollection<UnitTileViewModel> Units { get; } = new();

    // Grouped view by building (e.g., "101동")
    public ObservableCollection<UnitGroupViewModel> Groups { get; } = new();

    public MainViewModel()
    {
        // 초기 페이지: 대시보드
        CurrentPage = PageType.Dashboard;

        // Sample data for demonstration
        var sample = new[]
        {
            new UnitTileViewModel { UnitName = "101동 101호", Message = "정상", Metric = 0, Status = UnitStatus.Normal },
            new UnitTileViewModel { UnitName = "101동 102호", Message = "미납 1개월", Metric = 1, Status = UnitStatus.Attention },
            new UnitTileViewModel { UnitName = "101동 103호", Message = "미납 3개월", Metric = 3, Status = UnitStatus.Urgent },
            new UnitTileViewModel { UnitName = "101동 104호", Message = "공실", Metric = null, Status = UnitStatus.Vacant },
            new UnitTileViewModel { UnitName = "101동 105호", Message = "수리 중", Metric = null, Status = UnitStatus.Maintenance },
        };

        // Create multiple buildings and units for grouping demo
        // Buildings: 101동, 102동, 103동
        for (int i = 0; i < 36; i++)
        {
            var item = sample[i % sample.Length];
            var dongNumber = 101 + (i / 12); // 12 units per building
            var unitNumber = 101 + (i % 12);

            var vm = new UnitTileViewModel
            {
                UnitName = $"{dongNumber}동 {unitNumber}호",
                Message = item.Message,
                Metric = item.Metric.HasValue ? (double?)(item.Metric.Value + (i % 2)) : null,
                Status = item.Status
            };

            Units.Add(vm);

            // Group by dong (e.g., "101동")
            var groupName = $"{dongNumber}동";
            var group = Groups.FirstOrDefault(g => g.GroupName == groupName);
            if (group is null)
            {
                group = new UnitGroupViewModel { GroupName = groupName };
                Groups.Add(group);
            }
            group.Units.Add(vm);
        }
    }

    [RelayCommand]
    private void OpenDashboard()
    {
        CurrentPage = PageType.Dashboard;
        IsPaneOpen = false;
    }

    [RelayCommand]
    private void OpenUnitManagement()
    {
        CurrentPage = PageType.UnitManagement;
        IsPaneOpen = false;
    }

    [RelayCommand]
    private void OpenRentPayment()
    {
        CurrentPage = PageType.RentPayment;
        IsPaneOpen = false;
    }

    [RelayCommand]
    private void OpenUtilityBills()
    {
        CurrentPage = PageType.UtilityBills;
        IsPaneOpen = false;
    }

    [RelayCommand]
    private void OpenStatistics()
    {
        CurrentPage = PageType.Statistics;
        IsPaneOpen = false;
    }

    [RelayCommand]
    private void OpenSettings()
    {
        CurrentPage = PageType.Settings;
        IsPaneOpen = false;
    }

    [RelayCommand]
    private void ShowUnitDetail(UnitTileViewModel unit)
    {
        SelectedUnit = unit;
        IsDetailDialogOpen = true;
    }

    [RelayCommand]
    private void ShowUnitEdit(UnitTileViewModel unit)
    {
        SelectedUnit = unit;
        IsEditDialogOpen = true;
    }

    [RelayCommand]
    private void CloseDetailDialog()
    {
        IsDetailDialogOpen = false;
    }

    [RelayCommand]
    private void CloseEditDialog()
    {
        IsEditDialogOpen = false;
    }

    [RelayCommand]
    private void SaveUnitEdit()
    {
        // 저장 로직 (추후 구현)
        IsEditDialogOpen = false;
    }
}
