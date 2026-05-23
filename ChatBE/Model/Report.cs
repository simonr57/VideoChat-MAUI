namespace ChatBE.Model
{
    public class Report
    {
        public required string Reporter { get; set; }
        public required string Reporting { get; set; }
        public required DateTime WhenAt { get; set; }
        public required string Issue { get; set; }
    }
}
