using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using LegacyVault.Features.IdentityAccess.Contracts;
using LegacyVault.Models;
using Microsoft.IdentityModel.Tokens;

namespace LegacyVault.Features.IdentityAccess.Services
{
    public class JwtTokenService : IJwtTokenService
    {
        private readonly IConfiguration _configuration;

        public JwtTokenService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public string GenerateToken(
            Account account,
            bool rememberMe)
        {
            var jwtKey =
                _configuration["Jwt:Key"];

            var issuer =
                _configuration["Jwt:Issuer"];

            var audience =
                _configuration["Jwt:Audience"];

            if (string.IsNullOrWhiteSpace(jwtKey))
            {
                throw new InvalidOperationException(
                    "JWT Key is not configured.");
            }

            // Những thông tin được nhúng vào token
            var claims = new List<Claim>
            {
                new Claim(
                    JwtRegisteredClaimNames.Sub,
                    account.Id.ToString()),

                new Claim(
                    ClaimTypes.NameIdentifier,
                    account.Id.ToString()),

                new Claim(
                    ClaimTypes.Name,
                    account.FullName),

                new Claim(
                    ClaimTypes.Email,
                    account.Email),

                new Claim(
                    ClaimTypes.Role,
                    account.Role)
            };

            // Tạo khóa ký token
            var securityKey =
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtKey));

            var credentials =
                new SigningCredentials(
                    securityKey,
                    SecurityAlgorithms.HmacSha256);

            // RememberMe = true thì token sống lâu hơn
            var expires =
                rememberMe
                    ? DateTime.UtcNow.AddDays(30)
                    : DateTime.UtcNow.AddHours(2);

            var token =
                new JwtSecurityToken(
                    issuer: issuer,
                    audience: audience,
                    claims: claims,
                    expires: expires,
                    signingCredentials: credentials);

            return new JwtSecurityTokenHandler()
                .WriteToken(token);
        }
    }
}