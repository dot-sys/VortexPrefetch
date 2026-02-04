using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Collections;
using Vortex.UI.Views;
using System.Windows.Data;

namespace Vortex.UI.Helpers
{
    public static class PopupHelper
    {
        public static void ShowTextPopup(string title, string content, Window owner = null)
        {
            var popup = new InfoPopupWindow(title, content)
            {
                Owner = owner ?? Application.Current.MainWindow
            };
            popup.ShowDialog();
        }

        public static void ShowDataGridPopup(string title, IEnumerable data, Window owner = null)
        {
            var dataGrid = new DataGrid
            {
                ItemsSource = data,
                AutoGenerateColumns = false,
                IsReadOnly = true,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                RowBackground = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                AlternatingRowBackground = new SolidColorBrush(Color.FromRgb(10, 10, 10)),
                Foreground = Brushes.White,
                Background = new SolidColorBrush(Color.FromRgb(0, 0, 0)),
                BorderBrush = Brushes.White,
                BorderThickness = new Thickness(0),
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                SelectionMode = DataGridSelectionMode.Single,
                SelectionUnit = DataGridSelectionUnit.FullRow,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch
            };

            try
            {
                if (Application.Current.FindResource("VortexDataGridStyle") is Style style)
                {
                    dataGrid.Style = style;
                }
            }
            catch
            {
            }

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Path",
                Binding = new System.Windows.Data.Binding("Path"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Modified",
                Binding = new System.Windows.Data.Binding("Modified"),
                MaxWidth = 75,
                Width = new DataGridLength(1, DataGridLengthUnitType.Auto)
            });

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Signed",
                Binding = new System.Windows.Data.Binding("Signed"),
                MaxWidth = 100,
                Width = new DataGridLength(1, DataGridLengthUnitType.Auto)
            });

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Source",
                Binding = new System.Windows.Data.Binding("Source"),
                MaxWidth = 75,
                Width = new DataGridLength(1, DataGridLengthUnitType.Auto)
            });

            var popup = new InfoPopupWindow(title, dataGrid)
            {
                Owner = owner ?? Application.Current.MainWindow,
                SizeToContent = SizeToContent.WidthAndHeight
            };
            popup.ShowDialog();
        }

        public static void ShowListPopup(string title, IEnumerable data, Window owner = null)
        {
            var dataGrid = new DataGrid
            {
                ItemsSource = data,
                AutoGenerateColumns = false,
                IsReadOnly = true,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                CanUserAddRows = false,
                CanUserDeleteRows = false,
                SelectionMode = DataGridSelectionMode.Single,
                SelectionUnit = DataGridSelectionUnit.FullRow,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                VerticalAlignment = VerticalAlignment.Stretch,
                GridLinesVisibility = DataGridGridLinesVisibility.Horizontal,
                Width = 650,
                MinWidth = 500,
                Margin = new Thickness(4)
            };

            try
            {
                if (Application.Current.FindResource("VortexDataGridStyle") is Style style)
                {
                    dataGrid.Style = style;
                }
            }
            catch
            {
            }

            var rowStyle = new Style(typeof(DataGridRow));
            var negativeTrigger = new DataTrigger
            {
                Binding = new Binding("IsPositive"),
                Value = false
            };
            negativeTrigger.Setters.Add(new Setter(Control.ForegroundProperty, Brushes.Red));
            rowStyle.Triggers.Add(negativeTrigger);
            dataGrid.RowStyle = rowStyle;

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Check",
                Binding = new Binding("Name"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Auto),
                MinWidth = 200
            });

            dataGrid.Columns.Add(new DataGridTextColumn
            {
                Header = "Result",
                Binding = new Binding("Message"),
                Width = new DataGridLength(1, DataGridLengthUnitType.Star)
            });

            var popup = new InfoPopupWindow(title, dataGrid)
            {
                Owner = owner ?? Application.Current.MainWindow,
                SizeToContent = SizeToContent.WidthAndHeight
            };

            popup.Show();
        }
    }
}
