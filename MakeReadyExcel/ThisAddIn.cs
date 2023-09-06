using MakeReadyExcel.Helpers;
using MakeReadyExcel.Models;
using MakeReadyExcel.Properties;
using MakeReadyGeneral.Models;
using MakeReadyWpf;
using MakeReadyWpf.Helpers;
using NLog;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Serialization;
using Excel = Microsoft.Office.Interop.Excel;
using Office = Microsoft.Office.Core;

namespace MakeReadyExcel
{
    public partial class ThisAddIn
    {
        private const string XmlPartName = "MakeReadyXmlData";
        private const string WebSiteName = "MakeReady.by";

        private static readonly Logger logger = LogManager.GetCurrentClassLogger();
        private static readonly ToastViewModel Toast = new ToastViewModel();
        private static readonly ToastWindow toastWindow = new ToastWindow(Toast);

        internal MakeReady StandbyConnector { get; private set; } = new MakeReady();
        internal Performance StandbyData { get; private set; } = new Performance();
        internal Competition SelectedCompetition { get; private set; }
        internal List<Shooter> FilteredShooters { get; private set; } = new List<Shooter>();

        internal int SelectedDivisionId { get; private set; } = -1;
        internal int SelectedDivisionIndex { get; private set; } = 0;
        internal int SelectedShooterId { get; private set; } = -1;
        internal int SelectedShooterIndex { get; private set; } = 0;

        protected override Office.IRibbonExtensibility CreateRibbonExtensibilityObject()
        {
            return new Ribbon();
        }

        private void ThisAddIn_Startup(object sender, EventArgs e)
        {
            //((Excel.AppEvents_Event)Application).NewWorkbook += Application_NewWorkbook;
            Application.WorkbookOpen += Application_WorkbookOpen;
            Application.WorkbookActivate += Application_WorkbookActivate;

            StandbyConnector.LoginToken = Settings.Default.UserToken;
            StandbyConnector.UserName = Settings.Default.UserName;
            StandbyConnector.LoginEmail = Settings.Default.UserEmail;
            StandbyConnector.LoginTimestamp = Settings.Default.LoginTime;
        }

        private void ThisAddIn_Shutdown(object sender, EventArgs e)
        {
            //
        }

        private void Application_WorkbookOpen(Excel.Workbook wb)
        {
            var xmlPart = wb.GetCustomXmlPart(XmlPartName);
            if (xmlPart == null) return;

            var xmlSerializer = new XmlSerializer(typeof(Performance));
            try
            {
                Performance performance;
                using (TextReader tr = new StringReader(xmlPart.XML))
                {
                    performance = (Performance)xmlSerializer.Deserialize(tr);
                }

                // merge matches
                if (DateTime.Compare(StandbyData.TimestampLoaded, performance.TimestampLoaded) < 0)
                {
                    StandbyData.TimestampLoaded = performance.TimestampLoaded;
                }
                if (StandbyData.Competitions.Any())
                {
                    foreach (Competition competition in performance.Competitions)
                    {
                        var existing = StandbyData.Competitions.FirstOrDefault(c => c.Id == competition.Id);
                        if (existing != null)
                        {
                            if (existing.IsCompleted || !competition.IsCompleted) continue;
                            StandbyData.Competitions.Remove(existing);
                        }
                        StandbyData.Competitions.Add(competition);
                        if (competition.IsCompleted)
                        {
                            competition.CalculateStatistics();
                        }
                    }
                    UpdateOpenedWorkbooks();
                }
                else
                {
                    foreach (Competition competition in performance.Competitions)
                    {
                        if (competition.IsCompleted)
                        {
                            competition.CalculateStatistics();
                        }
                    }
                    StandbyData.Competitions = performance.Competitions;
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        private void UpdateOpenedWorkbooks()
        {
            try
            {
                foreach (Excel.Workbook wb in Application.Workbooks)
                {
                    var xmlPart = wb.GetCustomXmlPart(XmlPartName);
                    if (xmlPart != null)
                    {
                        var xmlSerializer = new XmlSerializer(typeof(Performance));
                        var sb = new StringBuilder();
                        using (TextWriter tw = new StringWriter(sb))
                        {
                            xmlSerializer.Serialize(tw, StandbyData);
                        }

                        wb.SetCustomXmlPart(XmlPartName, sb.ToString());
                        wb.Saved = false;
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                Toast.ShowToast("Update workbook info", $"Error occured when trying to update workbook data.\n{ex.Message}", false, false, true);
            }
        }

        internal delegate void RibbonEventHandler();
        internal event RibbonEventHandler RibbonEvent;
        private void Application_WorkbookActivate(Excel.Workbook wb)
        {
            if (RibbonEvent != null) RibbonEvent.Invoke();
        }

        public bool IsActiveWorkbookData()
        {
            return !string.IsNullOrEmpty(Application.ActiveWorkbook.GetCustomXmlPartProperty(XmlPartName));
        }

        public void DivisionItemSelected(int divisionId, int divisionIndex)
        {
            FilterShooters(divisionId);
            SelectedDivisionId = divisionId;
            SelectedDivisionIndex = divisionIndex;
            SelectedShooterId = FilteredShooters[0].Id;
            SelectedShooterIndex = 0;
        }

        public void ShooterItemSelected(int shooterId, int shooterIndex)
        {
            SelectedShooterId = shooterId;
            SelectedShooterIndex = shooterIndex;
        }

        #region VSTO generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InternalStartup()
        {
            this.Startup += new System.EventHandler(ThisAddIn_Startup);
            this.Shutdown += new System.EventHandler(ThisAddIn_Shutdown);
        }

        #endregion

        #region Login/Logout

        public async Task<bool> Login()
        {
            bool? result;
            var vm = new LoginViewModel(Settings.Default.UserEmail);
            Toast.ShowToast("Log In", $"Logging into {WebSiteName} ...", true, true);

            using (Application.SwitchCursor(Excel.XlMousePointer.xlNorthwestArrow))
            {
                result = await StaTask.RunStaTask(() =>
                {
                    return new LoginWindow(vm).ShowDialog();
                });

                if (result != true)
                {
                    Toast.HideToast();
                    return false;
                }

                result = await StandbyConnector.Login(vm.Email, vm.Password);
            }

            if (result == true)
            {
                Toast.CompleteProgress($"Successfully logged into {WebSiteName}");
                Settings.Default.LoginTime = StandbyConnector.LoginTimestamp;
                Settings.Default.UserEmail = StandbyConnector.LoginEmail;
                Settings.Default.UserToken = StandbyConnector.LoginToken;
                Settings.Default.UserName = StandbyConnector.UserName;
                Settings.Default.Save();
            }
            else if (result == false)
            {
                Toast.CompleteProgress($"Could not log into {WebSiteName}\nEmail and/or password might be incorrect.", true);
            }
            else
            {
                Toast.CompleteProgress($"Could not log into {WebSiteName}\nResponse is not as expected.", true);
            }

            return result == true;
        }

        public async Task<bool> Logout()
        {
            bool? result;
            Toast.ShowToast("Log Out", $"Logging out from {WebSiteName} ...", true, true);

            using (Application.SwitchCursor(Excel.XlMousePointer.xlWait))
            {
                result = await StandbyConnector.Logout();
            }

            if (result == true)
            {
                Toast.CompleteProgress($"Successfully logged out from {WebSiteName}");
                Settings.Default.UserToken = "";
                Settings.Default.UserName = "";
                Settings.Default.Save();
            }
            else if (result == false)
            {
                Toast.CompleteProgress($"Could not log out from {WebSiteName}\nLogin cookie might be incorrect.", true);
            }
            else
            {
                Toast.CompleteProgress($"Could not log out from {WebSiteName}\nResponse is not as expected.", true);
            }

            return result == true;
        }

        public async Task<bool> LoadCompetitions()
        {
            await StaTask.RunStaTask(() =>
            {
                Toast.ShowToast("Load matches", "Loading list of matches ...", true, true);
            });

            List<Competition> competitions;
            using (Application.SwitchCursor(Excel.XlMousePointer.xlWait))
            {
                competitions = await StandbyConnector.LoadCompetitions(Login);
            }

            if (competitions == null)
            {
                Toast.CompleteProgress($"Could not load matches from {WebSiteName}", true);
            }
            else if (competitions.Count == 0)
            {
                Toast.CompleteProgress($"Could not load matches from {WebSiteName}\nResponse is not as expected.", true);
            }
            else
            {
                StandbyData.TimestampLoaded = DateTime.Now;
                Toast.CompleteProgress($"Successfully loaded {competitions.Count} matches.");
                if (StandbyData.Competitions.Any())
                {
                    foreach (Competition competition in competitions)
                    {
                        if (!StandbyData.Competitions.Any(c => c.Id == competition.Id))
                        {
                            StandbyData.Competitions.Add(competition);
                        }
                    }
                }
                else
                {
                    StandbyData.Competitions = competitions;
                }
                UpdateOpenedWorkbooks();
                return true;
            }

            return false;
        }

        #endregion

        #region Shooters

        public async Task<bool> SelectMatchAndShooters()
        {
            var vm = new CompetitionViewModel(StandbyData.Competitions);
            vm.CountryFilter = Settings.Default.FilterCountry;
            if (Settings.Default.FilterDateStart.Year >= 2000)
            {
                vm.DateStart = Settings.Default.FilterDateStart;
                if (Settings.Default.FilterDateEnd.Year >= 2000) vm.DateEnd = Settings.Default.FilterDateEnd;
            }

            bool? result = await StaTask.RunStaTask(() =>
            {
                return new CompetitionWindow(vm).ShowDialog();
            });
            if (result != true) return false;

            Settings.Default.FilterCountry = vm.CountryFilter;
            Settings.Default.FilterDateStart = vm.DateStart.HasValue ? vm.DateStart.Value : DateTime.MinValue;
            Settings.Default.FilterDateEnd = vm.DateEnd.HasValue ? vm.DateEnd.Value : DateTime.MinValue;
            Settings.Default.Save();

            SelectedCompetition = vm.SelectedCompetition;
            if (!SelectedCompetition.IsCompleted || vm.ReloadData)
            {
                result = await LoadShooters(SelectedCompetition);
            }
            else
            {
                DivisionItemSelected(-1, 0);
            }

            return result == true;
        }

        private async Task<bool> LoadShooters(Competition competition)
        {
            Tuple<List<Shooter>, List<Stage>> tuple;
            using (Application.SwitchCursor(Excel.XlMousePointer.xlWait))
            {
                Toast.ShowToast("Load match data", "Loading shooters data", true, true);
                tuple = await StandbyConnector.LoadShooters(competition.Id, Login);
            }

            if (tuple == null)
            {
                Toast.ShowToast("Load match data", $"Could not load shooters/stages from {WebSiteName}", false, false, true);
                return false;
            }

            competition.Stages = tuple.Item2;
            competition.StagesCount = tuple.Item2.Count;
            competition.TotalPoints = tuple.Item2.Sum(s => s.MaxPoints);

            List<Shooter> shooters = tuple.Item1.Where(sh => !sh.Name.StartsWith("MegaBeast")).ToList();
            competition.Divisions = CreateDivisions(shooters);
            competition.ShootersCount = shooters.Count;

            if (!LoadStages(competition.Id, competition.StagesCount, shooters))
            {
                Toast.CompleteProgress($"Could not load shooters results from {WebSiteName}", true);
                return false;
            }
            else
            {
                Toast.CompleteProgress("Shooters data has been loaded successfully.");
            }

            competition.IsCompleted = true;
            competition.CalculateStatistics();
            DivisionItemSelected(-1, 0);

            await StaTask.RunStaTask(() =>
            {
                UpdateOpenedWorkbooks();
            });

            return true;
        }

        private List<Division> CreateDivisions(List<Shooter> shooters)
        {
            var divDefaults = Division.CreateDefaultList();
            var codes = shooters.Select(sh => sh.DivisionCode).Distinct().ToList();
            codes.Sort();

            var divisions = new List<Division>();
            foreach (int code in codes)
            {
                var division = divDefaults.FirstOrDefault(d => d.Id == code);
                if (division == null) divisions.Add(new Division(code, $"div {code}"));
                else divisions.Add(division);

                division.Shooters = shooters.Where(sh => sh.DivisionCode == division.Id).ToList();
                division.ShootersCount = division.Shooters.Count;
            }
            return divisions;
        }

        private bool LoadStages(string competitionId, int stagesCount, List<Shooter> shooters)
        {
            bool success = true;
            using (Application.SwitchCursor(Excel.XlMousePointer.xlWait))
            {
                int counter = 0;
                foreach (Shooter shooter in shooters)
                {
                    shooter.SetAdditionalProperties();
                    Toast.SetPercentage($"Loading data for {shooter.ShortName} ({++counter} of {shooters.Count})", shooters.Count, counter);
                    var accuracy = Task.Run(async () => await StandbyConnector.LoadAccuracy(competitionId, shooter.Id, Login)).Result;
                    if (accuracy == null)
                    {
                        if (competitionId == "1585cd3ab8b723c8b4869b02ecf38b10" && shooter.Id == 4)
                        {
                            accuracy = StageResult.CreateDefaultList(stagesCount);
                        }
                        else if (competitionId == "c040377370a7155f03e1ec131d2a4909" && (shooter.Id == 2 || shooter.Id == 5))
                        {
                            accuracy = StageResult.CreateDefaultList(stagesCount);
                        }
                        else if (competitionId == "63cd337637d35caea6c389e2bd527fe4" && (shooter.Id == 38 || shooter.Id == 15))
                        {
                            accuracy = StageResult.CreateDefaultList(stagesCount);
                        }
                        else
                        {
                            accuracy = StageResult.CreateDefaultList(stagesCount);
                            string msg = $"Could not load data for shooter {shooter.Id} ({shooter.ShortName}), match {competitionId}.";
                            logger.Warn(msg);
                            Toast.ShowToast("Load shooters results", msg, false, false, true);
                            //success = false;
                            //break;
                        }
                    }
                    else if (accuracy.Count != stagesCount)
                    {
                        string msg = $"Number of results doesn't fit number of stages for shooter {shooter.Id} ({shooter.ShortName}), match {competitionId}";
                        logger.Error(msg);
                        Toast.ShowToast("Load shooters results", msg, false, false, true);
                        success = false;
                        break;
                    }
                    shooter.CompetitionResults = accuracy;
                }
            }

            return success;
        }

        private void FilterShooters(int divisionId)
        {
            if (divisionId < 0)
            {
                FilteredShooters = SelectedCompetition.Divisions.SelectMany(d => d.Shooters).ToList();
            }
            else
            {
                FilteredShooters = SelectedCompetition.Divisions.First(d => d.Id == divisionId).Shooters;
            }
        }

        #endregion

        #region Charts

        public void DivisionChartByShooter()
        {
            Excel.Worksheet ws = Application.ActiveWorkbook.Worksheets.Add();

            try
            {
                CreateChartByShooter(ws);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        public void DivisionChartByStage()
        {
            Excel.Worksheet ws = Application.ActiveWorkbook.Worksheets.Add();

            try
            {
                CreateChartByStage(ws);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        public void Chart2SingleShooter()
        {
            Excel.Worksheet ws = Application.ActiveWorkbook.Worksheets.Add();

            Shooter currentShooter = FilteredShooters.First(sh => sh.Id == SelectedShooterId);
            CreateChartSingleShooter(ws, SelectedCompetition, currentShooter, 1, true);
        }

        public void Chart3SingleShooter()
        {
            Excel.Worksheet ws = Application.ActiveWorkbook.Worksheets.Add();

            int row = 1;
            Shooter currentShooter = FilteredShooters.First(sh => sh.Id == SelectedShooterId);
            string[] names = currentShooter.ShortName.Split(new[] { '\x20', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            string lastName = names[0];
            string firstName = names.Length > 1 ? names[1] : names[0];
            foreach (var competition in StandbyData.Competitions.Where(c => c.IsCompleted).OrderByDescending(c => c.EventDate))
            {
                var shooters = competition.Divisions.SelectMany(d => d.Shooters).Where(sh => sh.Name.Contains(lastName) && sh.Name.Contains(firstName)).ToList();
                foreach (Shooter shooter in shooters)
                {
                    row = CreateChartSingleShooter(ws, competition, shooter, row, false);
                }
            }
        }

        private void CreateChartByShooter(Excel.Worksheet ws)
        {
            string decSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            Excel.ChartObjects charts = (Excel.ChartObjects)ws.ChartObjects(Type.Missing);
            List<Stage> stages = SelectedCompetition.Stages;
            List<Shooter> shooters = FilteredShooters;

            const int row = 1; const int col = 1; // starting point (header row)
            const int c2 = col + 1; const int c8 = c2 + 6; const int c11 = c8 + 3; const int c14 = c11 + 3; const int cEx = c14 + 1;
            const int chartHeight = 10; const int chartWidth = 5; // height in rows, width in columns
            const int graphHeight = 23; const int graphWidth = 12; // height in rows, width in columns
            int rowStep = Math.Max(stages.Count + 3, chartHeight + 1);

            for (int i = 0; i < shooters.Count; i++)
            {
                int rH = row + i * rowStep; // header row
                int rF = rH + stages.Count + 1; // footer row
                Shooter shooter = shooters[i];
                int pf = (int)shooter.Factor;

                // header
                ws.Cells[rH, col].Value2 = shooter.ShortName;
                ws.Cells[rH, c2].Value2 = "A";
                ws.Cells[rH, c2+1].Value2 = "C";
                ws.Cells[rH, c2+2].Value2 = "D";
                ws.Cells[rH, c2+3].Value2 = "M";
                ws.Cells[rH, c2+4].Value2 = "PT";
                ws.Cells[rH, c2+5].Value2 = "PE";
                ws.Cells[rH, c8].Value2 = "Score";
                ws.Cells[rH, c8+1].Value2 = "Time";
                ws.Cells[rH, c8+2].Value2 = "HF";
                ws.Cells[rH, c11].Value2 = "MAX";
                ws.Cells[rH, c11+1].Value2 = "%";
                ws.Cells[rH, c11+2].Value2 = "Points";
                ws.Cells[rH, c14].Value2 = "Place";

                // body
                for (int j = 0; j < stages.Count; j++)
                {
                    var res = shooter.CompetitionResults[j];
                    int rC = rH + j + 1; // current row

                    ws.Cells[rC, col].Value2 = $"Stage {res.StageNumber}";
                    ws.Cells[rC, c2].Value2 = res.AlphaCount;
                    ws.Cells[rC, c2+1].Value2 = res.CharlieCount;
                    ws.Cells[rC, c2+2].Value2 = res.DeltaCount;
                    ws.Cells[rC, c2+3].Value2 = res.MissCount;
                    ws.Cells[rC, c2+4].Value2 = res.NoshootCount;
                    ws.Cells[rC, c2+5].Value2 = res.PenaltyCount;
                    ws.Cells[rC, c8].Formula = $"=MAX(0,{ws.Cells[rC, c2].Address}*5+{ws.Cells[rC, c2+1].Address}*{pf + 2}+{ws.Cells[rC, c2+2].Address}*{pf}-({ws.Cells[rC, c2+3].Address}+{ws.Cells[rC, c2+4].Address}+{ws.Cells[rC, c2+5].Address})*10)";
                    ws.Cells[rC, c8+1].Value2 = res.TimeTaken;
                    ws.Cells[rC, c8+2].NumberFormat = $"0{decSeparator}0000";
                    ws.Cells[rC, c8+2].Formula = $"=IF({ws.Cells[rC, c8+1].Address}>0,{ws.Cells[rC, c8].Address}/{ws.Cells[rC, c8+1].Address},0)";
                    ws.Cells[rC, c11].Value2 = stages[j].MaxPoints;
                    ws.Cells[rC, c11+1].NumberFormat = $"0{decSeparator}00%";
                    ws.Cells[rC, c11+1].Value2 = "0%";
                    ws.Cells[rC, c11+2].NumberFormat = $"0{decSeparator}00";
                    ws.Cells[rC, c11+2].Formula = $"={ws.Cells[rC, c11].Address}*{ws.Cells[rC, c11+1].Address}";
                    ws.Cells[rC, c14].Value2 = "0";
                }

                // footer
                ws.Cells[rF, col].Value2 = "Total SUM";
                ws.Cells[rF, c2].Formula = $"=SUM({ws.Cells[rH+1, c2].Address}:{ws.Cells[rF-1, c2].Address})";
                ws.Cells[rF, c2+1].Formula = $"=SUM({ws.Cells[rH+1, c2+1].Address}:{ws.Cells[rF-1, c2+1].Address})";
                ws.Cells[rF, c2+2].Formula = $"=SUM({ws.Cells[rH+1, c2+2].Address}:{ws.Cells[rF-1, c2+2].Address})";
                ws.Cells[rF, c2+3].Formula = $"=SUM({ws.Cells[rH+1, c2+3].Address}:{ws.Cells[rF-1, c2+3].Address})";
                ws.Cells[rF, c2+4].Formula = $"=SUM({ws.Cells[rH+1, c2+4].Address}:{ws.Cells[rF-1, c2+4].Address})";
                ws.Cells[rF, c2+5].Formula = $"=SUM({ws.Cells[rH+1, c2+5].Address}:{ws.Cells[rF-1, c2+5].Address})";
                ws.Cells[rF, c8].Formula = $"=SUM({ws.Cells[rH+1, c8].Address}:{ws.Cells[rF-1, c8].Address})";
                ws.Cells[rF, c8+1].Formula = $"=SUM({ws.Cells[rH+1, c8+1].Address}:{ws.Cells[rF-1, c8+1].Address})";
                ws.Cells[rF, c8+2].Formula = $"=SUM({ws.Cells[rH+1, c8+2].Address}:{ws.Cells[rF-1, c8+2].Address})";
                ws.Cells[rF, c11].Value2 = "";
                ws.Cells[rF, c11+1].Value2 = "";
                ws.Cells[rF, c11+2].Formula = $"=SUM({ws.Cells[rH+1, c11+2].Address}:{ws.Cells[rF-1, c11+2].Address})";
                ws.Cells[rF, c14].Value2 = "";

                // top row and left column - bold font
                ws.Range[ws.Cells[rH, col], ws.Cells[rH, cEx-1]].Font.Bold = true;
                ws.Range[ws.Cells[rH, col], ws.Cells[rF, col]].Font.Bold = true;
                // center headers on the top row
                ws.Range[ws.Cells[rH, c2], ws.Cells[rH, cEx-1]].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                // mark results columns with color
                ws.Range[ws.Cells[rH, c2], ws.Cells[rH, c2+5]].Interior.Color = Excel.XlRgbColor.rgbLightGreen;
                ws.Cells[rH, c8+1].Interior.Color = Excel.XlRgbColor.rgbLightGreen;
                // top/left corner - bigger font for shooter name
                ws.Range[ws.Cells[rH, col], ws.Cells[rH, col]].Font.Size++;
                // vertical borders
                DrawVerticalLine(ws, c2, rH, rF);
                DrawVerticalLine(ws, c8, rH, rF);
                DrawVerticalLine(ws, c11, rH, rF);
                DrawVerticalLine(ws, cEx, rH, rF);
                // horizontal borders
                DrawHorizontalLine(ws, rH, col, cEx-1);
                DrawHorizontalLine(ws, rF-1, col, cEx-1);
                DrawHorizontalLine(ws, rF, col, cEx-1);

                // pie chart
                Excel.Range range1 = ws.Range[ws.Cells[rH, c2], ws.Cells[rH, c2+5]]; // headers
                Excel.Range range2 = ws.Range[ws.Cells[rF, c2], ws.Cells[rF, c2+5]]; // SUMs
                double cLeft = ws.Cells[rH, cEx + 1].Left;
                double cWidth = ws.Cells[rH, cEx + 1 + chartWidth].Left - cLeft;
                double cTop = ws.Cells[rH, 1].Top;
                double cHeight = ws.Cells[rH + chartHeight, 1].Top - cTop;
                Excel.ChartObject pieChartObject = charts.Add(cLeft, cTop, cWidth, cHeight);
                Excel.Chart pieChart = pieChartObject.Chart;
                pieChart.ChartType = Excel.XlChartType.xlPie;
                pieChart.SetSourceData(Application.Union(range1, range2), Type.Missing);
                pieChart.ApplyLayout(6);
                pieChart.ChartTitle.Delete();
            }
            ws.Columns[col].AutoFit();

            // percent by stage graph
            double gLeft = ws.Cells[row, cEx + 2 + chartWidth].Left;
            double gWidth = ws.Cells[row, cEx + 2 + chartWidth + graphWidth].Left - gLeft;
            double gTop = ws.Cells[row, 1].Top;
            double gHeight = ws.Cells[row + graphHeight, 1].Top - gTop;
            Excel.ChartObject percentChartObject = charts.Add(gLeft, gTop, gWidth, gHeight);
            Excel.Chart percentChart = percentChartObject.Chart;
            percentChart.ChartType = Excel.XlChartType.xlLineMarkers;
            percentChart.HasTitle = true;
            percentChart.ChartTitle.Text = "Percent by Stage";
            for (int i = 0; i < shooters.Count; i++)
            {
                Excel.Series line = percentChart.SeriesCollection().NewSeries();
                line.Name = shooters[i].ShortName;
                int r1 = row + i * rowStep + 1;
                line.Values = ws.Range[ws.Cells[r1, c11 + 1], ws.Cells[r1 + stages.Count - 1, c11 + 1]];
            }

            // time by stage graph
            double gTop2 = ws.Cells[row + graphHeight + 1, 1].Top;
            double gHeight2 = ws.Cells[row + graphHeight * 2 + 1, 1].Top - gTop2;
            Excel.ChartObject timeChartObject = charts.Add(gLeft, gTop2, gWidth, gHeight2);
            Excel.Chart timeChart = timeChartObject.Chart;
            timeChart.ChartType = Excel.XlChartType.xlLineMarkers;
            timeChart.HasTitle = true;
            timeChart.ChartTitle.Text = "Time by Stage";
            for (int i = 0; i < shooters.Count; i++)
            {
                Excel.Series line = timeChart.SeriesCollection().NewSeries();
                line.Name = shooters[i].ShortName;
                int r1 = row + i * rowStep + 1;
                line.Values = ws.Range[ws.Cells[r1, c8 + 1], ws.Cells[r1 + stages.Count - 1, c8 + 1]];
            }

            // HF by stage graph
            double gTop3 = ws.Cells[row + graphHeight * 2 + 2, 1].Top;
            double gHeight3 = ws.Cells[row + graphHeight * 3 + 2, 1].Top - gTop3;
            Excel.ChartObject hfChartObject = charts.Add(gLeft, gTop3, gWidth, gHeight3);
            Excel.Chart hfChart = hfChartObject.Chart;
            hfChart.ChartType = Excel.XlChartType.xlLineMarkers;
            hfChart.HasTitle = true;
            hfChart.ChartTitle.Text = "HF by Stage";
            for (int i = 0; i < shooters.Count; i++)
            {
                Excel.Series line = hfChart.SeriesCollection().NewSeries();
                line.Name = shooters[i].ShortName;
                int r1 = row + i * rowStep + 1;
                line.Values = ws.Range[ws.Cells[r1, c8 + 2], ws.Cells[r1 + stages.Count - 1, c8 + 2]];
            }

            // calculate HF percentage and place
            var hfList = new List<string>();
            var placeList = new List<string>();
            var hfRange = Enumerable.Range(0, shooters.Count);
            for (int j = 0; j < stages.Count; j++)
            {
                hfList.Add(string.Join(",", hfRange.Select(i => ws.Cells[row + 1 + j + i * rowStep, c8 + 2].Address)));
                placeList.Add(string.Join(",", hfRange.Select(i => $"IF({ws.Cells[row + 1 + j + i * rowStep, c11 + 2].Address}>{{0}},1,0)")));
            }
            for (int i = 0; i < shooters.Count; i++)
            {
                for (int j = 0; j < stages.Count; j++)
                {
                    int r1 = row + j + i * rowStep + 1;
                    ws.Cells[r1, c11 + 1].Formula = $"={ws.Cells[r1, c8 + 2].Address}/MAX({hfList[j]})";
                    ws.Cells[r1, c14].Formula = $"=SUM(1,{string.Format(placeList[j], ws.Cells[r1, c11 + 2].Address)})";
                }
            }
        }

        private void CreateChartByStage(Excel.Worksheet ws)
        {
            string decSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            Excel.ChartObjects charts = (Excel.ChartObjects)ws.ChartObjects(Type.Missing);
            List<Stage> stages = SelectedCompetition.Stages;
            List<Shooter> shooters = FilteredShooters;

            const int row = 1; const int col = 1; // starting point
            const int c2 = col + 1; const int c8 = c2 + 6; const int c12 = c8 + 4; const int c15 = c12 + 3; const int cEx = c15 + 2;
            int rowStep = shooters.Count + 3;

            for (int i = 0; i < stages.Count; i++)
            {
                int rH = row + i * rowStep; // header row
                int rF = rH + shooters.Count + 1; // footer row
                Stage stage = stages[i];
                shooters = shooters.OrderByDescending(sh => sh.CompetitionResults[i].HitFactor).ToList();

                // header
                ws.Cells[rH, col].Value2 = $"Stage {stage.StageNumber} - {stage.StageName}";
                ws.Cells[rH, col].AddComment($"{stage.PaperNumber} paper(s), {stage.PopperNumber} popper(s), {stage.PlateNumber} plate(s), {stage.DisappearNumber} disappear, {stage.PenaltyNumber} penalty");
                ws.Cells[rH, c2].Value2 = "A";
                ws.Cells[rH, c2+1].Value2 = "C";
                ws.Cells[rH, c2+2].Value2 = "D";
                ws.Cells[rH, c2+3].Value2 = "M";
                ws.Cells[rH, c2+4].Value2 = "PT";
                ws.Cells[rH, c2+5].Value2 = "PE";
                ws.Cells[rH, c8].Value2 = "Score";
                ws.Cells[rH, c8+1].Value2 = "Time";
                ws.Cells[rH, c8+2].Value2 = "A-Time";
                ws.Cells[rH, c8+3].Value2 = "HF";
                ws.Cells[rH, c12].Value2 = "%";
                ws.Cells[rH, c12+1].Value2 = "Points";
                ws.Cells[rH, c12+2].Value2 = "Place";
                ws.Cells[rH, c15].Value2 = "MAX";
                ws.Cells[rH, c15+1].Value2 = "Value";

                // body
                for (int j = 0; j < shooters.Count(); j++)
                {
                    Shooter shooter = shooters[j];
                    int pf = (int)shooter.Factor;
                    var res = shooter.CompetitionResults[i];
                    int rC = rH + j + 1; // current row

                    ws.Cells[rC, col].Value2 = shooter.ShortName;
                    ws.Cells[rC, c2].Value2 = res.AlphaCount;
                    ws.Cells[rC, c2+1].Value2 = res.CharlieCount;
                    ws.Cells[rC, c2+2].Value2 = res.DeltaCount;
                    ws.Cells[rC, c2+3].Value2 = res.MissCount;
                    ws.Cells[rC, c2+4].Value2 = res.NoshootCount;
                    ws.Cells[rC, c2+5].Value2 = res.PenaltyCount;
                    ws.Cells[rC, c8].Formula = $"=MAX(0,{ws.Cells[rC, c2].Address}*5+{ws.Cells[rC, c2 + 1].Address}*{pf + 2}+{ws.Cells[rC, c2 + 2].Address}*{pf}-({ws.Cells[rC, c2 + 3].Address}+{ws.Cells[rC, c2 + 4].Address}+{ws.Cells[rC, c2 + 5].Address})*10)";
                    ws.Cells[rC, c8+1].NumberFormat = $"0{decSeparator}00";
                    ws.Cells[rC, c8+1].Value2 = res.TimeTaken;
                    ws.Cells[rC, c8+2].NumberFormat = $"0{decSeparator}00";
                    ws.Cells[rC, c8+2].Formula = $"=IF({ws.Cells[rC, c8].Address}>0,{ws.Cells[rC - j + shooters.Count, c15].Address}*{ws.Cells[rC, c8 + 1].Address}/{ws.Cells[rC, c8].Address},0)";
                    ws.Cells[rC, c8+3].NumberFormat = $"0{decSeparator}0000";
                    ws.Cells[rC, c8+3].Formula = $"=IF({ws.Cells[rC, c8 + 1].Address}>0,{ws.Cells[rC, c8].Address}/{ws.Cells[rC, c8 + 1].Address},0)";
                    ws.Cells[rC, c12].NumberFormat = $"0{decSeparator}00%";
                    ws.Cells[rC, c12].Formula = $"={ws.Cells[rC, c8 + 3].Address}/MAX({ws.Cells[rC - j, c8 + 3].Address}:{ws.Cells[rC - j + shooters.Count - 1, c8 + 3].Address})";
                    ws.Cells[rC, c12+1].NumberFormat = $"0{decSeparator}00";
                    ws.Cells[rC, c12+1].Formula = $"={ws.Cells[rC - j + shooters.Count, c15].Address}*{ws.Cells[rC, c12].Address}";
                    ws.Cells[rC, c12+2].Value2 = $"=COUNTIF({ws.Cells[rC - j, c8 + 3].Address}:{ws.Cells[rC - j + shooters.Count - 1, c8 + 3].Address},\">\"&{ws.Cells[rC, c8 + 3].Address})+1";
                }

                // footer
                ws.Cells[rF, col].Value2 = "Average";
                ws.Cells[rF, c2].Value2 = "";
                ws.Cells[rF, c2+1].Value2 = "";
                ws.Cells[rF, c2+2].Value2 = "";
                ws.Cells[rF, c2+3].Value2 = "";
                ws.Cells[rF, c2+4].Value2 = "";
                ws.Cells[rF, c2+5].Value2 = "";
                ws.Cells[rF, c8].NumberFormat = $"0{decSeparator}00";
                ws.Cells[rF, c8].Formula = $"=AVERAGEIF({ws.Cells[rH + 1, c8].Address}:{ws.Cells[rF - 1, c8].Address},\">0\")";
                ws.Cells[rF, c8+1].NumberFormat = $"0{decSeparator}00";
                ws.Cells[rF, c8+1].Formula = $"=AVERAGEIF({ws.Cells[rH + 1, c8 + 1].Address}:{ws.Cells[rF - 1, c8 + 1].Address},\">0\")";
                ws.Cells[rF, c8+2].NumberFormat = $"0{decSeparator}00";
                ws.Cells[rF, c8+2].Formula = $"=AVERAGEIF({ws.Cells[rH + 1, c8 + 2].Address}:{ws.Cells[rF - 1, c8 + 2].Address},\">0\")";
                ws.Cells[rF, c8+3].NumberFormat = $"0{decSeparator}0000";
                ws.Cells[rF, c8+3].Formula = $"=AVERAGEIF({ws.Cells[rH + 1, c8 + 3].Address}:{ws.Cells[rF - 1, c8 + 3].Address},\">0\")";
                ws.Cells[rF, c12].NumberFormat = $"0{decSeparator}00%";
                ws.Cells[rF, c12].Formula = $"=AVERAGEIF({ws.Cells[rH + 1, c12].Address}:{ws.Cells[rF - 1, c12].Address},\">0\")";
                ws.Cells[rF, c12+1].NumberFormat = $"0{decSeparator}00";
                ws.Cells[rF, c12+1].Formula = $"=AVERAGEIF({ws.Cells[rH + 1, c12 + 1].Address}:{ws.Cells[rF - 1, c12 + 1].Address},\">0\")";
                ws.Cells[rF, c12+2].Value2 = "";
                ws.Cells[rF, c15].Value2 = stage.MaxPoints;
                ws.Cells[rF, c15+1].NumberFormat = $"0{decSeparator}00%";
                ws.Cells[rF, c15+1].Value2 = "";

                // top row and left column - bold font
                ws.Range[ws.Cells[rH, col], ws.Cells[rH, cEx-1]].Font.Bold = true;
                ws.Range[ws.Cells[rH, col], ws.Cells[rF, col]].Font.Bold = true;
                // center headers on the top row
                ws.Range[ws.Cells[rH, c2], ws.Cells[rH, cEx-1]].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
                // mark results columns with color
                ws.Range[ws.Cells[rH, c2], ws.Cells[rH, c2 + 5]].Interior.Color = Excel.XlRgbColor.rgbLightGreen;
                ws.Cells[rH, c8+1].Interior.Color = Excel.XlRgbColor.rgbLightGreen;
                // top/left corner - bigger font for shooter name
                ws.Range[ws.Cells[rH, col], ws.Cells[rH, col]].Font.Size++;
                // vertical borders
                DrawVerticalLine(ws, c2, rH, rF);
                DrawVerticalLine(ws, c8, rH, rF);
                DrawVerticalLine(ws, c12, rH, rF);
                DrawVerticalLine(ws, c15, rH, rF);
                DrawVerticalLine(ws, cEx, rH, rF);
                // horizontal borders
                DrawHorizontalLine(ws, rH, col, cEx-1);
                DrawHorizontalLine(ws, rF - 1, col, cEx-1);
                DrawHorizontalLine(ws, rF, col, cEx-1);
            }
            ws.Columns[1].AutoFit();

            // calculate stage % value
            var maxRange = Enumerable.Range(0, stages.Count);
            string maxSum = string.Join(",", maxRange.Select(i => ws.Cells[row + shooters.Count + 1 + i * rowStep, c15].Address));
            for (int i = 0; i < stages.Count; i++)
            {
                ws.Cells[row + shooters.Count + 1 + i * rowStep, c15 + 1].Formula = $"={ws.Cells[row + shooters.Count + 1 + i * rowStep, c15].Address}/SUM({maxSum})";
            }
        }

        private int CreateChartSingleShooter(Excel.Worksheet ws, Competition competition, Shooter shooter, int startRow, bool drawGraph)
        {
            string decSeparator = CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
            Excel.ChartObjects charts = (Excel.ChartObjects)ws.ChartObjects(Type.Missing);
            List<Stage> stages = competition.Stages;
            int pf = (int)shooter.Factor;

            int row = startRow + 1; const int col = 1; // starting point
            const int c2 = col + 1; const int c8 = c2 + 6; const int c11 = c8 + 3; const int cEx = c11 + 4;
            int rH = row; // header row
            int rF = rH + stages.Count + 1; // footer row
            const int chartHeight = 12; const int chartWidth = 6; // height in rows, width in columns
            int graphHeight = 23; const int graphWidth = 13; // height in rows, width in columns

            // top header
            ws.Cells[startRow, col].Value2 = $"{competition.EventDate:yyyy-MM-dd} - {competition.Title}";
            ws.Cells[startRow, col].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            ws.Cells[startRow, col].Font.Bold = true;
            ws.Range[ws.Cells[startRow, col], ws.Cells[startRow, c11 + 3]].Merge();

            // header
            ws.Cells[row, col].Value2 = shooter.ShortName;
            ws.Cells[row, c2].Value2 = "A";
            ws.Cells[row, c2+1].Value2 = "C";
            ws.Cells[row, c2+2].Value2 = "D";
            ws.Cells[row, c2+3].Value2 = "M";
            ws.Cells[row, c2+4].Value2 = "PT";
            ws.Cells[row, c2+5].Value2 = "PE";
            ws.Cells[row, c8].Value2 = "Score";
            ws.Cells[row, c8+1].Value2 = "Time";
            ws.Cells[row, c8+2].Value2 = "HF";
            ws.Cells[row, c11].Value2 = "MAX";
            ws.Cells[row, c11+1].Value2 = "%";
            ws.Cells[row, c11+2].Value2 = "Points";
            ws.Cells[row, c11+3].Value2 = "Place";

            // body
            for (int i = 0; i < stages.Count; i++)
            {
                Stage stage = stages[i];
                var res = shooter.CompetitionResults[i];
                int rC = rH + i + 1; // current row

                ws.Cells[rC, col].Value2 = $"Stage {res.StageNumber}";
                ws.Cells[rC, c2].Value2 = res.AlphaCount;
                ws.Cells[rC, c2+1].Value2 = res.CharlieCount;
                ws.Cells[rC, c2+2].Value2 = res.DeltaCount;
                ws.Cells[rC, c2+3].Value2 = res.MissCount;
                ws.Cells[rC, c2+4].Value2 = res.NoshootCount;
                ws.Cells[rC, c2+5].Value2 = res.PenaltyCount;
                ws.Cells[rC, c8].Formula = $"=MAX(0,{ws.Cells[rC, c2].Address}*5+{ws.Cells[rC, c2+1].Address}*{pf + 2}+{ws.Cells[rC, c2+2].Address}*{pf}-({ws.Cells[rC, c2+3].Address}+{ws.Cells[rC, c2+4].Address}+{ws.Cells[rC, c2+5].Address})*10)";
                ws.Cells[rC, c8+1].Value2 = res.TimeTaken;
                ws.Cells[rC, c8+2].NumberFormat = $"0{decSeparator}0000";
                ws.Cells[rC, c8+2].Formula = $"=IF({ws.Cells[rC, c8+1].Address}>0,{ws.Cells[rC, c8].Address}/{ws.Cells[rC, c8+1].Address},0)";
                ws.Cells[rC, c11].Value2 = stage.MaxPoints;
                ws.Cells[rC, c11+1].NumberFormat = $"0{decSeparator}00%";
                ws.Cells[rC, c11+1].Value2 = res.RelativeHitFactor;
                ws.Cells[rC, c11+2].NumberFormat = $"0{decSeparator}00";
                ws.Cells[rC, c11+2].Formula = $"={ws.Cells[rC, c11].Address}*{ws.Cells[rC, c11+1].Address}";
                ws.Cells[rC, c11+3].Value2 = res.Place;
            }

            // footer
            ws.Cells[rF, col].Value2 = "Total SUM";
            ws.Cells[rF, c2].Formula = $"=SUM({ws.Cells[rH+1, c2].Address}:{ws.Cells[rF-1, c2].Address})";
            ws.Cells[rF, c2+1].Formula = $"=SUM({ws.Cells[rH+1, c2+1].Address}:{ws.Cells[rF-1, c2+1].Address})";
            ws.Cells[rF, c2+2].Formula = $"=SUM({ws.Cells[rH+1, c2+2].Address}:{ws.Cells[rF-1, c2+2].Address})";
            ws.Cells[rF, c2+3].Formula = $"=SUM({ws.Cells[rH+1, c2+3].Address}:{ws.Cells[rF-1, c2+3].Address})";
            ws.Cells[rF, c2+4].Formula = $"=SUM({ws.Cells[rH+1, c2+4].Address}:{ws.Cells[rF-1, c2+4].Address})";
            ws.Cells[rF, c2+5].Formula = $"=SUM({ws.Cells[rH+1, c2+5].Address}:{ws.Cells[rF-1, c2+5].Address})";
            ws.Cells[rF, c8].Formula = $"=SUM({ws.Cells[rH+1, c8].Address}:{ws.Cells[rF-1, c8].Address})";
            ws.Cells[rF, c8+1].Formula = $"=SUM({ws.Cells[rH+1, c8+1].Address}:{ws.Cells[rF-1, c8+1].Address})";
            ws.Cells[rF, c8+2].Value2 = "";
            ws.Cells[rF, c11].Value2 = "";
            ws.Cells[rF, c11+1].Value2 = "";
            ws.Cells[rF, c11+2].Formula = $"=SUM({ws.Cells[rH+1, c11+2].Address}:{ws.Cells[rF-1, c11+2].Address})";
            ws.Cells[rF, c11+3].Value2 = "";

            // top row and left column - bold font
            ws.Range[ws.Cells[rH, col], ws.Cells[rH, cEx-1]].Font.Bold = true;
            ws.Range[ws.Cells[rH, col], ws.Cells[rF, col]].Font.Bold = true;
            // center headers on the top row
            ws.Range[ws.Cells[rH, c2], ws.Cells[rH, cEx-1]].HorizontalAlignment = Excel.XlHAlign.xlHAlignCenter;
            // top/left corner - bigger font for shooter name
            ws.Range[ws.Cells[rH, col], ws.Cells[rH, col]].Font.Size++;

            // vertical borders
            DrawVerticalLine(ws, c2, rH, rF);
            DrawVerticalLine(ws, c8, rH, rF);
            DrawVerticalLine(ws, c11, rH, rF);
            DrawVerticalLine(ws, cEx, rH, rF);
            // horizontal borders
            DrawHorizontalLine(ws, rH, col, cEx - 1);
            DrawHorizontalLine(ws, rF - 1, col, cEx - 1);
            DrawHorizontalLine(ws, rF, col, cEx - 1);

            ws.Columns[col].AutoFit();

            // pie chart
            Excel.Range range1 = ws.Range[ws.Cells[rH, c2], ws.Cells[rH, c2 + 5]]; // headers
            Excel.Range range2 = ws.Range[ws.Cells[rF, c2], ws.Cells[rF, c2 + 5]]; // SUMs
            double cLeft = ws.Cells[row, col + 1].Left;
            double cWidth = ws.Cells[row, col + 1 + chartWidth].Left - cLeft;
            double cTop = ws.Cells[rF + 2, 1].Top;
            double cHeight = ws.Cells[rF + 2 + chartHeight, 1].Top - cTop;
            Excel.ChartObject pieChartObject = charts.Add(cLeft, cTop, cWidth, cHeight);
            Excel.Chart pieChart = pieChartObject.Chart;
            pieChart.ChartType = Excel.XlChartType.xlPie;
            pieChart.SetSourceData(Application.Union(range1, range2), Type.Missing);
            pieChart.ApplyLayout(6);
            pieChart.ChartTitle.Delete();

            // percent/time/HF by stage graph
            int lastRow = rF + chartHeight + 3;
            if (stages.Count > 1 && drawGraph)
            {
                // percent by stage graph
                double gLeft = ws.Cells[row, col + 1].Left;
                double gWidth = ws.Cells[row, col + 1 + graphWidth].Left - gLeft;
                double gTop = ws.Cells[lastRow, col].Top;
                double gHeight = ws.Cells[lastRow + graphHeight, col].Top - gTop;
                Excel.ChartObject percentChartObject = charts.Add(gLeft, gTop, gWidth, gHeight);
                Excel.Chart percentChart = percentChartObject.Chart;
                percentChart.ChartType = Excel.XlChartType.xlLineMarkers;
                percentChart.HasTitle = true;
                percentChart.ChartTitle.Text = "Percent by Stage";
                Excel.Series line1 = percentChart.SeriesCollection().NewSeries();
                line1.Name = shooter.ShortName;
                line1.Values = ws.Range[ws.Cells[row + 1, c11 + 1], ws.Cells[rF - 1, c11 + 1]];
                lastRow += graphHeight + 1;

                // time by stage graph
                double gTop2 = ws.Cells[lastRow, col].Top;
                double gHeight2 = ws.Cells[lastRow + graphHeight, col].Top - gTop2;
                Excel.ChartObject timeChartObject = charts.Add(gLeft, gTop2, gWidth, gHeight2);
                Excel.Chart timeChart = timeChartObject.Chart;
                timeChart.ChartType = Excel.XlChartType.xlLineMarkers;
                timeChart.HasTitle = true;
                timeChart.ChartTitle.Text = "Time by Stage";
                Excel.Series line2 = timeChart.SeriesCollection().NewSeries();
                line2.Name = shooter.ShortName;
                line2.Values = ws.Range[ws.Cells[row + 1, c8 + 1], ws.Cells[rF - 1, c8 + 1]];
                lastRow += graphHeight + 1;

                // HF by stage graph
                double gTop3 = ws.Cells[lastRow, col].Top;
                double gHeight3 = ws.Cells[lastRow + graphHeight, col].Top - gTop3;
                Excel.ChartObject hfChartObject = charts.Add(gLeft, gTop3, gWidth, gHeight3);
                Excel.Chart hfChart = hfChartObject.Chart;
                hfChart.ChartType = Excel.XlChartType.xlLineMarkers;
                hfChart.HasTitle = true;
                hfChart.ChartTitle.Text = "HF by Stage";
                Excel.Series line3 = hfChart.SeriesCollection().NewSeries();
                line3.Name = shooter.ShortName;
                line3.Values = ws.Range[ws.Cells[row + 1, c8 + 2], ws.Cells[rF - 1, c8 + 2]];
                lastRow += graphHeight + 1;
            }

            return lastRow;
        }

        private void DrawHorizontalLine(Excel.Worksheet ws, int row, int colStart, int colEnd)
        {
            Excel.Border border = border = ws.Range[ws.Cells[row, colStart], ws.Cells[row, colEnd]].Borders[Excel.XlBordersIndex.xlEdgeBottom];
            border.LineStyle = Excel.XlLineStyle.xlContinuous;
        }

        private void DrawVerticalLine(Excel.Worksheet ws, int colNext, int rowStart, int rowEnd)
        {
            int col = colNext - 1;
            if (col < 1) return;

            Excel.Border border = ws.Range[ws.Cells[rowStart, col], ws.Cells[rowEnd, col]].Borders[Excel.XlBordersIndex.xlEdgeRight];
            border.LineStyle = Excel.XlLineStyle.xlContinuous;
        }

        #endregion

        #region Custom XML Part

        public void SaveDataAsXmlPart()
        {
            var xmlSerializer = new XmlSerializer(typeof(Performance));
            var sb = new StringBuilder();
            using (TextWriter tw = new StringWriter(sb))
            {
                xmlSerializer.Serialize(tw, StandbyData);
            }

            Application.ActiveWorkbook.SetCustomXmlPart(XmlPartName, sb.ToString());
            Application.ActiveWorkbook.Saved = false;
        }

        public void DeleteXmlPartData()
        {
            var xmlPart = Application.ActiveWorkbook.GetCustomXmlPart(XmlPartName);
            if (xmlPart != null)
            {
                xmlPart.Delete();
                Application.ActiveWorkbook.SetCustomXmlPartProperty(XmlPartName, "");
                Application.ActiveWorkbook.Saved = false;
            }
        }

        #endregion

        public async Task ShowAboutWindow()
        {
            var vm = new AboutViewModel();
            await StaTask.RunStaTask(() =>
            {
                new AboutWindow(vm).ShowDialog();
            });
        }

        public async Task Test()
        {
            try
            {
                await StaTask.RunStaTask(() =>
                {
                    Toast.ShowToast("Load matches", "Loading list of matches ...", true, true);
                });
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }
    }
}
