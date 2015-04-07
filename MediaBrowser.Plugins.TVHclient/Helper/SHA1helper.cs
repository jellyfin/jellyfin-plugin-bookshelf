using System.Security.Cryptography;

namespace MediaBrowser.Plugins.TVHclient.Helper
{
    public class SHA1helper
    {
        public static byte[] GenerateSaltedSHA1(string plainTextString, byte[] saltBytes)
        {
            HashAlgorithm algorithm = new SHA1Managed();

            byte[] plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainTextString);

            byte[] plainTextWithSaltBytes = new byte[plainTextBytes.Length + saltBytes.Length];
            for (int i = 0; i < plainTextBytes.Length; i++)
            {
                plainTextWithSaltBytes[i] = plainTextBytes[i];
            }
            for (int i = 0; i < saltBytes.Length; i++)
            {
                plainTextWithSaltBytes[plainTextBytes.Length + i] = saltBytes[i];
            }

            byte[] digest = algorithm.ComputeHash(plainTextWithSaltBytes);

            return digest;
        }
    }
}
