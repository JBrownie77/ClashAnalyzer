using System.Collections.Generic;

namespace ClashAnalyzer.Models
{
    public class War
    {
        public int SeasonId { get; set; }
        public string CreatedDate { get; set; }
        public List<WarParticipant> Participants { get; set; }
    }
}
