using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SWP301BETASK.Features.Admin.Configuration;
using SWP301BETASK.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace SWP301BETASK.Features.Admin.Services;

public interface IJwtService
{
    (string Token, DateTime ExpiresAt) Create(Account account);
}

public sealed class JwtService(IOptions<JwtOptions> options) : IJwtService
{
    private readonly JwtOptions _o = options.Value;

    public (string Token, DateTime ExpiresAt) Create(Account account)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_o.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var expires = DateTime.UtcNow.AddMinutes(_o.ExpiryMinutes);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, account.Id.ToString()),
            new Claim(ClaimTypes.Name, account.Username),
            new Claim(ClaimTypes.Email, account.Email),
            new Claim(ClaimTypes.Role, account.Role.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _o.Issuer,
            audience: _o.Audience,
            claims: claims,
            notBefore: DateTime.UtcNow,
            expires: expires,
            signingCredentials: creds);

        return (new JwtSecurityTokenHandler().WriteToken(token), expires);
    }
}
