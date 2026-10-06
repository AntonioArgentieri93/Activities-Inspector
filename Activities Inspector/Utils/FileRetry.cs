using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Activities_Inspector.Utils
{
    /// <summary>
    /// Lettura di un file appena scritto con brevi tentativi. Subito dopo la chiusura un antivirus (o OneDrive)
    /// puo' tenere il file aperto per qualche istante: la lettura fallisce con "file in uso" pur senza alcun errore
    /// reale, e l'esportazione riportava un fallimento anche se il CSV era stato scritto correttamente.
    /// </summary>
    internal static class FileRetry
    {
        public static async Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default, int attempts = 8)
        {
            for (var attempt = 1; ; attempt++)
            {
                try
                {
                    return await File.ReadAllBytesAsync(path, cancellationToken);
                }
                catch (IOException ex) when (attempt < attempts
                                             && !(ex is FileNotFoundException)
                                             && !(ex is DirectoryNotFoundException))
                {
                    // 25, 100, 225, 400... ms: in totale al massimo ~3,5 s, solo nel caso (raro) di file bloccato
                    await Task.Delay(TimeSpan.FromMilliseconds(25 * attempt * attempt), cancellationToken);
                }
            }
        }
    }
}
