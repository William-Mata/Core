using System.Security.Cryptography;

namespace Core.Infrastructure.Security;

internal static class SenhaHasher
{
    private const string Algoritmo = "PBKDF2";
    private const int Iteracoes = 100000;
    private const int TamanhoSalt = 16;
    private const int TamanhoHash = 32;

    public static string Hash(string senha)
    {
        var salt = RandomNumberGenerator.GetBytes(TamanhoSalt);
        var hash = Rfc2898DeriveBytes.Pbkdf2(senha, salt, Iteracoes, HashAlgorithmName.SHA256, TamanhoHash);
        return $"{Algoritmo}${Iteracoes}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
    }

    public static bool Verificar(string senha, string senhaHash)
    {
        if (string.IsNullOrWhiteSpace(senhaHash))
        {
            return false;
        }

        var partes = senhaHash.Split('$', StringSplitOptions.RemoveEmptyEntries);
        if (partes.Length != 4 || !string.Equals(partes[0], Algoritmo, StringComparison.Ordinal) || !int.TryParse(partes[1], out var iteracoes))
        {
            return false;
        }

        try
        {
            var salt = Convert.FromBase64String(partes[2]);
            var hashEsperado = Convert.FromBase64String(partes[3]);
            var hashInformado = Rfc2898DeriveBytes.Pbkdf2(senha, salt, iteracoes, HashAlgorithmName.SHA256, hashEsperado.Length);
            return CryptographicOperations.FixedTimeEquals(hashInformado, hashEsperado);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
