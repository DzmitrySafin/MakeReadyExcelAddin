using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MakeReadyWpf.Commands
{
    internal class RelayCommand : ICommand
    {
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute.Invoke();
        }

        public void Execute(object parameter)
        {
            _command.Invoke();
        }

        private readonly Func<bool> _canExecute;

        private readonly Action _command;

        public RelayCommand(Action command, Func<bool> canExecute = null)
        {
            _command = command;
            _canExecute = canExecute;
        }
    }

    public class RelayCommand<T> : ICommand
    {
        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute.Invoke((T)parameter);
        }

        public void Execute(object parameter)
        {
            _command.Invoke((T)parameter);
        }

        private readonly Predicate<T> _canExecute;

        private readonly Action<T> _command;

        public RelayCommand(Action<T> command, Predicate<T> canExecute = null)
        {
            _command = command;
            _canExecute = canExecute;
        }
    }
}
