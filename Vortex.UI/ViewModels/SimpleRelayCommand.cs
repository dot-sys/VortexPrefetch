using System;
using System.Windows.Input;

// ViewModels for application UI binding
namespace Vortex.UI.ViewModels
{
    // Basic command wrapper for parameterized actions
    public class SimpleRelayCommand : ICommand
    {
        // Action to execute with parameter
        private readonly Action<object> _execute;
        // Initializes command with action
        public SimpleRelayCommand(Action<object> execute)
        {
            _execute = execute;
        }
        // Always returns true for execution
        public bool CanExecute(object parameter) => true;
        // Empty event for execution availability
        public event EventHandler CanExecuteChanged { add { } remove { } }
        // Executes action with parameter
        public void Execute(object parameter) => _execute(parameter);
    }
}