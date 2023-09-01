using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Input;
using MakeReadyGeneral.Models;
using MakeReadyWpf.Commands;

namespace MakeReadyWpf
{
    public class CompetitionViewModel
    {
        public Competition SelectedCompetition { get; set; }
        public ListCollectionView Competitions { get; set; }
        public bool ReloadData { get; set; }

        public List<Country> Countries { get; set; } = Country.CreateDefaultList();
        public List<Country> EmptyCountries { get; set; } = new List<Country> { new Country() { Id = 0, Code = "", Title = "" } };

        #region Filters

        private string _countryFilter = "";
        public string CountryFilter
        {
            get { return _countryFilter; }
            set
            {
                _countryFilter = value;
                Competitions.Refresh();
            }
        }

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

        private DateTime? _dateStart;
        public DateTime? DateStart
        {
            get { return _dateStart; }
            set
            {
                _dateStart = value;
                Competitions.Refresh();
            }
        }

        private DateTime? _dateEnd;
        public DateTime? DateEnd
        {
            get { return _dateEnd; }
            set
            {
                _dateEnd = value;
                Competitions.Refresh();
            }
        }

        #endregion

        public CompetitionViewModel(List<Competition> competitions)
        {
            Competitions = new ListCollectionView(competitions);
            Competitions.Filter = ListCollectionFilter;
        }

        private bool ListCollectionFilter(object item)
        {
            var competition = item as Competition;

            bool accepted = true;
            accepted &= string.IsNullOrEmpty(CountryFilter) || competition.CountryCode.IndexOf(CountryFilter, StringComparison.OrdinalIgnoreCase) >= 0;
            accepted &= string.IsNullOrEmpty(TitleFilter) || competition.Title.IndexOf(TitleFilter, StringComparison.OrdinalIgnoreCase) >= 0;

            if (DateStart != null)
            {
                if (DateEnd != null)
                {
                    accepted &= competition.EventDate >= DateStart && competition.EventDate <= DateEnd;
                }
                else
                {
                    accepted &= competition.EventDate >= DateStart;
                }
            }

            return accepted;
        }

        public Action<bool?> CloseAction { get; set; }

        private ICommand _okCommand;
        public ICommand OkCommand => _okCommand ?? (_okCommand = new RelayCommand(CloseDialog, () => SelectedCompetition != null));

        private void CloseDialog()
        {
            CloseAction?.Invoke(true);
        }
    }
}
