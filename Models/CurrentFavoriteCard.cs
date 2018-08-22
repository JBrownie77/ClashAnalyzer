using System;

namespace ClashAnalyzer.Models
{
    public class CurrentFavoriteCard
    {
        public string Name { get; set; }
        public int Id { get; set; }
        public int MaxLevel { get; set; }
        public IconUrls IconUrls { get; set; }
    }
}
