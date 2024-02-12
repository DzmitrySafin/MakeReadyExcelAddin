using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MakeReadyGeneral.Models
{
    public class Shooter
    {
        // {"id":7,"title":"Safin Dmitriy (Revolver/Min/Regular)","div":"5"}
        private static Regex factorRegex = new Regex(@"(?<name>[\w\s\.]+?)\s*\((?<division>[\w\s]+)/(?<factor>\w+)(/(?<category>\w+))?\)", RegexOptions.IgnoreCase);

        #region MakeReady properties

        [XmlAttribute]
        [JsonProperty("id")]
        public int Id { get; set; }

        [XmlAttribute]
        [JsonProperty("title")]
        public string Name { get; set; }

        [XmlAttribute]
        [JsonProperty("div")]
        public int DivisionCode { get; set; }

        #endregion

        #region Additional properties

        [XmlElement]
        public string ShortName { get; set; }

        [XmlElement]
        public PowerFactor Factor { get; set; }

        [XmlArray]
        public List<StageResult> CompetitionResults { get; set; }

        #endregion

        #region Calculated properties

        [XmlIgnore]
        public int TotalPoints { get; set; }

        [XmlIgnore]
        public double TotalTime { get; set; }

        #endregion

        public Shooter()
        {
            // parameterless constructor for XML (de)serialization
        }

        public Shooter(int id, string name)
        {
            Id = id;
            Name = name;
        }

        public void SetAdditionalProperties()
        {
            string factor = "";
            var match = factorRegex.Match(Name);
            if (match.Success)
            {
                ShortName = match.Groups["name"].Value;
                factor = match.Groups["factor"].Value;
                //string category = match.Groups["category"].Value;
            }

            if (factor.IndexOf("min", StringComparison.OrdinalIgnoreCase) >= 0) Factor = PowerFactor.Minor;
            else if (factor.IndexOf("maj", StringComparison.OrdinalIgnoreCase) >= 0) Factor = PowerFactor.Major;
            else
            {
                Factor = PowerFactor.Unknown;
                //TODO: log warning
            }
        }

        public void CalculateStatistics()
        {
            foreach (StageResult stage in CompetitionResults)
            {
                int d = Factor == PowerFactor.Major ? 1 : 0;
                int points = stage.AlphaCount * 5 + (stage.CharlieCount + d) * 3 + (stage.DeltaCount + d) * 1 - (stage.MissCount + stage.NoshootCount + stage.PenaltyCount) * 10;
                stage.Score = points < 0 ? 0 : points;
                stage.HitFactor = stage.TimeTaken > 0 ? stage.Score / stage.TimeTaken : 0;
            }

            TotalPoints = CompetitionResults.Sum(x => x.Score);
            TotalTime = CompetitionResults.Sum(x => x.TimeTaken);
        }
    }
}
