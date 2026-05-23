using System.IdentityModel.Tokens.Jwt;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace ChatBE.Util
{
    public class JwtHelper
    {
        public static bool ValidateJwtToken(string token)
        {
            try
            {
                string jwtkey = ConfigurationHelper.GetConfigValue("JwtToken:MaSecurtKey");

                var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtkey));
                var tokenValidationParameters = new TokenValidationParameters
                {
                    ValidateLifetime = true,
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,

                    ClockSkew = TimeSpan.Zero, // No clock skew
                };

                var tokenHandler = new JwtSecurityTokenHandler();
                SecurityToken validatedToken;

                var principal = tokenHandler.ValidateToken(
                    token,
                    tokenValidationParameters,
                    out validatedToken
                );

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Token validation failed: {ex.Message}");
                return false;
            }
        }
    }
}
