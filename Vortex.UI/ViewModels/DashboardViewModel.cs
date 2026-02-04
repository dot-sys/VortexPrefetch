using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

// ViewModels for application UI binding
namespace Vortex.UI.ViewModels
{
    // Dashboard data aggregation and display manager
    public class DashboardViewModel : INotifyPropertyChanged
    {
        // Hardware dashboard items collection
        private ObservableCollection<DashboardItem> _hardwareItems;
        // Software dashboard items collection
        private ObservableCollection<DashboardItem> _softwareItems;
        // Tampering dashboard items collection
        private ObservableCollection<DashboardItem> _tamperingItems;

        // Gets or sets hardware items
        public ObservableCollection<DashboardItem> HardwareItems
        {
            get => _hardwareItems;
            set
            {
                _hardwareItems = value;
                OnPropertyChanged();
            }
        }

        // Gets or sets software items
        public ObservableCollection<DashboardItem> SoftwareItems
        {
            get => _softwareItems;
            set
            {
                _softwareItems = value;
                OnPropertyChanged();
            }
        }

        // Gets or sets tampering items
        public ObservableCollection<DashboardItem> TamperingItems
        {
            get => _tamperingItems;
            set
            {
                _tamperingItems = value;
                OnPropertyChanged();
            }
        }

        // Initializes dashboard item collections
        public DashboardViewModel()
        {
            HardwareItems = new ObservableCollection<DashboardItem>();
            SoftwareItems = new ObservableCollection<DashboardItem>();
            TamperingItems = new ObservableCollection<DashboardItem>();
        }

        // Property change notification event
        public event PropertyChangedEventHandler PropertyChanged;

        // Raises property changed event
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Key-value pair for dashboard display
    public class DashboardItem
    {
        // Property name label
        public string Property { get; set; }
        // Property value text
        public string Value { get; set; }
    }
}
