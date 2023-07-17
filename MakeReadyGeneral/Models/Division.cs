using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MakeReadyGeneral.Models
{
    public class Division
    {
        #region MakeReady properties

        [XmlAttribute]
        public int Id { get; set; }

        [XmlAttribute]
        public string Title { get; set; }

        #endregion

        #region Additional properties

        [XmlAttribute]
        public int ShootersCount { get; set; }

        public List<Shooter> Shooters { get; set; }

        #endregion

        #region Calculated properties

        [XmlIgnore]
        public List<DivisionStage> Stages { get; set; }

        #endregion

        public Division()
        {
            // parameterless constructor for XML (de)serialization
        }

        public Division(int id, string title)
        {
            Id = id;
            Title = title;
        }

        public void CalculateStatistics(List<Stage> matchStages)
        {
            foreach (var shooter in Shooters)
            {
                shooter.CalculateStatistics();
            }

            Stages = new List<DivisionStage>();
            foreach (var stage in matchStages)
            {
                Stages.Add(new DivisionStage
                {
                    StageNumber = stage.StageNumber,
                    MaxPointsShooter = Shooters.Aggregate((sh1, sh2) => sh1.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).Score > sh2.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).Score ? sh1 : sh2),
                    MinPointsShooter = Shooters.Aggregate((sh1, sh2) => sh1.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).Score > sh2.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).Score ? sh2 : sh1),
                    AveragePoints = Shooters.Average(sh => sh.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).Score),
                    MaxTimeShooter = Shooters.Aggregate((sh1, sh2) => sh1.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).TimeTaken > sh2.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).TimeTaken ? sh1 : sh2),
                    MinTimeShooter = Shooters.Aggregate((sh1, sh2) => sh1.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).TimeTaken > sh2.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).TimeTaken ? sh2 : sh1),
                    AverageTime = Shooters.Average(sh => sh.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).TimeTaken),
                    MaxHitFactorShooter = Shooters.Aggregate((sh1, sh2) => sh1.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).HitFactor > sh2.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).HitFactor ? sh1 : sh2),
                    MinHitFactorShooter = Shooters.Aggregate((sh1, sh2) => sh1.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).HitFactor > sh2.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).HitFactor ? sh2 : sh1),
                    AverageHitFactor = Shooters.Average(sh => sh.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).HitFactor)
                });

                int place = 1;
                foreach (var shooter in Shooters.OrderByDescending(sh => sh.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).HitFactor))
                {
                    shooter.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).Place = place++;
                    double bestHitFactor = Stages.First(sr => sr.StageNumber == stage.StageNumber).MaxHitFactorShooter.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).HitFactor;
                    shooter.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).RelativeHitFactor = shooter.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).HitFactor / bestHitFactor;
                    shooter.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).RelativePoints = stage.MaxPoints * shooter.CompetitionResults.First(sr => sr.StageNumber == stage.StageNumber).RelativeHitFactor / 100;
                }
            }
        }

        public static List<Division> CreateDefaultList()
        {
            return new List<Division>
            {
                new Division(0, "Combined"),
                new Division(1, "Open"),
                new Division(2, "Standard"),
                new Division(3, "Modified"),
                new Division(4, "Production"),
                new Division(5, "Revolver"),
                new Division(6, "Open Semi-Auto"),
                //new Division(7, ""),
                new Division(8, "Standard Semi-Auto"),
                //new Division(9, ""),
                new Division(10, "Open"),
                new Division(11, "Standard"),
                new Division(12, "Standard Manual"),
                new Division(18, "Classic"),
                new Division(20, "Open"),
                new Division(21, "Standard"),
                new Division(22, "Production"),
                new Division(24, "Production Optics"),
                new Division(27, "Pistol Caliber"),
                new Division(28, "Production Optics L."),
                new Division(29, "PC Optics"),
                new Division(31, "PC Iron"),
                new Division(71, "AA Rifle Open"),
            };
        }
    }
}
