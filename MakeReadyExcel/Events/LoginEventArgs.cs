using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MakeReadyExcel.Events
{
    internal class LoginEventArgs : EventArgs
    {
        public bool Success { get; set; }
        public string Message { get; set; }
    }

    internal delegate void LoginEventHandler(MakeReady sender, LoginEventArgs e);
}
