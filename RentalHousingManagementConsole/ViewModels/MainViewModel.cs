using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using RentalHousingManagementConsole.Services;
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

    // 로딩 여부 플래그 (중복 로드 방지)
    private bool _unitsLoaded;
    private bool _rentLoaded;
    private bool _utilitiesLoaded;
    private bool _statisticsLoaded;

    public MainViewModel()
    {
        // 초기 페이지: 대시보드. 샘플 데이터는 완전히 제거한다.
        CurrentPage = PageType.Dashboard;
    }

    /// <summary>
    /// 로그인 직후 서버에서 초기 데이터를 가져온다. 실패 시 샘플 데이터 유지.
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            // Socket.IO 연결 보장 및 유닛 데이터 조회 시도
            await DataGateway.Instance.ConnectIfNeededAsync().ConfigureAwait(false);

            // 첫 진입 시 세대 데이터 로드
            await LoadUnitsAsync().ConfigureAwait(false);
        }
        catch (Exception)
        {
            // 조회 실패 시 아무 것도 표시하지 않음 (샘플 데이터 없음)
        }
    }

    private async Task LoadUnitsAsync()
    {
        if (_unitsLoaded) return;
        _unitsLoaded = true;
        try
        {
            // 엔터티 명은 백엔드 mgmt_class 설계에 따라 변경될 수 있음. 기본 값으로 "Units"를 시도.
            var json = await DataGateway.Instance.ReadAsync("Units").ConfigureAwait(false);
            if (json is JsonElement el)
            {
                // JSON_DATA가 배열이라고 가정하고 매핑 시도: { unitName, message, metric, status }
                if (el.ValueKind == JsonValueKind.Array)
                {
                    // 백엔드에서 유효한 배열을 응답했다면(비어 있어도) 컬렉션을 비우고 실제 상태를 반영한다.
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Units.Clear();
                        Groups.Clear();
                    });

                    var list = el.EnumerateArray().ToList();
                    if (list.Count == 0)
                    {
                        // 빈 DB: 컬렉션을 비운 상태로 유지
                        return;
                    }

                    foreach (var item in list)
                    {
                        var unitName = item.TryGetProperty("unitName", out var p1) ? p1.GetString() : null;
                        unitName ??= item.TryGetProperty("UnitName", out var p1b) ? p1b.GetString() : null;
                        var message = item.TryGetProperty("message", out var p2) ? p2.GetString() : null;
                        var metric = item.TryGetProperty("metric", out var p3) && p3.ValueKind == JsonValueKind.Number ? p3.GetDouble() : (double?)null;
                        var statusStr = item.TryGetProperty("status", out var p4) ? p4.GetString() : null;

                        var status = statusStr switch
                        {
                            "Normal" => UnitStatus.Normal,
                            "Attention" => UnitStatus.Attention,
                            "Urgent" => UnitStatus.Urgent,
                            "Vacant" => UnitStatus.Vacant,
                            "Maintenance" => UnitStatus.Maintenance,
                            _ => UnitStatus.Normal
                        };

                        if (string.IsNullOrWhiteSpace(unitName))
                            continue;

                        var vm = new UnitTileViewModel
                        {
                            UnitName = unitName!,
                            Message = message ?? string.Empty,
                            Metric = metric,
                            Status = status
                        };

                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            Units.Add(vm);
                            var idx = unitName!.IndexOf(' ');
                            var groupName = idx > 0 ? unitName.Substring(0, idx) : "기타";
                            var group = Groups.FirstOrDefault(g => g.GroupName == groupName);
                            if (group is null)
                            {
                                group = new UnitGroupViewModel { GroupName = groupName };
                                Groups.Add(group);
                            }
                            group.Units.Add(vm);
                        });
                    }
                }
            }
        }
        catch (Exception)
        {
            // 조회 실패 시 샘플 데이터 유지 (무시)
        }
    }

    [RelayCommand]
    private void OpenDashboard()
    {
        CurrentPage = PageType.Dashboard;
        IsPaneOpen = false;
    }

    [RelayCommand]
    private async Task OpenUnitManagement()
    {
        CurrentPage = PageType.UnitManagement;
        IsPaneOpen = false;
        try { await LoadUnitsAsync().ConfigureAwait(false); } catch { /* ignore */ }
    }

    [RelayCommand]
    private async Task OpenRentPayment()
    {
        CurrentPage = PageType.RentPayment;
        IsPaneOpen = false;
        // DB 연동 골격: 엔터티 이름은 백엔드와 합의 필요. 예시로 "RentPayments" 사용.
        if (_rentLoaded) return;
        _rentLoaded = true;
        try
        {
            await DataGateway.Instance.ConnectIfNeededAsync().ConfigureAwait(false);
            await DataGateway.Instance.ReadAsync("RentPayments").ConfigureAwait(false);
        }
        catch { /* ignore */ }
    }

    [RelayCommand]
    private async Task OpenUtilityBills()
    {
        CurrentPage = PageType.UtilityBills;
        IsPaneOpen = false;
        if (_utilitiesLoaded) return;
        _utilitiesLoaded = true;
        try
        {
            await DataGateway.Instance.ConnectIfNeededAsync().ConfigureAwait(false);
            await DataGateway.Instance.ReadAsync("UtilityBills").ConfigureAwait(false);
        }
        catch { /* ignore */ }
    }

    [RelayCommand]
    private async Task OpenStatistics()
    {
        CurrentPage = PageType.Statistics;
        IsPaneOpen = false;
        if (_statisticsLoaded) return;
        _statisticsLoaded = true;
        try
        {
            await DataGateway.Instance.ConnectIfNeededAsync().ConfigureAwait(false);
            await DataGateway.Instance.ReadAsync("Statistics").ConfigureAwait(false);
        }
        catch { /* ignore */ }
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
    private async void SaveUnitEdit()
    {
        // 간단 구현: 선택된 유닛의 상태/메시지를 서버에 업데이트 시도
        try
        {
            if (SelectedUnit is not null)
            {
                var data = new
                {
                    unitName = SelectedUnit.UnitName,
                    message = SelectedUnit.Message,
                    metric = SelectedUnit.Metric,
                    status = SelectedUnit.Status.ToString()
                };
                await DataGateway.Instance.UpdateAsync("Units", data);
            }
        }
        catch
        {
            // 실패는 일단 무시하고 UI 닫기
        }
        finally
        {
            IsEditDialogOpen = false;
        }
    }
}
