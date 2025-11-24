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
/// 차량 관리 ViewModel
/// </summary>
public partial class VehicleViewModel : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<VehicleData> _vehicles = new();

    [ObservableProperty]
    private VehicleData? _selectedVehicle;

    [ObservableProperty]
    private VehicleData? _newVehicle;

    [ObservableProperty]
    private bool _isDetailDialogOpen;

    [ObservableProperty]
    private bool _isEditDialogOpen;

    [ObservableProperty]
    private bool _isCreateDialogOpen;

    private bool _loaded;

    public async Task LoadVehiclesAsync()
    {
        if (_loaded) return;
        _loaded = true;

        try
        {
            var json = await DataGateway.Instance.ReadAsync("Vehicle_data").ConfigureAwait(false);
            if (json is JsonElement el && el.ValueKind == JsonValueKind.Array)
            {
                await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => Vehicles.Clear());

                foreach (var item in el.EnumerateArray())
                {
                    var vehicle = new VehicleData
                    {
                        VehicleNumber = item.TryGetProperty("VehicleNumber", out var p1) ? p1.GetString() ?? string.Empty : string.Empty,
                        ContractId = item.TryGetProperty("ContractId", out var p2) ? p2.GetString() : null,
                        ResidentId = item.TryGetProperty("ResidentId", out var p3) ? p3.GetString() : null,
                        AdditionalPhoneNumber = item.TryGetProperty("AdditionalPhoneNumber", out var p4) ? p4.GetString() : null,
                        VehicleType = item.TryGetProperty("VehicleType", out var p5) ? p5.GetString() : null,
                        ParkingType = item.TryGetProperty("ParkingType", out var p6) ? p6.GetString() : null
                    };

                    await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => Vehicles.Add(vehicle));
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadVehiclesAsync 실패: {ex.Message}");
        }
    }

    [RelayCommand]
    private void ShowDetail(VehicleData vehicle)
    {
        SelectedVehicle = vehicle;
        IsDetailDialogOpen = true;
    }

    [RelayCommand]
    private void ShowEdit(VehicleData vehicle)
    {
        SelectedVehicle = vehicle;
        IsEditDialogOpen = true;
    }

    [RelayCommand]
    private void ShowCreate()
    {
        NewVehicle = new VehicleData { VehicleNumber = string.Empty };
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
            if (SelectedVehicle is not null)
            {
                var setClause = $"ContractId = '{SelectedVehicle.ContractId}', " +
                               $"ResidentId = '{SelectedVehicle.ResidentId}', " +
                               $"AdditionalPhoneNumber = '{SelectedVehicle.AdditionalPhoneNumber}', " +
                               $"VehicleType = '{SelectedVehicle.VehicleType?.Replace("'", "''")}', " +
                               $"ParkingType = '{SelectedVehicle.ParkingType}'";

                var whereClause = $"VehicleNumber = '{SelectedVehicle.VehicleNumber}'";

                await DataGateway.Instance.UpdateAsync("Vehicle_data", setClause, whereClause).ConfigureAwait(false);
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
            if (NewVehicle is not null && !string.IsNullOrWhiteSpace(NewVehicle.VehicleNumber))
            {
                var properties = "(VehicleNumber, ContractId, ResidentId, AdditionalPhoneNumber, VehicleType, ParkingType)";
                var values = $"('{NewVehicle.VehicleNumber}', '{NewVehicle.ContractId}', '{NewVehicle.ResidentId}', " +
                            $"'{NewVehicle.AdditionalPhoneNumber}', '{NewVehicle.VehicleType?.Replace("'", "''")}', '{NewVehicle.ParkingType}')";

                await DataGateway.Instance.CreateAsync("Vehicle_data", values, properties).ConfigureAwait(false);

                _loaded = false;
                await LoadVehiclesAsync().ConfigureAwait(false);
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
    private async Task DeleteAsync(VehicleData vehicle)
    {
        try
        {
            var whereClause = $"VehicleNumber = '{vehicle.VehicleNumber}'";
            await DataGateway.Instance.DeleteAsync("Vehicle_data", whereClause).ConfigureAwait(false);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => Vehicles.Remove(vehicle));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"DeleteAsync 실패: {ex.Message}");
        }
    }
}
