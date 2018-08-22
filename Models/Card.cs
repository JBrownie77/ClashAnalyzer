using System;

namespace ClashAnalyzer.Models
{
    public class Card
    {
        public string Name { get; set; }
        public int Level { get; set; }
        public int MaxLevel { get; set; }
        public int Count { get; set; }
        public IconUrls IconUrls { get; set; }
    }
}
