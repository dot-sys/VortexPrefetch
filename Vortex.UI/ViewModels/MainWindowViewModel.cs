using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using System.Windows.Input;
using Vortex.UI.Views;
using Prefetch.Models;

// ViewModels for application UI binding
namespace Vortex.UI.ViewModels
{
    // Main window navigation and lifecycle manager
    public partial class MainWindowViewModel : INotifyPropertyChanged
    {
        // Navigation frame for page routing
        private Frame _mainFrame;
        // Prefetch viewmodel instance
        private PrefetchViewModel _prefetchViewModel;
        // Registered framework viewmodel collection
        private readonly List<FrameworkViewModel> _frameworkViewModels;
        // Current active view identifier
        private string _currentView = "Welcome";

        // Initializes viewmodel and command bindings
        public MainWindowViewModel()
        {
            _frameworkViewModels = new List<FrameworkViewModel>();
        }

        // Gets prefetch viewmodel reference
        public PrefetchViewModel PrefetchViewModel
        {
            get
            {
                if (_prefetchViewModel == null)
                {
                    _prefetchViewModel = new PrefetchViewModel();
                }
                return _prefetchViewModel;
            }
        }

        // Gets readonly framework viewmodel list
        public IReadOnlyList<FrameworkViewModel> FrameworkViewModels => _frameworkViewModels.AsReadOnly();

        // Adds framework viewmodel to collection
        public void RegisterFrameworkViewModel(FrameworkViewModel viewModel)
        {
            if (viewModel != null && !_frameworkViewModels.Contains(viewModel))
            {
                _frameworkViewModels.Add(viewModel);
            }
        }

        // Finds framework viewmodel by name
        public FrameworkViewModel GetFrameworkViewModel(string name)
        {
            return _frameworkViewModels.FirstOrDefault(vm => vm.Name == name);
        }

        // Removes all registered framework viewmodels
        public void ClearFrameworkViewModels()
        {
            _frameworkViewModels.Clear();
        }

        // Assigns navigation frame reference
        public void SetFrame(Frame frame)
        {
            _mainFrame = frame;
            NavigateToWelcome();
        }

        // Navigates to welcome page
        public void NavigateToWelcome()
        {
            if (_mainFrame != null)
            {
                var welcomeView = new WelcomeView { DataContext = this };
                _mainFrame.Navigate(welcomeView);
                _currentView = "Welcome";
            }
        }

        // Navigates to prefetch analyzer
        public void NavigateToPrefetchAnalyzer()
        {
            if (_mainFrame != null)
            {
                var prefetchView = new PrefetchView { DataContext = _prefetchViewModel };
                _mainFrame.Navigate(prefetchView);
                _currentView = "Prefetch";
            }
        }

        // Initializes all framework trace operations
        public virtual void StartAllTraces()
        {
        }

        // Clears data and reloads welcome view
        public void RefreshCurrentView()
        {
            _prefetchViewModel = null;
            ClearFrameworkViewModels();

            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            NavigateToWelcome();
        }

        // Reloads current active view
        public void ReloadCurrentView()
        {
            switch (_currentView)
            {
                case "Welcome":
                    NavigateToWelcome();
                    break;
                case "Prefetch":
                    NavigateToPrefetchAnalyzer();
                    break;
                default:
                    NavigateToWelcome();
                    break;
            }
        }

        // Property change notification event
        public event PropertyChangedEventHandler PropertyChanged;

        // Raises property changed event
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Simple ICommand implementation for actions
    public class RelayCommand : ICommand
    {
        // Action to execute
        private readonly Action _execute;
        // Predicate for execution availability
        private readonly Func<bool> _canExecute;

        // Initializes command with action and predicate
        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        // Execution state change notification event
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        // Determines if command can execute
        public bool CanExecute(object parameter) => _canExecute?.Invoke() ?? true;
        // Executes command action
        public void Execute(object parameter) => _execute();
    }
}