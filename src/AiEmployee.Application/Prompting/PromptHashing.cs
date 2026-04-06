using System.Security.Cryptography;
using System.Text;

namespace AiEmployee.Application.Prompting;

public static class PromptHashing
{
    public static string ComputeSha256(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input ?? string.Empty);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
