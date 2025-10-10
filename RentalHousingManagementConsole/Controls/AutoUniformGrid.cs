using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;

namespace RentalHousingManagementConsole.Controls
{
    // AutoUniformGrid: calculates Columns from available width and MinItemWidth to fill the row without leftover space.
    // Use in XAML as ItemsPanel for ItemsControl to make tiles expand and fill to the right.
    public class AutoUniformGrid : UniformGrid
    {
        public static readonly StyledProperty<double> MinItemWidthProperty =
            AvaloniaProperty.Register<AutoUniformGrid, double>(nameof(MinItemWidth), 240d);

        public double MinItemWidth
        {
            get => GetValue(MinItemWidthProperty);
            set => SetValue(MinItemWidthProperty, value);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            // compute columns from available width
            if (!double.IsInfinity(constraint.Width) && constraint.Width > 0)
            {
                var min = Math.Max(1, MinItemWidth);
                var cols = Math.Max(1, (int)Math.Floor(constraint.Width / min));
                Columns = cols;
            }
            return base.MeasureOverride(constraint);
        }
    }
}
