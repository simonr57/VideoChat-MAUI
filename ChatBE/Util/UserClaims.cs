using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using Azure.Core;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

public class UserClaimsCustomAuthOptions : AuthenticationSchemeOptions
{
    public string Realm = "UserClaimsCustomAuth";
}

public class UserClaimsCustomAuthHandler : AuthenticationHandler<UserClaimsCustomAuthOptions>
{
    private IConfiguration _config;
    private const string AuthorizationHeaderName = "Authorization";
    private const string BasicSchemeName = "UserClaims";
    private readonly IHttpContextAccessor _httpContextAccessor;

    public UserClaimsCustomAuthHandler(
        IOptionsMonitor<UserClaimsCustomAuthOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        IConfiguration config,
        IHttpContextAccessor httpContextAccessor
    )
        : base(options, logger, encoder)
    {
        _config = config;
        _httpContextAccessor = httpContextAccessor;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.ContainsKey(AuthorizationHeaderName))
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        if (
            !AuthenticationHeaderValue.TryParse(
                Request.Headers[AuthorizationHeaderName],
                out var headerValue
            )
        )
        {
            return Task.FromResult(AuthenticateResult.NoResult());
        }

        var identityJWT =
            _httpContextAccessor?.HttpContext?.Request.Headers.Authorization.ToString();
        var claimFromJwt = SuccessfulLoginJwt(identityJWT);

        if (claimFromJwt != null)
        {
            var jwtidentity = new ClaimsIdentity(claimFromJwt, Scheme.Name);
            var jwtprincipal = new ClaimsPrincipal(jwtidentity);
            var jwtticket = new AuthenticationTicket(jwtprincipal, Scheme.Name);
            return Task.FromResult(AuthenticateResult.Success(jwtticket));
        }
        else
        {
            return Task.FromResult(AuthenticateResult.Fail("Something went wrong"));
        }
    }

    private Claim[]? SuccessfulLoginJwt(string? identityJWT)
    {
        if (string.IsNullOrEmpty(identityJWT))
        {
            return null;
        }

        string token = identityJWT.Contains("Bearer") ? identityJWT[7..] : "";

        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        if (!ValidateJwtToken(token))
        {
            return null;
        }

        var handler = new JwtSecurityTokenHandler();
        var decodedValue = handler.ReadJwtToken(token);
        var jwtclaims = decodedValue.Claims.ToArray();
        return jwtclaims;
    }

    private bool ValidateJwtToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_config["JwtToken:MaSecurtKey"] ?? "")
            );
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
