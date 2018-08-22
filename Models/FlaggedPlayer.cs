using System;
using System.Collections.Generic;

namespace ClashAnalyzer.Models
{
    public class FlaggedPlayer
    {
        public string Name { get; set; }
        public List<string> Reasons { get; set; }
    }
}
