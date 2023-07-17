using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MakeReadyGeneral.Models
{
    public enum PowerFactor
    {
        [XmlEnum]
        Unknown = 0,

        [XmlEnum]
        Minor = 1,

        [XmlEnum]
        Major = 2
    }
}
