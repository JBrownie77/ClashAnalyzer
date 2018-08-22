using System;

namespace ClashAnalyzer.Models
{
    public class War
    {
        public int SeasonId { get; set; }
        public string CreatedDate { get; set; }
        public WarParticipant Participants { get; set; }
    }
}
