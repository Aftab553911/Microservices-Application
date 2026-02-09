using System;
using System.Security.Cryptography;
using System.Text;

namespace AuthService.Services;

public class PasswordHasher
{
    public string Hash(string password)
    {
        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(password);
        var hash = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hash);
    }

    public bool Verify(string password, string storedHash)
    {
        return Hash(password) == storedHash;
    }
}
