using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MakeReadyGeneral.Models
{
    public class DivisionStage
    {
        public int StageNumber { get; set; }

        public Shooter MaxPointsShooter { get; set; }
        public Shooter MinPointsShooter { get; set; }
        public int MaxPoints => MaxPointsShooter.CompetitionResults[StageNumber - 1].Score;
        public int MinPoints => MinPointsShooter.CompetitionResults[StageNumber - 1].Score;
        public double AveragePoints { get; set; }

        public Shooter MaxTimeShooter { get; set; }
        public Shooter MinTimeShooter { get; set; }
        public double MaxTime => MaxTimeShooter.CompetitionResults[StageNumber - 1].TimeTaken;
        public double MinTime => MinTimeShooter.CompetitionResults[StageNumber - 1].TimeTaken;
        public double AverageTime { get; set; }

        public Shooter MaxHitFactorShooter { get; set; }
        public Shooter MinHitFactorShooter { get; set; }
        public double MaxHitFactor => MaxHitFactorShooter.CompetitionResults[StageNumber - 1].HitFactor;
        public double MinHitFactor => MinHitFactorShooter.CompetitionResults[StageNumber - 1].HitFactor;
        public double AverageHitFactor { get; set; }
    }
}
