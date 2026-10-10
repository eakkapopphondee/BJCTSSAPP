using System.Security.Cryptography;
using System.Text;

namespace thaibev.Data;

public sealed class PasswordHash(IConfiguration configuration)
{
    public string Compute(string password)
    {
        var salt = configuration["Authentication:PasswordSalt"];
        if (string.IsNullOrEmpty(salt))
            throw new InvalidOperationException("Authentication:PasswordSalt must be configured.");
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(password + salt)));
    }
}
