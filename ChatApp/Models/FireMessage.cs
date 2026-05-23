namespace ChatApp.Models
{
    public class FireMessage
    {
        public required string Id { get; set; }
        public required string Sender { get; set; }
        public required string Text { get; set; }
        public bool IsClient { get; set; }
        public bool ShowWebView { get; set; }
        public bool ShowImageView { get; set; }
        public required DateTime SentDate { get; set; }
    }
}
