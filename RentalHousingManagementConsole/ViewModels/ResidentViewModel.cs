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
/// 주민 관리 ViewModel
/// </summary>
public partial class ResidentViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<ResidentData> _residents = new();

    [ObservableProperty]
    private ResidentData? _selectedResident;

    [ObservableProperty]
    private ResidentData? _newResident;

    [ObservableProperty]
    private bool _isDetailDialogOpen;

    [ObservableProperty]
    private bool _isEditDialogOpen;

    [ObservableProperty]
    private bool _isCreateDialogOpen;

    private bool _loaded;

    public async Task LoadResidentsAsync()
    {
        if (_loaded) return;
        _loaded = true;

        try
        {
            var json = await DataGateway.Instance.ReadAsync("Resident_data").ConfigureAwait(false);
            if (json is JsonElement el && el.ValueKind == JsonValueKind.Array)
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => Residents.Clear());

                foreach (var item in el.EnumerateArray())
                {
                    var resident = new ResidentData
                    {
                        ResidentId = item.TryGetProperty("ResidentId", out var p1) ? p1.GetString() ?? string.Empty : string.Empty,
                        ContractId = item.TryGetProperty("ContractId", out var p2) ? p2.GetString() : null,
                        Name = item.TryGetProperty("Name", out var p3) ? p3.GetString() : null,
                        FamilyRelationship = item.TryGetProperty("FamilyRelationship", out var p4) ? p4.GetString() : null,
                        PhoneNumber = item.TryGetProperty("PhoneNumber", out var p5) ? p5.GetString() : null,
                        Language = item.TryGetProperty("Language", out var p6) ? p6.GetString() : null,
                        ResidencyStatus = item.TryGetProperty("ResidencyStatus", out var p7) && p7.ValueKind == JsonValueKind.True,
                        ApprovalStatus = item.TryGetProperty("ApprovalStatus", out var p8) && p8.ValueKind == JsonValueKind.True
                    };

                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => Residents.Add(resident));
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadResidentsAsync 실패: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowDetail(ResidentData resident)
    {
        SelectedResident = resident;
        IsDetailDialogOpen = true;
    }

    [RelayCommand]
    private void ShowEdit(ResidentData resident)
    {
        SelectedResident = resident;
        IsEditDialogOpen = true;
    }

    [RelayCommand]
    private void ShowCreate()
    {
        NewResident = new ResidentData { ResidentId = Guid.NewGuid().ToString() };
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
            if (SelectedResident is not null)
            {
                var setClause = $"Name = '{SelectedResident.Name?.Replace("'", "''")}', " +
                               $"FamilyRelationship = '{SelectedResident.FamilyRelationship?.Replace("'", "''")}', " +
                               $"PhoneNumber = '{SelectedResident.PhoneNumber}', " +
                               $"Language = '{SelectedResident.Language}', " +
                               $"ResidencyStatus = {(SelectedResident.ResidencyStatus == true ? "1" : "0")}, " +
                               $"ApprovalStatus = {(SelectedResident.ApprovalStatus == true ? "1" : "0")}";

                var whereClause = $"ResidentId = '{SelectedResident.ResidentId}'";

                await DataGateway.Instance.UpdateAsync("Resident_data", setClause, whereClause).ConfigureAwait(false);
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
            if (NewResident is not null && !string.IsNullOrWhiteSpace(NewResident.ContractId))
            {
                var properties = "(ResidentId, ContractId, Name, FamilyRelationship, PhoneNumber, Language, ResidencyStatus, ApprovalStatus)";
                var values = $"('{NewResident.ResidentId}', '{NewResident.ContractId}', '{NewResident.Name?.Replace("'", "''")}', " +
                            $"'{NewResident.FamilyRelationship?.Replace("'", "''")}', '{NewResident.PhoneNumber}', '{NewResident.Language}', " +
                            $"{(NewResident.ResidencyStatus == true ? "1" : "0")}, {(NewResident.ApprovalStatus == true ? "1" : "0")})";

                await DataGateway.Instance.CreateAsync("Resident_data", values, properties).ConfigureAwait(false);

                _loaded = false;
                await LoadResidentsAsync().ConfigureAwait(false);
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
    private async Task DeleteAsync(ResidentData resident)
    {
        try
        {
            var whereClause = $"ResidentId = '{resident.ResidentId}'";
            await DataGateway.Instance.DeleteAsync("Resident_data", whereClause).ConfigureAwait(false);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => Residents.Remove(resident));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteAsync 실패: {ex.Message}");
        }
    }
}
