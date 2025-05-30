using System;
using System.Windows.Input;

namespace LANChat.UI.Commands
{
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Action execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke() ?? true;
        }

        public void Execute(object? parameter)
        {
            _execute();
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }

    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Predicate<T>? _canExecute;

        public RelayCommand(Action<T> execute, Predicate<T>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object? parameter)
        {
            return parameter is T value && (_canExecute?.Invoke(value) ?? true);
        }

        public void Execute(object? parameter)
        {
            if (parameter is T value)
            {
                _execute(value);
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }
    }
}