using System;
using System.Reflection;
using System.Windows.Input;
using MakeReadyWpf.Commands;

namespace MakeReadyWpf
{
    public class AboutViewModel
    {
        public string ProductVersion { get; set; }

        public AboutViewModel()
        {
            var asm = Assembly.GetCallingAssembly();
            ProductVersion = $"MakeReady Excel Addin v.{asm.GetCustomAttribute<AssemblyFileVersionAttribute>().Version}";
        }

        public Action<bool?> CloseAction { get; set; }

        private ICommand _okCommand;
        public ICommand OkCommand => _okCommand ?? (_okCommand = new RelayCommand(CloseDialog));

        private void CloseDialog()
        {
            CloseAction?.Invoke(true);
        }
    }
}
