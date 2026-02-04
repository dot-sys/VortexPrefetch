using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace Vortex.UI.Helpers
{
    public static class DataGridContextMenuHelper
    {
        public static void CopyValue(DataGrid dataGrid)
        {
            if (dataGrid?.CurrentCell == null)
                return;

            try
            {
                var cellContent = dataGrid.CurrentCell.Column?.GetCellContent(dataGrid.CurrentCell.Item);
                if (cellContent is TextBlock textBlock)
                {
                    var item = dataGrid.CurrentCell.Item;
                    var columnIndex = dataGrid.CurrentCell.Column.DisplayIndex;

                    string textToCopy = null;

                    var fullValueProperty = item?.GetType().GetProperty("FullValue");
                    if (fullValueProperty != null && columnIndex == 1)
                    {
                        textToCopy = fullValueProperty.GetValue(item)?.ToString();
                    }

                    if (string.IsNullOrEmpty(textToCopy))
                    {
                        textToCopy = textBlock.Text;
                    }

                    if (!string.IsNullOrEmpty(textToCopy))
                    {
                        Clipboard.SetText(textToCopy);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy value: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static void CopyRow(DataGrid dataGrid)
        {
            if (dataGrid?.SelectedItem == null)
                return;

            try
            {
                var rowData = new System.Text.StringBuilder();
                var item = dataGrid.SelectedItem;

                foreach (var column in dataGrid.Columns)
                {
                    var cellContent = column.GetCellContent(item);
                    if (cellContent is TextBlock textBlock)
                    {
                        if (rowData.Length > 0)
                            rowData.Append("\t");

                        var columnIndex = column.DisplayIndex;
                        var fullValueProperty = item?.GetType().GetProperty("FullValue");

                        if (fullValueProperty != null && columnIndex == 1)
                        {
                            var fullValue = fullValueProperty.GetValue(item)?.ToString();
                            rowData.Append(fullValue ?? textBlock.Text);
                        }
                        else
                        {
                            rowData.Append(textBlock.Text);
                        }
                    }
                }

                if (rowData.Length > 0)
                {
                    Clipboard.SetText(rowData.ToString());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy row: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Opens file path in explorer
        public static void GoToPath(DataGrid dataGrid)
        {
            if (dataGrid?.CurrentCell == null)
                return;

            try
            {
                var cellContent = dataGrid.CurrentCell.Column?.GetCellContent(dataGrid.CurrentCell.Item);
                if (cellContent is TextBlock textBlock)
                {
                    string path = textBlock.Text?.Trim();
                    if (string.IsNullOrEmpty(path))
                        return;

                    string directoryPath = GetDirectoryPath(path);

                    if (!string.IsNullOrEmpty(directoryPath) && Directory.Exists(directoryPath))
                    {
                        OpenFolderInExistingExplorer(directoryPath);
                    }
                    else
                    {
                        MessageBox.Show($"Directory does not exist:\n{directoryPath}", "Path Not Found",
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open path: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Opens folder in existing explorer window
        private static void OpenFolderInExistingExplorer(string folderPath)
        {
            try
            {
                Type shellType = Type.GetTypeFromProgID("Shell.Application");
                dynamic shell = Activator.CreateInstance(shellType);

                try
                {
                    var windows = shell.Windows();
                    bool navigated = false;

                    foreach (dynamic window in windows)
                    {
                        try
                        {
                            string windowName = window.Name;
                            if (windowName != null && windowName.Contains("Explorer"))
                            {
                                window.Navigate(folderPath);

                                int hwnd = window.HWND;
                                SetForegroundWindow(new IntPtr(hwnd));

                                navigated = true;
                                break;
                            }
                        }
                        catch
                        {
                            continue;
                        }
                    }

                    if (!navigated)
                    {
                        Process.Start("explorer.exe", $"/e,\"{folderPath}\"");
                    }
                }
                finally
                {
                    System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);
                }
            }
            catch
            {
                Process.Start("explorer.exe", $"/e,\"{folderPath}\"");
            }
        }

        // Windows API for window focus
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        // Extracts directory from file path
        private static string GetDirectoryPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return null;

            path = path.TrimEnd('\\', ' ');

            if (Path.HasExtension(path))
            {
                return Path.GetDirectoryName(path);
            }
            else
            {
                return path;
            }
        }

        // Checks if column contains path data
        public static bool IsPathColumn(DataGrid dataGrid)
        {
            if (dataGrid?.CurrentCell == null)
                return false;

            var column = dataGrid.CurrentCell.Column;
            if (column == null)
                return false;

            var headerText = column.Header?.ToString() ?? string.Empty;

            if (headerText.IndexOf("path", StringComparison.OrdinalIgnoreCase) >= 0)
                return true;

            if (column is DataGridBoundColumn boundColumn)
            {
                var binding = boundColumn.Binding as Binding;
                var bindingPath = binding?.Path?.Path ?? string.Empty;
                if (bindingPath.IndexOf("path", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;

                var sortPath = boundColumn.SortMemberPath ?? string.Empty;
                if (sortPath.IndexOf("path", StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }

            return false;
        }

        // Gets current cell text value
        public static string GetCellValue(DataGrid dataGrid)
        {
            if (dataGrid?.CurrentCell == null)
                return null;

            try
            {
                var cellContent = dataGrid.CurrentCell.Column?.GetCellContent(dataGrid.CurrentCell.Item);
                if (cellContent is TextBlock textBlock)
                {
                    return textBlock.Text;
                }
            }
            catch
            {
            }

            return null;
        }
    }
}
