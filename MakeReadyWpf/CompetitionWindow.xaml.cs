using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
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
using System.Windows.Shapes;
using MakeReadyWpf.AttachedProperties;

namespace MakeReadyWpf
{
    /// <summary>
    /// Interaction logic for CompetitionWindow.xaml
    /// </summary>
    public partial class CompetitionWindow : Window
    {
        private GridViewColumnHeader sortColumn = null;
        private SortAdorner sortAdorner = null;

        public CompetitionWindow(CompetitionViewModel viewModel)
        {
            InitializeComponent();

            var dateHeader = (listView.View as GridView).Columns[0].Header as GridViewColumnHeader;
            listView.Items.SortDescriptions.Add(new SortDescription(dateHeader.Tag.ToString(), ListSortDirection.Descending));

            DataContext = viewModel;
            viewModel.CloseAction = delegate (bool? ok) { if (System.Windows.Interop.ComponentDispatcher.IsThreadModal) DialogResult = ok; Close(); };
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            (DataContext as CompetitionViewModel).CloseAction.Invoke(true);
        }

        private void GridViewColumnHeader_Click(object sender, RoutedEventArgs e)
        {
            GridViewColumnHeader column = sender as GridViewColumnHeader;
            string sortBy = column.Tag.ToString();
            if (sortColumn != null)
            {
                AdornerLayer.GetAdornerLayer(sortColumn).Remove(sortAdorner);
            }
            listView.Items.SortDescriptions.Clear();

            ListSortDirection newDirection = ListSortDirection.Ascending;
            if (sortColumn == column && sortAdorner.Direction == newDirection)
            {
                newDirection = ListSortDirection.Descending;
            }

            sortColumn = column;
            sortAdorner = new SortAdorner(sortColumn, newDirection);
            AdornerLayer.GetAdornerLayer(sortColumn).Add(sortAdorner);
            listView.Items.SortDescriptions.Add(new SortDescription(sortBy, newDirection));
        }
    }
}
