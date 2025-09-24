using System.Security.Cryptography;
using System.Text;

namespace Adaplio.Api.Services;

public interface IAliasService
{
    string GenerateClientAlias(int clientId, int trainerId);
    string GenerateUniqueCode();
}

public class AliasService : IAliasService
{
    public string GenerateClientAlias(int clientId, int trainerId)
    {
        // Create stable hash from clientId + trainerId combination
        var input = $"{clientId}-{trainerId}";
        var inputBytes = Encoding.UTF8.GetBytes(input);

        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(inputBytes);

        // Take first 4 bytes and convert to base32-like format using alphanumeric chars
        var hash = BitConverter.ToUInt32(hashBytes, 0);

        // Convert to base36 (0-9, A-Z) and take first 4 characters
        var chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        var result = new StringBuilder(6);
        result.Append('C'); // Client prefix
        result.Append('-');

        var value = hash;
        for (int i = 0; i < 4; i++)
        {
            result.Append(chars[(int)(value % 36)]);
            value /= 36;
        }

        return result.ToString();
    }

    public string GenerateUniqueCode()
    {
        // Generate 8-character alphanumeric code for grant codes
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        using var rng = RandomNumberGenerator.Create();
        var bytes = new byte[8];
        rng.GetBytes(bytes);

        var result = new StringBuilder(8);
        for (int i = 0; i < 8; i++)
        {
            result.Append(chars[bytes[i] % chars.Length]);
        }

        return result.ToString();
    }
}