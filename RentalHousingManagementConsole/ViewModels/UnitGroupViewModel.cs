using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RentalHousingManagementConsole.ViewModels;

public partial class UnitGroupViewModel : ObservableObject
{
    [ObservableProperty]
    private string _groupName = string.Empty; // e.g., "101동"

    public ObservableCollection<UnitTileViewModel> Units { get; } = new();
}
