namespace gaseous_server.Models
{
    public class StatisticsModel
    {
        public Guid SessionId { get; set; } = Guid.Empty;
        public long GameId { get; set; }
        public DateTime SessionStart { get; set; }
        public int SessionLength { get; set; }
        public DateTime SessionEnd
        {
            get
            {
                return SessionStart.AddMinutes(SessionLength);
            }
        }
    }
}