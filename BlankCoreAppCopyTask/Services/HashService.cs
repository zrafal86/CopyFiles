using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace BlankCoreAppCopyTask.Services
{
    public interface IHashCalculator
    {
        string CalculateHash(string filePath, HashAlgorithm algorithm);
    }

    internal class HashService : IHashCalculator
    {
        public string CalculateHash(string filePath, HashAlgorithm algorithm)
        {
            using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read);
            var bytes = algorithm.ComputeHash(stream);

            var builder = new StringBuilder();
            foreach (var t in bytes) builder.Append(t.ToString("x2"));

            return builder.ToString();
        }
    }
}