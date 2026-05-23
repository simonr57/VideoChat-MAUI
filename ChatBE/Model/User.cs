namespace ChatBE.Model
{
    public class User
    {
        public string HashedPW { get; set; }
        public string DeviceId { get; set; }
        public string base64 { get; set; }
        public string base64sm { get; set; }
        public DateTime Created { get; set; }

    }
}
