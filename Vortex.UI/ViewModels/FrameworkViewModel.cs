using System.ComponentModel;
using System.Runtime.CompilerServices;

// ViewModels for application UI binding
namespace Vortex.UI.ViewModels
{
    // Base viewmodel for framework extensibility
    public abstract class FrameworkViewModel : INotifyPropertyChanged
    {
        // Loading state indicator flag
        private bool _isLoading;
        // Viewmodel identifier name
        private string _name;

        // Gets or sets loading state
        public bool IsLoading
        {
            get => _isLoading;
            set
            {
                if (_isLoading != value)
                {
                    _isLoading = value;
                    OnPropertyChanged();
                }
            }
        }

        // Gets or sets viewmodel name
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        // Initializes framework viewmodel with name
        protected FrameworkViewModel(string name)
        {
            Name = name;
            IsLoading = false;
        }

        // Property change notification event
        public event PropertyChangedEventHandler PropertyChanged;

        // Raises property changed event
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
