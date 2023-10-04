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
    /// Interaction logic for ConfigurationWindow.xaml
    /// </summary>
    public partial class ConfigurationWindow : Window
    {
        public ConfigurationWindow(ConfigurationViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
            viewModel.CloseAction = delegate (bool? ok) { if (System.Windows.Interop.ComponentDispatcher.IsThreadModal) DialogResult = ok; Close(); };
        }

        private void ComboBox_GotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            comboBox.IsDropDownOpen = true;
        }

        private void ComboBox_DropDownClosed(object sender, EventArgs e)
        {
            ComboBox comboBox = sender as ComboBox;
            comboBox.Text = string.Empty;
            //comboBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.First));
            comboBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Up));
        }
    }
}
