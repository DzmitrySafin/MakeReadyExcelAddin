using NLog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;
using Office = Microsoft.Office.Core;

namespace MakeReadyExcel.Helpers
{
    internal static class WorkbookHelper
    {
        private static readonly Logger logger = LogManager.GetCurrentClassLogger();

        public static string GetCustomXmlPartProperty(this Excel.Workbook wb, string propertyName)
        {
            try
            {
                foreach (var prop in wb.CustomDocumentProperties)
                {
                    if (prop.Name == propertyName) return prop.Value;
                }
                return string.Empty;

                //var customDocumentProperties = (Office.DocumentProperties)wb.CustomDocumentProperties;
                //var property = customDocumentProperties.Cast<Office.DocumentProperty>().FirstOrDefault(p => p.Name == propertyName);
                //return property == null ? string.Empty : property.Value.ToString();
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return string.Empty;
            }
        }

        public static void SetCustomXmlPartProperty(this Excel.Workbook wb, string propertyName, string id)
        {
            try
            {
                foreach (var prop in wb.CustomDocumentProperties)
                {
                    if (prop.Name == propertyName)
                    {
                        prop.Value = id;
                        return;
                    }
                }
                var customDocumentProperties = wb.CustomDocumentProperties;

                //var customDocumentProperties = (Office.DocumentProperties)wb.CustomDocumentProperties;
                //var property = customDocumentProperties.Cast<Office.DocumentProperty>().FirstOrDefault(p => p.Name == propertyName);
                //if (property != null)
                //{
                //    property.Value = id;
                //    return;
                //}

                var type = customDocumentProperties.GetType();
                object msoPropertyTypeString = Office.MsoDocProperties.msoPropertyTypeString;

                var args = new[] { propertyName, false, msoPropertyTypeString, id };
                type.InvokeMember("Add", BindingFlags.InvokeMethod, null, customDocumentProperties, args);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }

        public static Office.CustomXMLPart GetCustomXmlPart(this Excel.Workbook wb, string propertyName)
        {
            string id = wb.GetCustomXmlPartProperty(propertyName);
            if (string.IsNullOrEmpty(id)) return null;

            try
            {
                var customXmlParts = wb.CustomXMLParts;
                return customXmlParts.Cast<Office.CustomXMLPart>().FirstOrDefault(cxp => cxp.Id == id);
            }
            catch (Exception ex)
            {
                logger.Error(ex);
                return null;
            }
        }

        public static void SetCustomXmlPart(this Excel.Workbook wb, string propertyName, string xml)
        {
            var id = wb.GetCustomXmlPartProperty(propertyName);
            if (!string.IsNullOrEmpty(id))
            {
                var part = wb.CustomXMLParts.SelectByID(id);
                if (part != null) part.Delete();
            }

            var xmlpart = wb.CustomXMLParts.Add(xml);
            wb.SetCustomXmlPartProperty(propertyName, xmlpart.Id);
        }
    }
}
