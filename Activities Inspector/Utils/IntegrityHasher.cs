using Activities_Inspector.Models;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Activities_Inspector.Utils
{
    internal static class IntegrityHasher
    {
        internal static IntegrityRecord HashFile(string path, EntryType feature)
        {
            var acquiredUtc = DateTime.UtcNow;

            try
            {
                // Sola lettura, condivisione massima: non nega l'accesso
                // ad altri lettori e non modifica il file in alcun modo.
                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sha256 = SHA256.Create();
                var hash = sha256.ComputeHash(stream);

                return new IntegrityRecord(feature, path, ToHex(hash), stream.Length,
                    acquiredUtc, IntegrityStatus.Acquired);
            }
            catch (Exception ex) when (!(ex is OperationCanceledException))
            {
                return new IntegrityRecord(feature, path, null, null,
                    acquiredUtc, IntegrityStatus.NotAcquirable, ShortReason(ex));
            }
        }

        internal static string ToHex(byte[] hash)
        {
            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        private static string ShortReason(Exception ex)
        {
            if (ex is UnauthorizedAccessException) return "Accesso negato";
            if (ex is FileNotFoundException || ex is DirectoryNotFoundException) return "File non trovato";
            if (ex is IOException) return "File in uso o errore di I/O";
            return ex.GetType().Name;
        }
    }
}
