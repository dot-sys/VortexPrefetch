using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Data;
using Prefetch.Models;

// ViewModels for application UI binding
namespace Vortex.UI.ViewModels
{
    // Manages Prefetch analyzer data
    public class PrefetchViewModel : INotifyPropertyChanged
    {
        public PrefetchViewModel()
        {
            _leftGridView = CollectionViewSource.GetDefaultView(_leftGridData);
            _leftGridView.Filter = FilterLeftGrid;

            _filteredFilesView = CollectionViewSource.GetDefaultView(_filteredFiles);
            _filteredFilesView.Filter = FilterFiles;
        }

        private bool _markDeletedRed;
        public bool MarkDeletedRed
        {
            get => _markDeletedRed;
            set
            {
                if (_markDeletedRed != value)
                {
                    _markDeletedRed = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _markUnsignedGold;
        public bool MarkUnsignedGold
        {
            get => _markUnsignedGold;
            set
            {
                if (_markUnsignedGold != value)
                {
                    _markUnsignedGold = value;
                    OnPropertyChanged();
                }
            }
        }

        private ObservableCollection<PrefetchData> _filteredFiles = new ObservableCollection<PrefetchData>();
        private ICollectionView _filteredFilesView;
        public ObservableCollection<PrefetchData> FilteredFiles
        {
            get => _filteredFiles;
            set
            {
                if (_filteredFiles != value)
                {
                    _filteredFiles = value;
                    _filteredFilesView = CollectionViewSource.GetDefaultView(_filteredFiles);
                    if (_filteredFilesView != null)
                    {
                        _filteredFilesView.Filter = FilterFiles;
                        _filteredFilesView.Refresh();
                    }
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(FilteredFilesView));
                }
            }
        }

        public ICollectionView FilteredFilesView => _filteredFilesView;

        public void SetFilteredFiles(IEnumerable<PrefetchData> items)
        {
            _filteredFiles.Clear();
            if (items != null)
            {
                foreach (var item in items)
                {
                    _filteredFiles.Add(item);
                }
            }

            OnPropertyChanged(nameof(FilteredFiles));
            OnPropertyChanged(nameof(FilteredFilesView));
            _filteredFilesView?.Refresh();
        }

        private ObservableCollection<LeftGridItem> _leftGridData = new ObservableCollection<LeftGridItem>();
        private ICollectionView _leftGridView;
        public ObservableCollection<LeftGridItem> LeftGridData
        {
            get => _leftGridData;
            set
            {
                if (_leftGridData != value)
                {
                    _leftGridData = value;
                    _leftGridView = CollectionViewSource.GetDefaultView(_leftGridData);
                    _leftGridView.Filter = FilterLeftGrid;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(LeftGridView));
                    _leftGridView.Refresh();
                }
            }
        }

        public ICollectionView LeftGridView => _leftGridView;

        private string _leftSearchText = string.Empty;
        public string LeftSearchText
        {
            get => _leftSearchText;
            set
            {
                if (_leftSearchText != value)
                {
                    _leftSearchText = value;
                    OnPropertyChanged();
                    _leftGridView?.Refresh();
                }
            }
        }

        private string _sourcePathSearchText = string.Empty;
        public string SourcePathSearchText
        {
            get => _sourcePathSearchText;
            set
            {
                if (_sourcePathSearchText != value)
                {
                    _sourcePathSearchText = value;
                    OnPropertyChanged();
                    _filteredFilesView?.Refresh();
                }
            }
        }

        private ObservableCollection<RightGridItem> _rightGridData = new ObservableCollection<RightGridItem>();
        public ObservableCollection<RightGridItem> RightGridData
        {
            get => _rightGridData;
            set
            {
                _rightGridData = value;
                OnPropertyChanged();
            }
        }

        private PrefetchData _selectedPrefetch;
        public PrefetchData SelectedPrefetch
        {
            get => _selectedPrefetch;
            set
            {
                _selectedPrefetch = value;
                OnPropertyChanged();
                UpdateBottomGrid();
            }
        }

        private bool _isLoadingOverlayVisible = false;
        public bool IsLoadingOverlayVisible
        {
            get => _isLoadingOverlayVisible;
            set
            {
                _isLoadingOverlayVisible = value;
                OnPropertyChanged();
            }
        }

        private string _loadingMessage = "";
        public string LoadingMessage
        {
            get => _loadingMessage;
            set
            {
                _loadingMessage = value;
                OnPropertyChanged();
            }
        }

        private void UpdateBottomGrid()
        {
            LeftGridData.Clear();
            RightGridData.Clear();
            if (SelectedPrefetch != null)
            {
                int i = 1;
                foreach (var dir in SelectedPrefetch.Directories ?? new List<string>())
                {
                    LeftGridData.Add(new LeftGridItem { Index = i.ToString(), Directory = dir });
                    i++;
                }

                _leftGridView?.Refresh();

                AddRightGridItem("Source Name", SelectedPrefetch.ExecutableName);
                AddRightGridItem("Source Path", SelectedPrefetch.ExecutableFullPath);
                AddRightGridItem("PF Filename", SelectedPrefetch.SourceFilename);
                AddRightGridItem("Status", SelectedPrefetch.Status);
                AddRightGridItem("PFCreatedOn", FormatDateTime(SelectedPrefetch.PFCreatedOn));
                AddRightGridItem("PFAccesedOn", FormatDateTime(SelectedPrefetch.PFAccesedOn));
                AddRightGridItem("PFModifiedOn", FormatDateTime(SelectedPrefetch.PFModifiedOn));
                AddRightGridItem("Source Created On", FormatDateTime(SelectedPrefetch.SourceCreatedOn));
                AddRightGridItem("Source Accessed On", FormatDateTime(SelectedPrefetch.SourceAccessedOn));
                AddRightGridItem("Source Modified On", FormatDateTime(SelectedPrefetch.SourceModifiedOn));
                AddRightGridItem("Signature", SelectedPrefetch.SignatureStatus);
                AddRightGridItem("MD5 Hash", SelectedPrefetch.Md5Hash);
                AddRightGridItem("Run Count", SelectedPrefetch.RunCount.ToString());
                AddRightGridItem("Last Run #1", FormatDateTime(SelectedPrefetch.LastRun));
                AddRightGridItem("Last Run #2", FormatDateTime(SelectedPrefetch.PreviousRun0));
                AddRightGridItem("Last Run #3", FormatDateTime(SelectedPrefetch.PreviousRun1));
                AddRightGridItem("Last Run #4", FormatDateTime(SelectedPrefetch.PreviousRun2));
                AddRightGridItem("Last Run #5", FormatDateTime(SelectedPrefetch.PreviousRun3));
                AddRightGridItem("Last Run #6", FormatDateTime(SelectedPrefetch.PreviousRun4));
                AddRightGridItem("Last Run #7", FormatDateTime(SelectedPrefetch.PreviousRun5));
                AddRightGridItem("Last Run #8", FormatDateTime(SelectedPrefetch.PreviousRun6));

            }
        }

        private void AddRightGridItem(string info, string value)
        {
            RightGridData.Add(new RightGridItem { Info = info, Value = string.IsNullOrEmpty(value) ? "N/A" : value });
        }

        private string FormatDateTime(System.DateTime? dateTime)
        {
            if (!dateTime.HasValue || dateTime.Value == System.DateTime.MinValue)
            {
                return "N/A";
            }

            return dateTime.Value.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private bool FilterLeftGrid(object obj)
        {
            if (obj is LeftGridItem item)
            {
                if (string.IsNullOrWhiteSpace(_leftSearchText))
                {
                    return true;
                }

                return (item.Directory ?? string.Empty).IndexOf(_leftSearchText, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            return false;
        }

        private bool FilterFiles(object obj)
        {
            if (obj is PrefetchData data)
            {
                if (string.IsNullOrWhiteSpace(_sourcePathSearchText))
                {
                    return true;
                }

                var path = data.ExecutableFullPath ?? string.Empty;
                return path.IndexOf(_sourcePathSearchText, StringComparison.OrdinalIgnoreCase) >= 0;
            }

            return false;
        }
    }

    public class LeftGridItem
    {
        public string Index { get; set; }
        public string Directory { get; set; }
    }

    public class RightGridItem
    {
        public string Info { get; set; }
        public string Value { get; set; }
    }
}
