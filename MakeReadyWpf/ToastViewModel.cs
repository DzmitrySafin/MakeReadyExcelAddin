using MakeReadyGeneral.Models;
using MakeReadyWpf.Commands;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Media;

namespace MakeReadyWpf
{
    public class ToastViewModel : INotifyPropertyChanged
    {
        #region INotifyPropertyChanged

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        public ToastViewModel()
        {
            _timer = new Timer(OnTimeout, this, Timeout.Infinite, Timeout.Infinite);
        }

        #region Timer

        private bool _closingWindow = false;
        private int _timeout = Timeout.Infinite;
        private readonly Timer _timer;

        private static void OnTimeout(object state)
        {
            (state as ToastViewModel).IsShown = false;
        }

        private void SetTimer(int timeout)
        {
            if (_closingWindow)
            {
                _timeout = timeout;
                _timer.Change(timeout, Timeout.Infinite);
            }
            else if (_timeout != Timeout.Infinite)
            {
                _timeout = Timeout.Infinite;
                _timer.Change(Timeout.Infinite, Timeout.Infinite);
            }
        }

        #endregion

        #region Properties

        private bool _isShown = false;
        public bool IsShown
        {
            get { return _isShown; }
            set
            {
                _isShown = value;
                OnPropertyChanged();
            }
        }

        private string _header;
        public string Header
        {
            get { return _header; }
            set
            {
                _header = value;
                OnPropertyChanged();
            }
        }

        private string _message;
        public string Message
        {
            get { return _message; }
            set
            {
                _message = value;
                OnPropertyChanged();
            }
        }

        public string CurrentState => _closingWindow ? (_isError ? "Failure!" : "Done!") : IsIndeterminate ? "" : $"{_currentProgress}%";
        private int _currentProgress = 100;
        public int CurrentProgress
        {
            get { return _currentProgress; }
            set
            {
                _currentProgress = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CurrentState));
            }
        }

        private bool _isProgress = false;
        public bool IsProgress
        {
            get { return _isProgress; }
            set
            {
                _isProgress = value;
                OnPropertyChanged();
            }
        }

        private bool _isIndeterminate = false;
        public bool IsIndeterminate
        {
            get { return _isIndeterminate; }
            set
            {
                _isIndeterminate = value;
                OnPropertyChanged();
            }
        }

        private bool _isError = false;
        public bool IsError
        {
            get { return _isError; }
            set
            {
                _isError = value;
                OnPropertyChanged();
            }
        }

        #endregion

        public void ShowToast(string header, string message, bool isProgress, bool isIndeterminate, bool isError = false)
        {
            if (!string.IsNullOrEmpty(header)) Header = header;
            Message = message;
            IsProgress = isProgress;
            IsIndeterminate = isIndeterminate;
            IsError = isError;
            IsShown = true;

            _closingWindow = !isProgress;
            CurrentProgress = isProgress ? 0 : 100;

            SetTimer(4000);
        }

        public void SetError(string header, string message)
        {
            if (!string.IsNullOrEmpty(header)) Header = header;
            Message = message;
            IsError = true;
        }

        public void SetPercentage(string message, int total, int progress)
        {
            Message = message;
            CurrentProgress = progress * 100 / total;
        }

        public void CompleteProgress(string message, bool isError = false)
        {
            Message = message;
            IsError = isError;
            CurrentProgress = 100;
            IsIndeterminate = false;

            _closingWindow = true;
            SetTimer(4000);
        }

        public void HideToast()
        {
            _closingWindow = true;
            SetTimer(500);
        }

        #region Commands

        public ICommand CloseCommand
        {
            get
            {
                return new RelayCommand(() => IsShown = false);
            }
        }

        public ICommand MouseEnterCommand
        {
            get
            {
                return new RelayCommand(() => SetTimer(Timeout.Infinite));
            }
        }

        public ICommand MouseLeaveCommand
        {
            get
            {
                return new RelayCommand(() => SetTimer(4000));
            }
        }

        #endregion
    }
}
