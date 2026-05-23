namespace ChatBE.Model
{
    public class LoggedInResponse
    {
        public required string username { get; set; }
        public required string jwttoken { get; set; }
        public required DateTime expire { get; set; }
    }
}
