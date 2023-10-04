using MakeReadyExcel.Helpers;
using MakeReadyExcel.Properties;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Office = Microsoft.Office.Core;

namespace MakeReadyExcel
{
    [ComVisible(true)]
    public class Ribbon : Office.IRibbonExtensibility
    {
        private Office.IRibbonUI ribbon;

        #region IRibbonExtensibility Members

        public string GetCustomUI(string ribbonID)
        {
            return GetResourceText("MakeReadyExcel.Ribbon.xml");
        }

        #endregion

        #region Ribbon Callbacks

        public void Ribbon_Load(Office.IRibbonUI ribbonUI)
        {
            ribbon = ribbonUI;
            Globals.ThisAddIn.RibbonEvent += ribbon.Invalidate;
        }

        #region group Account

        public string GrpGeneral_Label(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.StandbyConnector.IsLoggedIn ? Globals.ThisAddIn.StandbyConnector.UserName : "Account";
        }

        public bool BtnLogin_Enabled(Office.IRibbonControl control)
        {
            return !Globals.ThisAddIn.StandbyConnector.IsLoggedIn;
        }

        public stdole.IPictureDisp BtnLogin_Image(Office.IRibbonControl control)
        {
            return ImageConverter.Convert(Resources.Login);
        }

        public async Task BtnLogin_Action(Office.IRibbonControl control)
        {
            //await Globals.ThisAddIn.LoginAndLoad();
            if (await Globals.ThisAddIn.Login())
            {
                ribbon.Invalidate();
            }
        }

        public bool BtnLogout_Enabled(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.StandbyConnector.IsLoggedIn;
        }

        public stdole.IPictureDisp BtnLogout_Image(Office.IRibbonControl control)
        {
            return ImageConverter.Convert(Resources.Logout);
        }

        public async Task BtnLogout_Action(Office.IRibbonControl control)
        {
            if (await Globals.ThisAddIn.Logout())
            {
                ribbon.Invalidate();
            }
        }

        public stdole.IPictureDisp BtnAbout_Image(Office.IRibbonControl control)
        {
            return ImageConverter.Convert(Resources.About);
        }

        public async Task BtnAbout_Action(Office.IRibbonControl control)
        {
            await Globals.ThisAddIn.ShowAboutWindow();
        }

        #endregion

        #region group Matches

        public string GrpCompetition_Label(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.StandbyData.IsCompetitionsLoaded ? $"{Globals.ThisAddIn.StandbyData.Competitions.Count} matches" : "Matches";
        }

        public bool BtnRefresh_Enabled(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.StandbyConnector.IsLoggedIn;
        }

        public string BtnRefresh_Label(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.StandbyData.IsCompetitionsLoaded ? "Refresh matches" : "Load matches";
        }

        public stdole.IPictureDisp BtnRefresh_Image(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.StandbyData.IsCompetitionsLoaded ? ImageConverter.Convert(Resources.Refresh) : ImageConverter.Convert(Resources.Load);
        }

        public async Task BtnRefresh_Action(Office.IRibbonControl control)
        {
            if (await Globals.ThisAddIn.LoadCompetitions())
            {
                ribbon.Invalidate();
                if (await Globals.ThisAddIn.SelectMatchAndShooters())
                {
                    ribbon.Invalidate();
                }
            }
        }

        public bool BtnSelect_Enabled(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.StandbyData.IsCompetitionsLoaded;
        }

        public stdole.IPictureDisp BtnSelect_Image(Office.IRibbonControl control)
        {
            return ImageConverter.Convert(Resources.Select);
        }

        public async Task BtnSelect_Action(Office.IRibbonControl control)
        {
            if (await Globals.ThisAddIn.SelectMatchAndShooters())
            {
                ribbon.Invalidate();
            }
        }

        public bool BtnSave_Enabled(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.StandbyData.IsCompetitionsLoaded && !Globals.ThisAddIn.IsActiveWorkbookData();
        }

        public stdole.IPictureDisp BtnSave_Image(Office.IRibbonControl control)
        {
            return ImageConverter.Convert(Resources.Save);
        }

        public void BtnSave_Action(Office.IRibbonControl control)
        {
            Globals.ThisAddIn.SaveDataAsXmlPart();
            ribbon.Invalidate();
        }

        public bool BtnDelete_Enabled(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.StandbyData.IsCompetitionsLoaded && Globals.ThisAddIn.IsActiveWorkbookData();
        }

        public stdole.IPictureDisp BtnDelete_Image(Office.IRibbonControl control)
        {
            return ImageConverter.Convert(Resources.Delete);
        }

        public void BtnDelete_Action(Office.IRibbonControl control)
        {
            Globals.ThisAddIn.DeleteXmlPartData();
            ribbon.Invalidate();
        }

        #endregion

        #region group Division/Shooter

        public string GrpDivision_Label(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.SelectedCompetition != null ? $"{Globals.ThisAddIn.FilteredShooters.Count} shooters" : "Division/Shooter";
        }

        public string LblCompetition_Label(Office.IRibbonControl control)
        {
            string text = Globals.ThisAddIn.SelectedCompetition != null ? $"{Globals.ThisAddIn.SelectedCompetition.CountryCode}: {Globals.ThisAddIn.SelectedCompetition.Title}" : "                match not selected";
            if (text.Length > 55) text = text.Substring(0, 45) + " ...";
            return text;
        }

        public int DropDivisions_SelectedIndex(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.SelectedDivisionIndex;
        }

        public bool DropDivisions_Enabled(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.SelectedCompetition != null && Globals.ThisAddIn.SelectedCompetition.Divisions.Any();
        }

        public int DropDivisions_ItemCount(Office.IRibbonControl control)
        {
            return (Globals.ThisAddIn.SelectedCompetition?.Divisions.Count ?? -1) + 1;
        }

        public int DropDivisions_ItemId(Office.IRibbonControl control, int index)
        {
            return index == 0 ? -1 : Globals.ThisAddIn.SelectedCompetition.Divisions[index-1].Id;
        }

        public string DropDivisions_ItemLabel(Office.IRibbonControl control, int index)
        {
            return index == 0 ? "All shooters" : Globals.ThisAddIn.SelectedCompetition.Divisions[index-1].Title;
        }

        public void DropDivisions_Action(Office.IRibbonControl control, int selectedId, int selectedIndex)
        {
            Globals.ThisAddIn.DivisionItemSelected(selectedId, selectedIndex);
            ribbon.InvalidateControl("dropShooters");
            ribbon.InvalidateControl("grpDivision");
            ribbon.InvalidateControl("splitChart1");
        }

        public int DropShooters_SelectedIndex(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.SelectedShooterIndex;
        }

        public bool DropShooters_Enabled(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.FilteredShooters.Any();
        }

        public int DropShooters_ItemCount(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.FilteredShooters.Count;
        }

        public int DropShooters_ItemId(Office.IRibbonControl control, int index)
        {
            return Globals.ThisAddIn.FilteredShooters[index].Id;
        }

        public string DropShooters_ItemLabel(Office.IRibbonControl control, int index)
        {
            return Globals.ThisAddIn.FilteredShooters[index].Name;
        }

        public void DropShooters_Action(Office.IRibbonControl control, int selectedId, int selectedIndex)
        {
            Globals.ThisAddIn.ShooterItemSelected(selectedId, selectedIndex);
        }

        #endregion

        #region group Charts

        public bool SplitChart1_Enabled(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.SelectedCompetition != null && Globals.ThisAddIn.SelectedDivisionId > 0;
        }

        public void BtnChart1User_Action(Office.IRibbonControl control)
        {
            Globals.ThisAddIn.DivisionChartByShooter();
        }

        public void BtnChart1Stage_Action(Office.IRibbonControl control)
        {
            Globals.ThisAddIn.DivisionChartByStage();
        }

        public bool BtnChart2_Enabled(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.SelectedCompetition != null;
        }

        public void BtnChart2_Action(Office.IRibbonControl control)
        {
            Globals.ThisAddIn.Chart2SingleShooter();
        }

        public bool BtnChart3_Enabled(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.SelectedCompetition != null;
        }

        public void BtnChart3_Action(Office.IRibbonControl control)
        {
            Globals.ThisAddIn.Chart3SingleShooter();
        }

        public bool BtnChart4_Enabled(Office.IRibbonControl control)
        {
            return Globals.ThisAddIn.SelectedCompetition != null;
        }

        public void BtnChart4_Action(Office.IRibbonControl control)
        {
            Globals.ThisAddIn.Chart4Test();
        }

        #endregion

        #endregion

        #region Helpers

        private static string GetResourceText(string resourceName)
        {
            Assembly asm = Assembly.GetExecutingAssembly();
            string[] resourceNames = asm.GetManifestResourceNames();
            for (int i = 0; i < resourceNames.Length; ++i)
            {
                if (string.Compare(resourceName, resourceNames[i], StringComparison.OrdinalIgnoreCase) == 0)
                {
                    using (StreamReader resourceReader = new StreamReader(asm.GetManifestResourceStream(resourceNames[i])))
                    {
                        if (resourceReader != null)
                        {
                            return resourceReader.ReadToEnd();
                        }
                    }
                }
            }
            return null;
        }

        #endregion
    }
}
