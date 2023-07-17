using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MakeReadyGeneral.Models
{
    public class Stage
    {
        [XmlAttribute]
        public int StageNumber { get; set; }

        [XmlAttribute]
        public string StageName { get; set; }

        [XmlElement]
        public int PaperNumber { get; set; }
        [XmlElement]
        public int PopperNumber { get; set; }
        [XmlElement]
        public int PlateNumber { get; set; }
        [XmlElement]
        public int DisappearNumber { get; set; }
        [XmlElement]
        public int PenaltyNumber { get; set; }

        [XmlElement]
        public int MaxPoints { get; set; }
    }
}
