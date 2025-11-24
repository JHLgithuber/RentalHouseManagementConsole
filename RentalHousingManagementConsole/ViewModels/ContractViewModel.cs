using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using RentalHousingManagementConsole.Models;
using RentalHousingManagementConsole.Services;

namespace RentalHousingManagementConsole.ViewModels;

/// <summary>
/// 계약 관리 ViewModel
/// </summary>
public partial class ContractViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ContractData> _contracts = new();

    [ObservableProperty]
    private ContractData? _selectedContract;

    [ObservableProperty]
    private ContractData? _newContract;

    [ObservableProperty]
    private bool _isDetailDialogOpen;

    [ObservableProperty]
    private bool _isEditDialogOpen;

    [ObservableProperty]
    private bool _isCreateDialogOpen;

    private bool _loaded;

    public async Task LoadContractsAsync()
    {
        if (_loaded) return;
        _loaded = true;

        try
        {
            var json = await DataGateway.Instance.ReadAsync("Contract_data").ConfigureAwait(false);
            if (json is JsonElement el && el.ValueKind == JsonValueKind.Array)
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => Contracts.Clear());

                foreach (var item in el.EnumerateArray())
                {
                    var contract = new ContractData
                    {
                        ContractId = item.TryGetProperty("ContractId", out var p1) ? p1.GetString() ?? string.Empty : string.Empty,
                        UnitId = item.TryGetProperty("UnitId", out var p2) ? p2.GetString() : null,
                        TenantName = item.TryGetProperty("TenantName", out var p3) ? p3.GetString() : null,
                        PersonalId = item.TryGetProperty("PersonalId", out var p4) ? p4.GetString() : null,
                        PhoneNumber = item.TryGetProperty("PhoneNumber", out var p5) ? p5.GetString() : null,
                        ContractStartDate = item.TryGetProperty("ContractStartDate", out var p6) && DateTime.TryParse(p6.GetString(), out var d1) ? d1 : null,
                        MoveInDate = item.TryGetProperty("MoveInDate", out var p7) && DateTime.TryParse(p7.GetString(), out var d2) ? d2 : null,
                        ContractEndDate = item.TryGetProperty("ContractEndDate", out var p8) && DateTime.TryParse(p8.GetString(), out var d3) ? d3 : null,
                        ContractRent = item.TryGetProperty("ContractRent", out var p9) && p9.ValueKind == JsonValueKind.Number ? (int?)p9.GetInt32() : null,
                        ContractDeposit = item.TryGetProperty("ContractDeposit", out var p10) && p10.ValueKind == JsonValueKind.Number ? (int?)p10.GetInt32() : null
                    };

                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => Contracts.Add(contract));
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadContractsAsync 실패: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowDetail(ContractData contract)
    {
        SelectedContract = contract;
        IsDetailDialogOpen = true;
    }

    [RelayCommand]
    private void ShowEdit(ContractData contract)
    {
        SelectedContract = contract;
        IsEditDialogOpen = true;
    }

    [RelayCommand]
    private void ShowCreate()
    {
        NewContract = new ContractData { ContractId = Guid.NewGuid().ToString() };
        IsCreateDialogOpen = true;
    }

    [RelayCommand]
    private void CloseDetailDialog() => IsDetailDialogOpen = false;

    [RelayCommand]
    private void CloseEditDialog() => IsEditDialogOpen = false;

    [RelayCommand]
    private void CloseCreateDialog() => IsCreateDialogOpen = false;

    [RelayCommand]
    private async Task SaveEditAsync()
    {
        try
        {
            if (SelectedContract is not null)
            {
                var setClause = $"UnitId = '{SelectedContract.UnitId}', " +
                               $"TenantName = '{SelectedContract.TenantName?.Replace("'", "''")}', " +
                               $"PhoneNumber = '{SelectedContract.PhoneNumber}', " +
                               $"ContractRent = {SelectedContract.ContractRent ?? 0}, " +
                               $"ContractDeposit = {SelectedContract.ContractDeposit ?? 0}";

                var whereClause = $"ContractId = '{SelectedContract.ContractId}'";

                await DataGateway.Instance.UpdateAsync("Contract_data", setClause, whereClause).ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SaveEditAsync 실패: {ex.Message}");
        }
        finally
        {
            IsEditDialogOpen = false;
        }
    }

    [RelayCommand]
    private async Task SaveCreateAsync()
    {
        try
        {
            if (NewContract is not null && !string.IsNullOrWhiteSpace(NewContract.UnitId))
            {
                var properties = "(ContractId, UnitId, TenantName, PhoneNumber, ContractRent, ContractDeposit)";
                var values = $"('{NewContract.ContractId}', '{NewContract.UnitId}', '{NewContract.TenantName?.Replace("'", "''")}', " +
                            $"'{NewContract.PhoneNumber}', {NewContract.ContractRent ?? 0}, {NewContract.ContractDeposit ?? 0})";

                await DataGateway.Instance.CreateAsync("Contract_data", values, properties).ConfigureAwait(false);

                _loaded = false;
                await LoadContractsAsync().ConfigureAwait(false);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"SaveCreateAsync 실패: {ex.Message}");
        }
        finally
        {
            IsCreateDialogOpen = false;
        }
    }

    [RelayCommand]
    private async Task DeleteAsync(ContractData contract)
    {
        try
        {
            var whereClause = $"ContractId = '{contract.ContractId}'";
            await DataGateway.Instance.DeleteAsync("Contract_data", whereClause).ConfigureAwait(false);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => Contracts.Remove(contract));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteAsync 실패: {ex.Message}");
        }
    }
}
