using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;

namespace MakeReadyExcel.Helpers
{
    internal static class CursorHelper
    {
        public static CursorSwitcher SwitchCursor(this Excel.Application app, Excel.XlMousePointer pointer)
        {
            return new CursorSwitcher(app, pointer);
        }
    }

    internal class CursorSwitcher : IDisposable
    {
        private Excel.Application _application;
        private Excel.XlMousePointer _prevPointer;

        public CursorSwitcher(Excel.Application app, Excel.XlMousePointer newPointer)
        {
            _application = app;
            _prevPointer = _application.Cursor;
            _application.Cursor = newPointer;
        }

        public void Dispose()
        {
            _application.Cursor = _prevPointer;
        }
    }
}
