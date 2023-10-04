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
        public class CompetitionLevel
        {
            public int? Level { get; set; }
            public string Title { get; set; }

            public CompetitionLevel(int? level, string title)
            {
                Level = level;
                Title = title;
            }

            public static List<CompetitionLevel> CreateDefaultList()
            {
                return new List<CompetitionLevel>
                {
                    new CompetitionLevel(null, ""),
                    new CompetitionLevel(1, "1"),
                    new CompetitionLevel(2, "2"),
                    new CompetitionLevel(3, "3"),
                    new CompetitionLevel(4, "4"),
                    new CompetitionLevel(5, "5"),
                    new CompetitionLevel(0, "?")
                };
            }
        }

        public Competition SelectedCompetition { get; set; }
        public ListCollectionView Competitions { get; set; }
        public bool ReloadData { get; set; }

        public List<Country> Countries { get; set; } = Country.CreateDefaultList();
        public List<Country> EmptyCountries { get; set; } = new List<Country> { new Country() { Id = 0, Code = "", Title = "" } };
        public List<CompetitionLevel> Levels { get; set; } = CompetitionLevel.CreateDefaultList();

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

        private int? _levelFilter = null;
        public int? LevelFilter
        {
            get { return _levelFilter; }
            set
            {
                _levelFilter = value;
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
            accepted &= LevelFilter == null || (LevelFilter == 0 ? (competition.Level < 1 || competition.Level > 5) : competition.Level == LevelFilter);

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
