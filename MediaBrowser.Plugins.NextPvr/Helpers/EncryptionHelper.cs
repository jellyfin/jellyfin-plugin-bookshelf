using System;
using System.Security.Cryptography;
using System.Text;

namespace MediaBrowser.Plugins.NextPvr.Helpers
{
    public static class EncryptionHelper
    {
        public static string GetMd5Hash(string value)
        {
            HashAlgorithm hashAlgorithm = HashAlgorithm.Create("MD5");

            if (hashAlgorithm != null)
            {
                byte[] hashValue = hashAlgorithm.ComputeHash(new UTF8Encoding().GetBytes(value));
                //Bit convertor return the byte to string as all caps hex values seperated by "-"
                return BitConverter.ToString(hashValue).Replace("-", "").ToLowerInvariant();
            }

            return string.Empty;
        }
    }
}
