using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace ERPAccounting.API.Controllers;

/// <summary>
/// Authentication controller for JWT token generation
/// </summary>
[ApiController]
[Route("api/v1/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IConfiguration configuration, ILogger<AuthController> logger)
    {
        _configuration = configuration;
        _logger = logger;
    }

    /// <summary>
    /// Generate test JWT token for development and testing
    /// </summary>
    /// <remarks>
    /// ⚠️ **DEVELOPMENT ONLY** - Remove this endpoint before deploying to production!
    /// 
    /// This endpoint generates a JWT token with admin privileges for testing purposes.
    /// In production, use proper authentication flow with username/password or OAuth.
    /// 
    /// **Usage:**
    /// 1. Call this endpoint to get a token
    /// 2. Copy the token value
    /// 3. Add to Frontend .env.local as `VITE_JWT_TOKEN=your-token-here`
    /// 4. Token is valid for 24 hours
    /// 
    /// **Sample Response:**
    /// ```json
    /// {
    ///   "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
    ///   "expiresAt": "2025-12-03T20:00:00Z",
    ///   "username": "test_user",
    ///   "roles": ["Admin"],
    ///   "instructions": "Copy this token to Frontend .env.local as VITE_JWT_TOKEN"
    /// }
    /// ```
    /// </remarks>
    /// <response code="200">JWT token successfully generated</response>
    /// <response code="500">JWT configuration is missing or invalid</response>
    [HttpGet("test-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TestTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public IActionResult GenerateTestToken()
    {
        try
        {
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];
            var jwtSigningKey = _configuration["Jwt:SigningKey"];

            if (string.IsNullOrEmpty(jwtSigningKey))
            {
                _logger.LogError("JWT SigningKey is not configured");
                return StatusCode(500, new
                {
                    error = "JWT configuration is missing",
                    message = "JWT:SigningKey not found in appsettings.json"
                });
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSigningKey));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var username = "test_user";
            var email = "test@example.com";
            var role = "Admin";

            var claims = new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Role, role),
                new Claim(JwtRegisteredClaimNames.Sub, username),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64),
            };

            var expiresAt = DateTime.UtcNow.AddHours(24);

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: credentials
            );

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            _logger.LogInformation("Test JWT token generated for user: {Username}, expires at: {ExpiresAt}",
                username, expiresAt);

            return Ok(new TestTokenResponse
            {
                Token = tokenString,
                ExpiresAt = expiresAt,
                Username = username,
                Email = email,
                Roles = new[] { role },
                Instructions = "Copy this token to Frontend .env.local as VITE_JWT_TOKEN",
                Warning = "⚠️ This is a TEST endpoint. Remove before production deployment!"
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating test JWT token");
            return StatusCode(500, new
            {
                error = "Failed to generate token",
                message = ex.Message
            });
        }
    }

    /// <summary>
    /// Validate if provided JWT token is valid
    /// </summary>
    /// <param name="token">JWT token to validate</param>
    /// <response code="200">Token is valid</response>
    /// <response code="400">Token is invalid or expired</response>
    [HttpPost("validate-token")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(TokenValidationResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public IActionResult ValidateToken([FromBody] TokenValidationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return BadRequest(new { error = "Token is required" });
        }

        try
        {
            var jwtIssuer = _configuration["Jwt:Issuer"];
            var jwtAudience = _configuration["Jwt:Audience"];
            var jwtSigningKey = _configuration["Jwt:SigningKey"];

            if (string.IsNullOrEmpty(jwtSigningKey))
            {
                return StatusCode(500, new { error = "JWT configuration is missing" });
            }

            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.UTF8.GetBytes(jwtSigningKey);

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtIssuer,
                ValidAudience = jwtAudience,
                IssuerSigningKey = new SymmetricSecurityKey(key),
                ClockSkew = TimeSpan.Zero
            };

            var principal = tokenHandler.ValidateToken(request.Token, validationParameters, out var validatedToken);

            var jwtToken = validatedToken as JwtSecurityToken;
            var username = principal.FindFirst(ClaimTypes.Name)?.Value;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value;
            var roles = principal.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray();

            return Ok(new TokenValidationResult
            {
                IsValid = true,
                Username = username,
                Email = email,
                Roles = roles,
                ExpiresAt = jwtToken?.ValidTo,
                Message = "Token is valid"
            });
        }
        catch (SecurityTokenExpiredException)
        {
            return BadRequest(new TokenValidationResult
            {
                IsValid = false,
                Message = "Token has expired"
            });
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Token validation failed");
            return BadRequest(new TokenValidationResult
            {
                IsValid = false,
                Message = $"Token is invalid: {ex.Message}"
            });
        }
    }
}

/// <summary>
/// Test token response
/// </summary>
public class TestTokenResponse
{
    /// <summary>
    /// JWT token string
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Token expiration date (UTC)
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Username associated with the token
    /// </summary>
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email associated with the token
    /// </summary>
    public string Email { get; set; } = string.Empty;

    /// <summary>
    /// Roles assigned to the token
    /// </summary>
    public string[] Roles { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Instructions for using the token
    /// </summary>
    public string Instructions { get; set; } = string.Empty;

    /// <summary>
    /// Warning message
    /// </summary>
    public string Warning { get; set; } = string.Empty;
}

/// <summary>
/// Token validation request
/// </summary>
public class TokenValidationRequest
{
    /// <summary>
    /// JWT token to validate
    /// </summary>
    public string Token { get; set; } = string.Empty;
}

/// <summary>
/// Token validation result
/// </summary>
public class TokenValidationResult
{
    /// <summary>
    /// Whether the token is valid
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Username from token claims
    /// </summary>
    public string? Username { get; set; }

    /// <summary>
    /// Email from token claims
    /// </summary>
    public string? Email { get; set; }

    /// <summary>
    /// Roles from token claims
    /// </summary>
    public string[]? Roles { get; set; }

    /// <summary>
    /// Token expiration date
    /// </summary>
    public DateTime? ExpiresAt { get; set; }

    /// <summary>
    /// Validation message
    /// </summary>
    public string Message { get; set; } = string.Empty;
}
