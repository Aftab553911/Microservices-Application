namespace AuthService.Models;

public class RegisterRequest
{
    public string Username { get; set; } = default!;
    public string Password { get; set; } = default!;

    // Optional role assignment
    public string? Role { get; set; }
}
