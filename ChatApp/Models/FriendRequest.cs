namespace ChatApp.Models
{
    public class FriendRequest
    {
        public required string SenderUserId { get; set; }
        public required string ReceiverUserId { get; set; }
        public required string Status { get; set; }
        public DateTime SentAt { get; set; }
    }
}
