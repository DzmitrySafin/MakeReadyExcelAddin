using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MakeReadyGeneral.Models
{
    public class StageResult
    {
        #region MakeReady properties

        public int AlphaCount { get; set; }
        public int CharlieCount { get; set; }
        public int DeltaCount { get; set; }
        public int MissCount { get; set; }
        public int NoshootCount { get; set; }
        public int PenaltyCount { get; set; }
        public double TimeTaken { get; set; }

        #endregion

        #region Additional properties

        [XmlAttribute]
        public int StageNumber { get; set; }

        #endregion

        #region Calculated properties

        [XmlIgnore]
        public int Score { get; set; }

        [XmlIgnore]
        public double HitFactor { get; set; }

        [XmlIgnore]
        public int Place { get; set; }

        [XmlIgnore]
        public double RelativeHitFactor { get; set; }

        [XmlIgnore]
        public double RelativePoints { get; set; }

        #endregion

        public static List<StageResult> CreateDefaultList(int count)
        {
            var list = new List<StageResult>();
            for (int i = 0; i < count; i++)
            {
                list.Add(new StageResult { StageNumber = i + 1 });
            }
            return list;
        }
    }
}
