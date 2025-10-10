using System;
using System.Globalization;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RentalHousingManagementConsole.ViewModels;

public enum UnitStatus
{
    Normal,
    Attention,
    Urgent,
    Vacant,
    Maintenance
}

public partial class UnitTileViewModel : ObservableObject
{
    [ObservableProperty]
    private string _unitName = string.Empty; // e.g., "101동 1203호" or "A-1203"

    [ObservableProperty]
    private string? _message; // e.g., "미납 2개월", "점검 필요"

    [ObservableProperty]
    private double? _metric; // any numeric value to visualize briefly

    [ObservableProperty]
    private UnitStatus _status = UnitStatus.Normal;

    // Derived property to paint background by status. Not observable separately for simplicity.
    public IBrush BackgroundBrush => Status switch
    {
        UnitStatus.Normal => Brushes.SeaGreen,
        UnitStatus.Attention => Brushes.Goldenrod,
        UnitStatus.Urgent => Brushes.IndianRed,
        UnitStatus.Vacant => Brushes.SteelBlue,
        UnitStatus.Maintenance => Brushes.SlateGray,
        _ => Brushes.Gray
    };

    // Optional foreground contrasting color
    public IBrush ForegroundBrush => Status switch
    {
        UnitStatus.Normal => Brushes.White,
        UnitStatus.Attention => Brushes.Black,
        UnitStatus.Urgent => Brushes.White,
        UnitStatus.Vacant => Brushes.White,
        UnitStatus.Maintenance => Brushes.White,
        _ => Brushes.White
    };

    // Helper short text for metric
    public string MetricText => Metric.HasValue ? Metric.Value.ToString("0.#", CultureInfo.InvariantCulture) : string.Empty;
}