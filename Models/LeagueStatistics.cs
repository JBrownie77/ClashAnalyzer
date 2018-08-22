using System;

namespace ClashAnalyzer.Models
{
    public class LeagueStatistics
    {
        public SeasonStatistics CurrentSeason { get; set; }
        public SeasonStatistics PreviousSeason { get; set; }
        public SeasonStatistics BestSeason { get; set; }
    }
}
