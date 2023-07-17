using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using MakeReadyWpf.Commands;

namespace MakeReadyWpf
{
    public class LoginViewModel
    {
        public string Email { get; set; }
        public string Password { get; set; }

        public LoginViewModel(string email = "")
        {
            Email = email;
        }

        public Action<bool?> CloseAction { get; set; }

        private ICommand _okCommand;
        public ICommand OkCommand => _okCommand ?? (_okCommand = new RelayCommand(CloseDialog, () => !string.IsNullOrEmpty(Email) && !string.IsNullOrEmpty(Password)));

        private void CloseDialog()
        {
            CloseAction?.Invoke(true);
        }
    }
}
