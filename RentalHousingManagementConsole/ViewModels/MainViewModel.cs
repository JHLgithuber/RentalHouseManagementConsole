using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;

namespace RentalHousingManagementConsole.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    [ObservableProperty]
    private bool _isPaneOpen;

    public ObservableCollection<UnitTileViewModel> Units { get; } = new();

    public MainViewModel()
    {
        // Sample data for demonstration
        var sample = new[]
        {
            new UnitTileViewModel { UnitName = "101동 101호", Message = "정상", Metric = 0, Status = UnitStatus.Normal },
            new UnitTileViewModel { UnitName = "101동 102호", Message = "미납 1개월", Metric = 1, Status = UnitStatus.Attention },
            new UnitTileViewModel { UnitName = "101동 103호", Message = "미납 3개월", Metric = 3, Status = UnitStatus.Urgent },
            new UnitTileViewModel { UnitName = "101동 104호", Message = "공실", Metric = null, Status = UnitStatus.Vacant },
            new UnitTileViewModel { UnitName = "101동 105호", Message = "수리 중", Metric = null, Status = UnitStatus.Maintenance },
        };

        for (int i = 0; i < 20; i++)
        {
            var item = sample[i % sample.Length];
            Units.Add(new UnitTileViewModel
            {
                UnitName = item.UnitName.Replace("101호", ($"{100 + i % 50}호")),
                Message = item.Message,
                Metric = item.Metric.HasValue ? (double?)(item.Metric.Value + (i % 2)) : null,
                Status = item.Status
            });
        }
    }
}
