using System;
using System.Collections.Generic;

namespace ClashAnalyzer.Models
{
    public class RiverRaceClan
    {
        public string Tag { get; set; }
        public string FinishTime { get; set; }
        public List<RiverRaceParticipant> Participants { get; set; }
    }
}
