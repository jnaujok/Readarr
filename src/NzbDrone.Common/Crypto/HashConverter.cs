using System;
using System.Security.Cryptography;
using System.Text;

namespace NzbDrone.Common.Crypto
{
    public static class HashConverter
    {
        public static int GetHashInt31(string target)
        {
            var hash = GetHash(target);
            return BitConverter.ToInt32(hash, 0) & 0x7fffffff;
        }

        public static byte[] GetHash(string target)
        {
            return SHA1.HashData(Encoding.Default.GetBytes(target));
        }
    }
}
