using System;
using System.Windows.Input;

namespace TorpedoFrontEnd
{
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> execute;
        private readonly Predicate<T> canExecute;

        public RelayCommand(Action<T> executeAction, Predicate<T> canExecutePredicate = null)
        {
            execute = executeAction;
            canExecute = canExecutePredicate;
        }

        public bool CanExecute(object parameter) =>
            canExecute == null || canExecute((T)parameter);

        public void Execute(object parameter) =>
            execute((T)parameter);

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
}