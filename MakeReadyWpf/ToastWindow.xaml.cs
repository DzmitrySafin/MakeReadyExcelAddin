using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace MakeReadyWpf
{
    /// <summary>
    /// Interaction logic for ToastWindow.xaml
    /// </summary>
    public partial class ToastWindow : Popup
    {
        public ToastWindow(ToastViewModel viewModel)
        {
            InitializeComponent();

            DataContext = viewModel;
        }

        private CustomPopupPlacement[] GetPopupPlacement(Size popupSize, Size targetSize, Point offset)
        {
            var point = SystemParameters.WorkArea.BottomRight;
            point.Y -= popupSize.Height;
            return new[] { new CustomPopupPlacement(point, PopupPrimaryAxis.Horizontal) };
        }
    }
}
