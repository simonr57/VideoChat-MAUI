namespace ChatBE.Util
{
    public interface IGenerateJWT
    {
        (string, DateTime) GenerateJSONWebToken(string getFriends, string Username );
    }
}

