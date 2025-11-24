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
/// 청구 관리 ViewModel
/// </summary>
public partial class BillViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<BillData> _bills = new();

    [ObservableProperty]
    private BillData? _selectedBill;

    [ObservableProperty]
    private BillData? _newBill;

    [ObservableProperty]
    private bool _isDetailDialogOpen;

    [ObservableProperty]
    private bool _isEditDialogOpen;

    [ObservableProperty]
    private bool _isCreateDialogOpen;

    private bool _loaded;

    public async Task LoadBillsAsync()
    {
        if (_loaded) return;
        _loaded = true;

        try
        {
            var json = await DataGateway.Instance.ReadAsync("Bill_data").ConfigureAwait(false);
            if (json is JsonElement el && el.ValueKind == JsonValueKind.Array)
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => Bills.Clear());

                foreach (var item in el.EnumerateArray())
                {
                    var bill = new BillData
                    {
                        BillId = item.TryGetProperty("BillId", out var p1) ? p1.GetString() ?? string.Empty : string.Empty,
                        ContractId = item.TryGetProperty("ContractId", out var p2) ? p2.GetString() : null,
                        BillDate = item.TryGetProperty("BillDate", out var p3) && DateTime.TryParse(p3.GetString(), out var d1) ? d1 : null,
                        Rent = item.TryGetProperty("Rent", out var p4) && p4.ValueKind == JsonValueKind.Number ? (int?)p4.GetInt32() : null,
                        ManagementFee = item.TryGetProperty("ManagementFee", out var p5) && p5.ValueKind == JsonValueKind.Number ? (int?)p5.GetInt32() : null,
                        UnpaidAmount = item.TryGetProperty("UnpaidAmount", out var p6) && p6.ValueKind == JsonValueKind.Number ? (int?)p6.GetInt32() : null,
                        WaterBill = item.TryGetProperty("WaterBill", out var p7) && p7.ValueKind == JsonValueKind.Number ? (int?)p7.GetInt32() : null,
                        ElectricityBill = item.TryGetProperty("ElectricityBill", out var p8) && p8.ValueKind == JsonValueKind.Number ? (int?)p8.GetInt32() : null,
                        GasBill = item.TryGetProperty("GasBill", out var p9) && p9.ValueKind == JsonValueKind.Number ? (int?)p9.GetInt32() : null,
                        PaidAmount = item.TryGetProperty("PaidAmount", out var p10) && p10.ValueKind == JsonValueKind.Number ? (int?)p10.GetInt32() : null
                    };

                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => Bills.Add(bill));
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadBillsAsync 실패: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowDetail(BillData bill)
    {
        SelectedBill = bill;
        IsDetailDialogOpen = true;
    }

    [RelayCommand]
    private void ShowEdit(BillData bill)
    {
        SelectedBill = bill;
        IsEditDialogOpen = true;
    }

    [RelayCommand]
    private void ShowCreate()
    {
        NewBill = new BillData { BillId = Guid.NewGuid().ToString(), BillDate = DateTime.Now };
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
            if (SelectedBill is not null)
            {
                var setClause = $"Rent = {SelectedBill.Rent ?? 0}, " +
                               $"ManagementFee = {SelectedBill.ManagementFee ?? 0}, " +
                               $"UnpaidAmount = {SelectedBill.UnpaidAmount ?? 0}, " +
                               $"WaterBill = {SelectedBill.WaterBill ?? 0}, " +
                               $"ElectricityBill = {SelectedBill.ElectricityBill ?? 0}, " +
                               $"GasBill = {SelectedBill.GasBill ?? 0}, " +
                               $"PaidAmount = {SelectedBill.PaidAmount ?? 0}";

                var whereClause = $"BillId = '{SelectedBill.BillId}'";

                await DataGateway.Instance.UpdateAsync("Bill_data", setClause, whereClause).ConfigureAwait(false);
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
            if (NewBill is not null && !string.IsNullOrWhiteSpace(NewBill.ContractId))
            {
                var properties = "(BillId, ContractId, BillDate, Rent, ManagementFee, UnpaidAmount, WaterBill, ElectricityBill, GasBill)";
                var values = $"('{NewBill.BillId}', '{NewBill.ContractId}', '{NewBill.BillDate:yyyy-MM-dd}', " +
                            $"{NewBill.Rent ?? 0}, {NewBill.ManagementFee ?? 0}, {NewBill.UnpaidAmount ?? 0}, " +
                            $"{NewBill.WaterBill ?? 0}, {NewBill.ElectricityBill ?? 0}, {NewBill.GasBill ?? 0})";

                await DataGateway.Instance.CreateAsync("Bill_data", values, properties).ConfigureAwait(false);

                _loaded = false;
                await LoadBillsAsync().ConfigureAwait(false);
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
    private async Task DeleteAsync(BillData bill)
    {
        try
        {
            var whereClause = $"BillId = '{bill.BillId}'";
            await DataGateway.Instance.DeleteAsync("Bill_data", whereClause).ConfigureAwait(false);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => Bills.Remove(bill));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteAsync 실패: {ex.Message}");
        }
    }
}
