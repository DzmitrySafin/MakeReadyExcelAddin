using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MakeReadyGeneral.Models
{
    public class Competition
    {
        #region MakeReady properties

        [XmlAttribute]
        public string Id { get; set; }

        [XmlAttribute(DataType="date")]
        public DateTime EventDate { get; set; }

        [XmlAttribute]
        public string CountryCode { get; set; }

        [XmlAttribute]
        public string Title { get; set; }

        #endregion

        #region Additional properties

        [XmlAttribute]
        public int StagesCount { get; set; }

        [XmlAttribute]
        public int TotalPoints { get; set; }

        [XmlAttribute]
        public int ShootersCount { get; set; }

        [XmlAttribute]
        public bool IsCompleted { get; set; }

        [XmlArray]
        public List<Division> Divisions { get; set; }

        [XmlArray]
        public List<Stage> Stages { get; set; }

        //[XmlIgnore] // [XmlElement]
        //public Country Country { get; set; }

        #endregion

        public Competition()
        {
            // parameterless constructor for XML (de)serialization
        }

        public Competition(string id, DateTime dt, string country, string title)
        {
            Id = id;
            EventDate = dt;
            CountryCode = country;
            Title = title;
        }

        public void CalculateStatistics()
        {
            foreach (var division in Divisions)
            {
                division.CalculateStatistics(Stages);
            }
        }
    }
}
