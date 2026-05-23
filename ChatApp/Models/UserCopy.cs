namespace ChatApp.Models
{
    public class UserCopy
    {
        public required string DeviceId { get; set; }
        public string? base64 { get; set; }
        public string? base64sm { get; set; }
        public DateTime Created { get; set; }
        public ImageSource? ImageSource { get; set; }
    }
}
