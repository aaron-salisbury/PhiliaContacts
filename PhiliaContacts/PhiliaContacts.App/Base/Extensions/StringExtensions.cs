using System.Security.Cryptography;
using System.Text;

namespace PhiliaContacts.App.Base.Extensions
{
    public static class StringExtensions
    {
        /// <summary>
        /// Return hash-string of a string.
        /// </summary>
        public static string GetHashString(this string value)
        {
            // https://stackoverflow.com/questions/3984138/hash-string-in-c-sharp

            if (string.IsNullOrEmpty(value))
            {
                return null;
            }

            using (HashAlgorithm algorithm = SHA256.Create())
            {
                byte[] hash = algorithm.ComputeHash(Encoding.UTF8.GetBytes(value));

                StringBuilder stringBuilder = new StringBuilder();
                foreach (byte b in hash)
                {
                    stringBuilder.Append(b.ToString("X2"));
                }

                return stringBuilder.ToString();
            }
        }
    }
}
