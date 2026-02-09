namespace AuthService.Models;

public class TokenRequest
{
    public string RefreshToken { get; set; } = default!;
}
