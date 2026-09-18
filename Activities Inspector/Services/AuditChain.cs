using Activities_Inspector.Models;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Activities_Inspector.Services
{
    internal static class AuditChain
    {
        internal const string GenesisPreviousHash = "GENESI";

        internal static string ComputeHash(int sequence, System.DateTime timestampUtc,
            AuditCategory category, string detail, string previousHash)
        {
            var input = $"{sequence}|{timestampUtc:O}|{category}|{detail}|{previousHash}";

            using var sha256 = SHA256.Create();
            var hash = sha256.ComputeHash(Encoding.UTF8.GetBytes(input));

            var sb = new StringBuilder(hash.Length * 2);
            foreach (var b in hash)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        internal static bool Verify(IReadOnlyList<AuditEntry> entries)
        {
            var previous = GenesisPreviousHash;

            foreach (var entry in entries.OrderBy(e => e.Sequence))
            {
                if (entry.PreviousHash != previous) return false;

                var expected = ComputeHash(entry.Sequence, entry.TimestampUtc,
                    entry.Category, entry.Detail, entry.PreviousHash);

                if (entry.Hash != expected) return false;

                previous = entry.Hash;
            }

            return true;
        }
    }
}
