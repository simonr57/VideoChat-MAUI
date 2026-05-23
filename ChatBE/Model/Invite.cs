namespace ChatBE.Model
{
    public class Invite
    {
        public DateTime SentAt { get; set; }
        public required string DeviceId { get; set; }
    }
}
