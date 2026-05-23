using System.IdentityModel.Tokens.Jwt;
using System.Reflection;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ChatBE.Util
{
    public class GenerateJWT : IGenerateJWT
    {
        private IConfiguration _config;

        public GenerateJWT(IConfiguration config)
        {
            _config = config;
        }

        public (string, DateTime) GenerateJSONWebToken(string getFriends, string Username)
        {
            return Generate(getFriends, Username);
        }

        private (string, DateTime) Generate(string getFriends, string Username)
        {
            var seckey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["JwtToken:MaSecurtKey"] ?? "")
            );

            var credentials = new SigningCredentials(seckey, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim("Friends", getFriends),
                new Claim("Username", Username),
            };

            var token = new JwtSecurityToken(
                issuer: _config["JwtToken:Issuer"],
                audience: _config["JwtToken:Issuer"],
                claims,
                expires: DateTime.UtcNow.AddDays(20),
                signingCredentials: credentials
            );

            var encodetoken = new JwtSecurityTokenHandler().WriteToken(token);
            return (encodetoken, token.ValidTo.ToUniversalTime());
        }
    }
}
