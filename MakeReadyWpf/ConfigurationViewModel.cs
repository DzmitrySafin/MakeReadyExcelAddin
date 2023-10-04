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
using System.Windows.Data;
using System.Windows.Input;

namespace MakeReadyWpf
{
    public class ConfigurationViewModel
    {
        public ListCollectionView Competitions { get; set; }
        public ListCollectionView SelectedCompetitions { get; set; }

        private string _titleFilter = "";
        public string TitleFilter
        {
            get { return _titleFilter; }
            set
            {
                _titleFilter = value;
                Competitions.Refresh();
            }
        }

        public ConfigurationViewModel(List<Competition> competitions)
        {
            Competitions = new ListCollectionView(competitions);
            Competitions.Filter = ListCollectionFilter;

            SelectedCompetitions = new ListCollectionView(competitions);
            SelectedCompetitions.Filter = ListSelectedCollectionFilter;
        }

        private bool ListCollectionFilter(object item)
        {
            var competition = item as Competition;

            bool accepted = true;
            accepted &= string.IsNullOrEmpty(TitleFilter) || competition.Title.IndexOf(TitleFilter, StringComparison.OrdinalIgnoreCase) >= 0;

            return accepted;
        }

        private bool ListSelectedCollectionFilter(object item)
        {
            var competition = item as Competition;

            return competition.IsSelected;
        }

        public ICommand CompetitionCheckedCommand
        {
            get
            {
                return new RelayCommand(() => SelectedCompetitions.Refresh());
            }
        }

        public ICommand CompetitionUncheckCommand
        {
            get
            {
                return new RelayCommand<Competition>((item) =>
                {
                    item.IsSelected = false;
                    SelectedCompetitions.Refresh();
                });
            }
        }

        public Action<bool?> CloseAction { get; set; }

        private ICommand _okCommand;
        //public ICommand OkCommand => _okCommand ?? (_okCommand = new RelayCommand(CloseDialog, () => SelectedCompetition != null));
        public ICommand OkCommand => _okCommand ?? (_okCommand = new RelayCommand(CloseDialog));

        private void CloseDialog()
        {
            CloseAction?.Invoke(true);
        }
    }
}
