using System;
using System.IO;

namespace Activities_Inspector.Services.Evidence
{
    public class EvidenceSourceProvider : IEvidenceSourceProvider
    {
        public IEvidenceSource Current { get; private set; } = new LiveEvidenceSource();

        public void UseLive() => Current = new LiveEvidenceSource();

        public void UseImage(string rootPath)
        {
            if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
                throw new InvalidOperationException("Cartella immagine inesistente.");

            if (!Directory.Exists(Path.Combine(rootPath, "Windows")))
                throw new InvalidOperationException("La cartella non contiene un'immagine valida (manca Windows).");

            Current = new OfflineEvidenceSource(rootPath);
        }
    }
}
