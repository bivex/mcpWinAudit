namespace mcpWinAuditServer.Models
{
    public class StandingsResponse
    {
        public int Rank { get; set; }
        public string TeamName { get; set; }
        public int Played { get; set; }
        public int Wins { get; set; }
        public int Draws { get; set; }
        public int Losses { get; set; }
        public int For { get; set; }
        public int Against { get; set; }
        public int Points { get; set; }
        public int GoalsFor { get; set; }
        public int GoalsAgainst { get; set; }
        public int BehindsFor { get; set; }
        public int BehindsAgainst { get; set; }
        public double Percentage { get; set; }
    }
} 