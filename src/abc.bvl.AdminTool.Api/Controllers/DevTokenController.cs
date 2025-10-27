using abc.bvl.AdminTool.Api.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace abc.bvl.AdminTool.Api.Controllers;

/// <summary>
/// Development-only controller for generating JWT tokens for testing
/// ⚠️ SHOULD BE DISABLED IN PRODUCTION ⚠️
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class DevTokenController : ControllerBase
{
    private readonly JwtSettings _jwtSettings;
    private readonly IWebHostEnvironment _environment;

    public DevTokenController(IOptions<JwtSettings> jwtSettings, IWebHostEnvironment environment)
    {
        _jwtSettings = jwtSettings.Value;
        _environment = environment;
    }

    /// <summary>
    /// Generate a development JWT token for testing (Development environment only)
    /// </summary>
    /// <param name="userId">User ID (default: test.user)</param>
    /// <param name="email">User email (default: test.user@abc.bvl)</param>
    /// <param name="roles">Comma-separated roles (default: Admin,ScreenManager)</param>
    /// <returns>JWT token</returns>
    [HttpGet("generate")]
    [AllowAnonymous]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public IActionResult GenerateToken(
        [FromQuery] string userId = "test.user",
        [FromQuery] string email = "test.user@abc.bvl",
        [FromQuery] string roles = "Admin,ScreenManager")
    {
        // Only allow in Development environment
        if (!_environment.IsDevelopment())
        {
            return StatusCode(403, new { error = "This endpoint is only available in Development environment" });
        }

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId),
            new Claim(ClaimTypes.Name, userId),
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Sub, userId),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
        };

        // Add roles
        foreach (var role in roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwtSettings.ExpirationMinutes),
            signingCredentials: credentials
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Ok(new
        {
            token = tokenString,
            type = "Bearer",
            expiresIn = _jwtSettings.ExpirationMinutes * 60,
            userId = userId,
            email = email,
            roles = roles.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries),
            usage = $"Add this header to your requests: Authorization: Bearer {tokenString.Substring(0, 20)}..."
        });
    }
}
