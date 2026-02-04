using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Navigation;
using Microsoft.Win32;
using Vortex.UI.Helpers;
using Vortex.UI.ViewModels;
using Prefetch.Models;

namespace Vortex.UI.Views
{
    public partial class PrefetchView : Page
    {
        private PrefetchViewModel _viewModel;

        private class TamperingCheckResult : INotifyPropertyChanged
        {
            private string _name;
            private string _message;
            private bool _isPositive;

            public string Name
            {
                get => _name;
                set => SetProperty(ref _name, value, nameof(Name));
            }

            public string Message
            {
                get => _message;
                set => SetProperty(ref _message, value, nameof(Message));
            }

            public bool IsPositive
            {
                get => _isPositive;
                set => SetProperty(ref _isPositive, value, nameof(IsPositive));
            }

            public event PropertyChangedEventHandler PropertyChanged;

            public void UpdateFrom(TamperingCheckResult other)
            {
                if (other == null)
                    return;

                Name = other.Name;
                Message = other.Message;
                IsPositive = other.IsPositive;
            }

            private void SetProperty<T>(ref T field, T value, string propertyName)
            {
                if (EqualityComparer<T>.Default.Equals(field, value))
                    return;

                field = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public PrefetchView()
        {
            InitializeComponent();
            DataContextChanged += PrefetchView_DataContextChanged;
            EnsureViewModel();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            WarmUpContextMenu(FilesDataGrid?.ContextMenu);
            WarmUpContextMenu(LeftDataGrid?.ContextMenu);
            WarmUpContextMenu(RightDataGrid?.ContextMenu);
        }

        private void PrefetchView_DataContextChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            EnsureViewModel();
        }

        private void EnsureViewModel()
        {
            if (!(DataContext is PrefetchViewModel viewModel))
            {
                viewModel = new PrefetchViewModel();
                DataContext = viewModel;
            }

            _viewModel = viewModel;
        }

        private void FilesDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilesDataGrid.SelectedItem is PrefetchData selected)
            {
                _viewModel.SelectedPrefetch = selected;
            }
        }

        private void WarmUpContextMenu(ContextMenu menu)
        {
            if (menu == null)
                return;

            menu.ApplyTemplate();

            foreach (var item in menu.Items.OfType<MenuItem>())
            {
                item.ApplyTemplate();
            }
        }

        private void DataGrid_PreviewMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            var dataGrid = sender as DataGrid;
            if (dataGrid == null)
                return;

            var cell = FindParent<DataGridCell>(e.OriginalSource as DependencyObject);
            if (cell?.Column == null)
                return;

            var rowData = cell.DataContext;
            if (rowData != null)
            {
                dataGrid.SelectedItem = rowData;
                dataGrid.CurrentCell = new DataGridCellInfo(rowData, cell.Column);
                dataGrid.Focus();
            }
        }

        private void DataGridContextMenu_Opened(object sender, RoutedEventArgs e)
        {
            var contextMenu = sender as ContextMenu;
            var dataGrid = contextMenu?.PlacementTarget as DataGrid;
            if (contextMenu == null || dataGrid == null)
                return;

            var goToItem = contextMenu.Items.OfType<MenuItem>().FirstOrDefault(i => i.Name == "GoToMenuItem");
            if (goToItem != null)
            {
                goToItem.IsEnabled = DataGridContextMenuHelper.IsPathColumn(dataGrid);
            }
        }

        private void ContextMenu_CopyValue_Click(object sender, RoutedEventArgs e)
        {
            var dataGrid = GetContextMenuDataGrid(sender);
            DataGridContextMenuHelper.CopyValue(dataGrid);
        }

        private void ContextMenu_CopyRow_Click(object sender, RoutedEventArgs e)
        {
            var dataGrid = GetContextMenuDataGrid(sender);
            DataGridContextMenuHelper.CopyRow(dataGrid);
        }

        private void ContextMenu_GoTo_Click(object sender, RoutedEventArgs e)
        {
            var dataGrid = GetContextMenuDataGrid(sender);
            DataGridContextMenuHelper.GoToPath(dataGrid);
        }

        private DataGrid GetContextMenuDataGrid(object sender)
        {
            var menuItem = sender as MenuItem;
            var contextMenu = menuItem?.Parent as ContextMenu;
            return contextMenu?.PlacementTarget as DataGrid;
        }

        private static T FindParent<T>(DependencyObject child) where T : DependencyObject
        {
            while (child != null)
            {
                if (child is T typedChild)
                    return typedChild;

                child = VisualTreeHelper.GetParent(child);
            }

            return null;
        }

        private void VirusTotalLink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            if (!(sender is Hyperlink hyperlink))
                return;

            var url = BuildVirusTotalUrl(hyperlink, e?.Uri);
            if (string.IsNullOrEmpty(url))
                return;

            if (TryShellLaunch(url))
            {
                e.Handled = true;
                return;
            }

            if (TryShellLaunch("explorer.exe", url))
            {
                e.Handled = true;
                return;
            }

            // Last fallback for stubborn environments
            TryShellLaunch("cmd.exe", $"/c start \"\" \"{url}\"");
            e.Handled = true;
        }

        private string BuildVirusTotalUrl(Hyperlink link, Uri uri)
        {
            var original = uri?.OriginalString ?? link?.NavigateUri?.OriginalString;
            if (string.IsNullOrWhiteSpace(original))
                return null;

            original = original.Trim();

            if (Uri.TryCreate(original, UriKind.Absolute, out var absolute))
                return absolute.AbsoluteUri;

            var vt = "https://www.virustotal.com/gui/file/" + original;
            return Uri.TryCreate(vt, UriKind.Absolute, out absolute)
                ? absolute.AbsoluteUri
                : null;
        }

        private bool TryShellLaunch(string fileName, string arguments = null)
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    UseShellExecute = true
                };

                if (!string.IsNullOrWhiteSpace(arguments))
                    startInfo.Arguments = arguments;

                Process.Start(startInfo);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private async void TamperingCheck_Click(object sender, RoutedEventArgs e)
        {
            var results = new ObservableCollection<TamperingCheckResult>
            {
                new TamperingCheckResult { Name = "SysMain Service", Message = "Checking...", IsPositive = true },
                new TamperingCheckResult { Name = "Registry Prefetcher", Message = "Checking...", IsPositive = true },
                new TamperingCheckResult { Name = "Recent Prefetch Deletion", Message = "Checking...", IsPositive = true },
                new TamperingCheckResult { Name = "Prefetch Files Count", Message = "Checking...", IsPositive = true }
            };

            PopupHelper.ShowListPopup("Tampering Check", results);

            await UpdateResultAsync(results[0], CheckSysMainService);
            await UpdateResultAsync(results[1], CheckPrefetcherRegistry);
            await UpdateResultAsync(results[2], CheckRecentPrefetchDeletion);
            await UpdateResultAsync(results[3], CheckPrefetchFilesCount);
        }

        private async Task UpdateResultAsync(TamperingCheckResult target, Func<TamperingCheckResult> checkFunc)
        {
            try
            {
                var result = await Task.Run(checkFunc);
                target.UpdateFrom(result);
            }
            catch (Exception ex)
            {
                target.UpdateFrom(Negative(target.Name, ex.Message));
            }
        }

        private TamperingCheckResult CheckSysMainService()
        {
            const string name = "SysMain Service";

            try
            {
                var startType = GetServiceStartType("SysMain");
                var isRunning = IsServiceRunning("SysMain");

                var isAutomatic = string.Equals(startType, "Automatic", StringComparison.OrdinalIgnoreCase);

                if (isRunning == true && isAutomatic)
                {
                    return Positive(name, "SysMain Service running");
                }

                var stateText = isRunning == null ? "Unknown" : (isRunning.Value ? "Running" : "Stopped");
                var details = string.IsNullOrWhiteSpace(startType)
                    ? $"(Status: {stateText})"
                    : $"(Status: {stateText}, Start: {startType})";

                return Negative(name, $"SysMain Service not running as expected {details}");
            }
            catch (Exception ex)
            {
                return Negative(name, $"SysMain Service not running as expected ({ex.Message})");
            }
        }

        private string GetServiceStartType(string serviceName)
        {
            try
            {
                var path = $"SYSTEM\\CurrentControlSet\\Services\\{serviceName}";
                using (var key = Registry.LocalMachine.OpenSubKey(path))
                {
                    var startValue = key?.GetValue("Start");
                    if (startValue == null)
                        return null;

                    var start = Convert.ToInt32(startValue);
                    switch (start)
                    {
                        case 2:
                            return "Automatic";
                        case 3:
                            return "Manual";
                        case 4:
                            return "Disabled";
                        default:
                            return start.ToString();
                    }
                }
            }
            catch
            {
            }

            return null;
        }

        private bool? IsServiceRunning(string serviceName)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = "sc.exe",
                    Arguments = $"query \"{serviceName}\"",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using (var process = Process.Start(psi))
                {
                    if (process == null)
                        return null;

                    var output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit(2000);

                    var line = output
                        .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                        .FirstOrDefault(l => l.IndexOf("STATE", StringComparison.OrdinalIgnoreCase) >= 0);

                    if (line == null)
                        return null;

                    if (line.IndexOf("RUNNING", StringComparison.OrdinalIgnoreCase) >= 0)
                        return true;

                    return false;
                }
            }
            catch
            {
                return null;
            }
        }

        private TamperingCheckResult CheckPrefetcherRegistry()
        {
            const string name = "Registry Prefetcher";

            try
            {
                using (var key = Registry.LocalMachine.OpenSubKey(@"SYSTEM\\CurrentControlSet\\Control\\Session Manager\\Memory Management\\PrefetchParameters"))
                {
                    var value = key?.GetValue("EnablePrefetcher");

                    if (value is int intValue && intValue == 3)
                    {
                        return Positive(name, "Prefetcher running as expected");
                    }

                    var details = value == null ? "(Value missing)" : $"(Value: {value})";
                    return Negative(name, $"Prefetcher not running as expected {details}");
                }
            }
            catch (Exception ex)
            {
                return Negative(name, $"Prefetcher not running as expected ({ex.Message})");
            }
        }

        private TamperingCheckResult CheckRecentPrefetchDeletion()
        {
            const string name = "Recent Prefetch Deletion";
            const string prefetchDir = @"C:\\Windows\\Prefetch";

            try
            {
                if (!Directory.Exists(prefetchDir))
                {
                    return Negative(name, "Prefetch Files seem to be manipulated (Prefetch directory missing)");
                }

                var files = Directory.GetFiles(prefetchDir, "*.pf");
                if (files.Length == 0)
                {
                    return Negative(name, "Prefetch Files seem to be manipulated (no prefetch files found)");
                }

                var creationTimes = files.Select(File.GetCreationTime).ToList();
                var oldest = creationTimes.Min();
                var newest = creationTimes.Max();
                var lastBoot = GetLastBootTime();

                if (!lastBoot.HasValue)
                {
                    return Negative(name, "Prefetch Files seem to be manipulated (Could not determine last restart time)");
                }

                var oldestOk = (DateTime.Now - oldest).TotalDays > 7;
                var newestOk = newest > lastBoot.Value;

                if (oldestOk && newestOk)
                {
                    return Positive(name, "Prefetch Files seem intact");
                }

                string outcome;
                if (!oldestOk && !newestOk)
                {
                    outcome = $"Oldest Created Date: {oldest:yyyy-MM-dd HH:mm:ss}; Newest Created Date: {newest:yyyy-MM-dd HH:mm:ss}";
                }
                else if (!oldestOk)
                {
                    outcome = $"Oldest Created Date: {oldest:yyyy-MM-dd HH:mm:ss}";
                }
                else
                {
                    outcome = $"Newest Created Date: {newest:yyyy-MM-dd HH:mm:ss}";
                }

                return Negative(name, $"Prefetch Files seem to be manipulated {outcome}");
            }
            catch (Exception ex)
            {
                return Negative(name, $"Prefetch Files seem to be manipulated ({ex.Message})");
            }
        }

        private DateTime? GetLastBootTime()
        {
            try
            {
                var uptime = TimeSpan.FromMilliseconds(GetTickCount64());
                return DateTime.Now - uptime;
            }
            catch
            {
            }

            return null;
        }

        [DllImport("kernel32")]
        private static extern ulong GetTickCount64();

        private TamperingCheckResult CheckPrefetchFilesCount()
        {
            const string name = "Prefetch Files Count";
            const string prefetchDir = @"C:\\Windows\\Prefetch";

            try
            {
                if (!Directory.Exists(prefetchDir))
                {
                    return Negative(name, "Prefetch files depleted: 0 (directory missing)");
                }

                var count = Directory.GetFiles(prefetchDir, "*.pf").Length;

                if (count > 50)
                {
                    return Positive(name, "Prefetch file count normal");
                }

                return Negative(name, $"Prefetch files depleted: {count}");
            }
            catch (Exception ex)
            {
                return Negative(name, $"Prefetch files depleted: {ex.Message}");
            }
        }

        private TamperingCheckResult Positive(string name, string message)
        {
            return new TamperingCheckResult
            {
                Name = name,
                Message = message,
                IsPositive = true
            };
        }

        private TamperingCheckResult Negative(string name, string message)
        {
            return new TamperingCheckResult
            {
                Name = name,
                Message = message,
                IsPositive = false
            };
        }
    }
}