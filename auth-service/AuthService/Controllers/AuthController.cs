using AuthService.Data;
using AuthService.Models;
using AuthService.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace AuthService.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly AuthDbContext _db;
    private readonly JwtTokenService _jwt;
    private readonly PasswordHasher _hasher;

    public AuthController(
        AuthDbContext db,
        JwtTokenService jwt,
        PasswordHasher hasher)
    {
        _db = db;
        _jwt = jwt;
        _hasher = hasher;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(LoginRequest request)
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = request.Username,
            PasswordHash = _hasher.Hash(request.Password),
      
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        return Ok("User registered");
    }
    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.Username == request.Username);

        if (user == null)
            return Unauthorized();

        if (!_hasher.Verify(request.Password, user.PasswordHash))
            return Unauthorized();

        // 🔐 Generate Access Token
        var accessToken = _jwt.GenerateToken(user.Username, user.Role);

        // 🔄 Generate Refresh Token
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Token = Guid.NewGuid().ToString(),
            ExpiryDate = DateTime.UtcNow.AddDays(30),
            IsRevoked = false
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        return Ok(new
        {
            accessToken,
            refreshToken = refreshToken.Token,
            expiresIn = 900 // 15 minutes
        });
    }


    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh(TokenRequest request)
    {
        var storedToken = await _db.RefreshTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(x => x.Token == request.RefreshToken);

        if (storedToken == null ||
            storedToken.IsRevoked ||
            storedToken.ExpiryDate < DateTime.UtcNow)
            return Unauthorized();

        var newAccessToken =
            _jwt.GenerateToken(storedToken.User.Username,
                               storedToken.User.Role);

        return Ok(new { accessToken = newAccessToken });
    }

}
