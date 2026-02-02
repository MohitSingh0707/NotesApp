using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using NotesApp.Application.Interfaces.Auth;
using NotesApp.Domain.Entities;

namespace NotesApp.Infrastructure.Auth;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly JwtOptions _options;

    public JwtTokenGenerator(JwtOptions options)
    {
        _options = options;
    }

    public string GenerateToken(User user)
    {
        var claims = new List<Claim>
    {
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
        new Claim("isGuest", user.IsGuest.ToString())
    };

        if (!string.IsNullOrWhiteSpace(user.Email))
            claims.Add(new Claim(ClaimTypes.Email, user.Email));

        if (!string.IsNullOrWhiteSpace(user.UserName))
            claims.Add(new Claim("username", user.UserName));

        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_options.Key)
        );

        var creds = new SigningCredentials(
            key,
            SecurityAlgorithms.HmacSha256
        );

        // 🔥 NO EXPIRY TOKEN
        var token = new JwtSecurityToken(
            issuer: _options.Issuer,
            audience: _options.Audience,
            claims: claims,
            signingCredentials: creds
        // ❌ no expires
        // ❌ no notBefore
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

}
