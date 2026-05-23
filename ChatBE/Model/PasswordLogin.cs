


namespace ChatBE.Model
{
    public class PasswordLogin
    {
        public required string Username { get; set; }
    }


    public class PasswordLoginRegister
    {
        public required string Username { get; set; }
        public required string DeviceId { get; set; }
        public required string HashedPW { get; set; }
    }
}
