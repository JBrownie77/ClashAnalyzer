using System;

namespace ClashAnalyzer.Models
{
    public class PlayerWarStats
    {
        public int WarsParticipated { get; set; }
        public int WarDayBattles { get; set; }
        public int WarDayWins { get; set; }
        public int CardsEarned { get; set; }
        public int LossStreak { get; set; }
        public int LongestLossStreak { get; set; }
    }
}
