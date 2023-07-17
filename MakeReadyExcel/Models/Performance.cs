using MakeReadyGeneral.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MakeReadyExcel.Models
{
    public class Performance
    {
        [XmlAttribute]
        public DateTime TimestampLoaded { get; set; }

        public List<Competition> Competitions { get; set; } = new List<Competition>();

        [XmlIgnore]
        public bool IsCompetitionsLoaded => Competitions.Any();
    }
}
