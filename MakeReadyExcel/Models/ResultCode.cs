using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MakeReadyExcel.Models
{
    internal enum ResultCode
    {
        Success = 0,
        Refused = 1, // user cancelled login
        LoginRequired = 2,
        ConnectionFailure = 3, // response status != 200 or exception
        RequestFailure = 4 // could not find what was expected in response
    }
}
