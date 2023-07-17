using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace MakeReadyWpf
{
    /// <summary>
    /// Interaction logic for LoginWindow.xaml
    /// </summary>
    public partial class LoginWindow : Window
    {
        public LoginWindow(LoginViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            viewModel.CloseAction = delegate (bool? ok) { if (System.Windows.Interop.ComponentDispatcher.IsThreadModal) DialogResult = ok; Close(); };
        }

        private void TextboxPassword_PasswordChanged(object sender, RoutedEventArgs e)
        {
            var tb = sender as PasswordBox;
            (DataContext as LoginViewModel).Password = tb.Password;
            tb.Tag = !string.IsNullOrEmpty(tb.Password);
        }
    }
}
