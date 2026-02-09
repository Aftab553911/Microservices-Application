using System;
using System.Collections.Generic;

namespace AuthService.Models;

public class User
{
    public Guid Id { get; set; }

    public string Username { get; set; } = default!;

    public string PasswordHash { get; set; } = default!;

    public string Role { get; set; } = "Customer";

    // Navigation
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
