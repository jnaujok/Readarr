using System;
using System.Security.Cryptography;
using System.Text;
using NzbDrone.Common.Extensions;

namespace NzbDrone.Core.Authentication
{
    public static class PasswordHasher
    {
        public const string Prefix = "pbkdf2";
        public const int Iterations = 100000;
        private const int SaltSize = 16;
        private const int KeySize = 32;

        public static string Hash(string password)
        {
            if (password.IsNullOrWhiteSpace())
            {
                throw new ArgumentException("Password cannot be empty", nameof(password));
            }

            var salt = RandomNumberGenerator.GetBytes(SaltSize);
            var hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

            return $"{Prefix}${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(hash)}";
        }

        public static bool Verify(string password, string stored)
        {
            if (password.IsNullOrWhiteSpace() || stored.IsNullOrWhiteSpace())
            {
                return false;
            }

            if (stored.StartsWith(Prefix + "$", StringComparison.Ordinal))
            {
                return VerifyPbkdf2(password, stored);
            }

            var legacy = password.SHA256Hash();
            var storedBytes = Encoding.UTF8.GetBytes(stored);
            var legacyBytes = Encoding.UTF8.GetBytes(legacy);

            if (storedBytes.Length != legacyBytes.Length)
            {
                return false;
            }

            return CryptographicOperations.FixedTimeEquals(storedBytes, legacyBytes);
        }

        public static bool NeedsRehash(string stored)
        {
            return stored.IsNullOrWhiteSpace() || !stored.StartsWith(Prefix + "$", StringComparison.Ordinal);
        }

        private static bool VerifyPbkdf2(string password, string stored)
        {
            var parts = stored.Split('$');
            if (parts.Length != 4)
            {
                return false;
            }

            if (!int.TryParse(parts[1], out var iterations) || iterations < 1)
            {
                return false;
            }

            byte[] salt;
            byte[] expected;
            try
            {
                salt = Convert.FromBase64String(parts[2]);
                expected = Convert.FromBase64String(parts[3]);
            }
            catch (FormatException)
            {
                return false;
            }

            var actual = Rfc2898DeriveBytes.Pbkdf2(password, salt, iterations, HashAlgorithmName.SHA256, expected.Length);

            return CryptographicOperations.FixedTimeEquals(actual, expected);
        }
    }
}
