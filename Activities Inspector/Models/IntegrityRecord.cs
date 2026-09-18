using System;
using System.Collections.Generic;

namespace Activities_Inspector.Models
{
    public enum IntegrityStatus
    {
        Acquired,
        NotAcquirable,
        LiveSource
    }

    public class IntegrityRecord
    {
        public EntryType Feature { get; }
        public string Path { get; }
        public string Sha256 { get; }
        public long? SizeBytes { get; }
        public DateTime? AcquiredUtc { get; }
        public IntegrityStatus Status { get; }
        public string Detail { get; }

        public IntegrityRecord(EntryType feature, string path, string sha256, long? sizeBytes,
            DateTime? acquiredUtc, IntegrityStatus status, string detail = null)
        {
            Feature = feature;
            Path = path;
            Sha256 = sha256;
            SizeBytes = sizeBytes;
            AcquiredUtc = acquiredUtc;
            Status = status;
            Detail = detail;
        }

        public static IntegrityRecord LiveSource(EntryType feature, string sourceDescription)
        {
            return new IntegrityRecord(feature, sourceDescription, null, null, null,
                IntegrityStatus.LiveSource, "Letto via API live, nessun file acquisibile");
        }
    }
}
