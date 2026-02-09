using System;

namespace AuthService.Models;

public class RefreshToken
{
    public Guid Id { get; set; }

    public Guid UserId { get; set; }

    public string Token { get; set; } = default!;

    public DateTime ExpiryDate { get; set; }

    public bool IsRevoked { get; set; }

    // Navigation
    public User User { get; set; } = default!;
}
