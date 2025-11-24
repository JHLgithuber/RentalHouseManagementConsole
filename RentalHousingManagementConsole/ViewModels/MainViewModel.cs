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

    [ObservableProperty]
    private bool _isCreateDialogOpen;

    [ObservableProperty]
    private UnitTileViewModel? _newUnit;

    [RelayCommand]
    private void ToggleFullScreen()
    {
        IsFullScreen = !IsFullScreen;
    }

    // Flat list remains for potential usages
    public ObservableCollection<UnitTileViewModel> Units { get; } = new();

    // Grouped view by building (e.g., "101동")
    public ObservableCollection<UnitGroupViewModel> Groups { get; } = new();

    // ViewModels for other entities
    public BillViewModel BillViewModel { get; } = new();
    public ContractViewModel ContractViewModel { get; } = new();
    public ResidentViewModel ResidentViewModel { get; } = new();
    public VehicleViewModel VehicleViewModel { get; } = new();

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
            // 백엔드 엔터티: Houseinfo_data
            var json = await DataGateway.Instance.ReadAsync("Houseinfo_data").ConfigureAwait(false);
            if (json is JsonElement el)
            {
                if (el.ValueKind == JsonValueKind.Array)
                {
                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        Units.Clear();
                        Groups.Clear();
                    });

                    var list = el.EnumerateArray().ToList();
                    if (list.Count == 0)
                    {
                        return;
                    }

                    foreach (var item in list)
                    {
                        // Houseinfo_data 필드 매핑
                        var unitId = item.TryGetProperty("UnitId", out var p1) ? p1.GetString() : null;
                        var location = item.TryGetProperty("Location", out var p2) ? p2.GetString() : null;
                        var roomNumber = item.TryGetProperty("RoomNumber", out var p3) && p3.ValueKind == JsonValueKind.Number ? (int?)p3.GetInt32() : null;
                        var standardRent = item.TryGetProperty("StandardRent", out var p4) && p4.ValueKind == JsonValueKind.Number ? (int?)p4.GetInt32() : null;
                        var standardDeposit = item.TryGetProperty("StandardDeposit", out var p5) && p5.ValueKind == JsonValueKind.Number ? (int?)p5.GetInt32() : null;
                        var listingStatus = item.TryGetProperty("ListingStatus", out var p6) && p6.ValueKind == JsonValueKind.True;
                        var remarks = item.TryGetProperty("Remarks", out var p7) ? p7.GetString() : null;

                        if (string.IsNullOrWhiteSpace(unitId))
                            continue;

                        // UnitId를 표시용 이름으로 사용 (예: "101동 101호")
                        var unitName = unitId!;
                        
                        // 상태 결정 로직 (매물여부 기반)
                        var status = listingStatus ? UnitStatus.Vacant : UnitStatus.Normal;
                        
                        var vm = new UnitTileViewModel
                        {
                            UnitId = unitId!,
                            UnitName = unitName,
                            Message = remarks ?? (listingStatus ? "매물" : "입주중"),
                            Metric = standardRent,
                            Status = status,
                            MonthlyRent = standardRent ?? 0,
                            Deposit = standardDeposit ?? 0,
                            Notes = remarks ?? string.Empty
                        };

                        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                        {
                            Units.Add(vm);
                            // 그룹명 추출 (예: "101동 101호" -> "101동")
                            var idx = unitName.IndexOf(' ');
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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadUnitsAsync 실패: {ex.Message}");
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
        if (_rentLoaded) return;
        _rentLoaded = true;
        try
        {
            await DataGateway.Instance.ConnectIfNeededAsync().ConfigureAwait(false);
            await BillViewModel.LoadBillsAsync().ConfigureAwait(false);
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
            await BillViewModel.LoadBillsAsync().ConfigureAwait(false);
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
    private void ShowCreateUnit()
    {
        NewUnit = new UnitTileViewModel
        {
            UnitId = string.Empty,
            UnitName = string.Empty,
            Status = UnitStatus.Vacant,
            MonthlyRent = 0,
            Deposit = 0,
            Notes = string.Empty
        };
        IsCreateDialogOpen = true;
    }

    [RelayCommand]
    private void CloseCreateDialog()
    {
        IsCreateDialogOpen = false;
    }

    [RelayCommand]
    private async Task SaveUnitEdit()
    {
        try
        {
            if (SelectedUnit is not null)
            {
                // 백엔드 UPDATE 형식: data에 "SET 절", where에 조건
                var setClause = $"StandardRent = {SelectedUnit.MonthlyRent}, " +
                               $"StandardDeposit = {SelectedUnit.Deposit}, " +
                               $"Remarks = '{SelectedUnit.Notes?.Replace("'", "''")}', " +
                               $"ListingStatus = {(SelectedUnit.Status == UnitStatus.Vacant ? "1" : "0")}";
                
                var whereClause = $"UnitId = '{SelectedUnit.UnitId}'";
                
                await DataGateway.Instance.UpdateAsync(
                    entity: "Houseinfo_data",
                    data: setClause,
                    where: whereClause
                ).ConfigureAwait(false);
                
                // 성공 시 로컬 데이터도 업데이트
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
                {
                    SelectedUnit.Message = SelectedUnit.Status == UnitStatus.Vacant ? "매물" : "입주중";
                });
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SaveUnitEdit 실패: {ex.Message}");
        }
        finally
        {
            IsEditDialogOpen = false;
        }
    }

    [RelayCommand]
    private async Task SaveCreateUnit()
    {
        try
        {
            if (NewUnit is not null && !string.IsNullOrWhiteSpace(NewUnit.UnitId))
            {
                // 백엔드 CREATE 형식: property에 컬럼명, data에 VALUES
                var properties = "(UnitId, StandardRent, StandardDeposit, Remarks, ListingStatus)";
                var values = $"('{NewUnit.UnitId}', {NewUnit.MonthlyRent}, {NewUnit.Deposit}, " +
                            $"'{NewUnit.Notes?.Replace("'", "''")}', {(NewUnit.Status == UnitStatus.Vacant ? "1" : "0")})";
                
                await DataGateway.Instance.CreateAsync(
                    entity: "Houseinfo_data",
                    data: values,
                    option: properties
                ).ConfigureAwait(false);
                
                // 성공 시 리스트 새로고침
                _unitsLoaded = false;
                await LoadUnitsAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SaveCreateUnit 실패: {ex.Message}");
        }
        finally
        {
            IsCreateDialogOpen = false;
        }
    }
}
