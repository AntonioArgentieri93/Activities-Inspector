using Activities_Inspector.Constants;
using Activities_Inspector.Utils;
using System;
using System.Collections.Generic;

namespace Activities_Inspector.Models
{
    public class RegistryShellItemDecorator : IShellItem
    {
        private const string AbsolutePathIdentifier = "AbsolutePath";
        protected IShellItem BaseShellItem { get; }
        protected RegistryKeyWrapper RegKey { get; }

        public RegistryShellItemDecorator(IShellItem shellItem, RegistryKeyWrapper regKey, IShellItem parentShellItem = null)
        {
            BaseShellItem = shellItem ?? throw new ArgumentNullException(nameof(shellItem));
            RegKey = regKey ?? throw new ArgumentNullException(nameof(regKey));
            Name = ResolveName(shellItem, regKey.Value);
            AbsolutePath = BuildAbsolutePath(parentShellItem, Name);
        }

        public ushort Size => BaseShellItem.Size;
        public byte Type => BaseShellItem.Type;
        public string TypeName => BaseShellItem.TypeName ?? string.Empty;
        public string Name { get; }
        public DateTime ModifiedDate => BaseShellItem.ModifiedDate;
        public DateTime AccessedDate => BaseShellItem.AccessedDate;
        public DateTime CreationDate => BaseShellItem.CreationDate;

        public string AbsolutePath { get; }

        /// <summary>LastWriteTime (ora locale) della chiave BagMRU dell'elemento.</summary>
        public DateTime? KeyLastWriteTime => RegKey.KeyLastWriteTime;

        /// <summary>Ultima interazione (ora locale) se l'elemento è in posizione 0 del MRUListEx del genitore, altrimenti null.</summary>
        public DateTime? LastInteracted => RegKey.LastInteracted;

        public string RegistryPath => RegKey.RegistryPath;

        public IDictionary<string, string> GetAllProperties()
        {
            IDictionary<string, string> baseDict = BaseShellItem.GetAllProperties();

            baseDict[AppConstants.ShellItems.Name] = Name;
            baseDict[AbsolutePathIdentifier] = AbsolutePath;

            if (RegKey.RegistryUser != string.Empty)
                baseDict[AppConstants.ShellItems.RegistryOwner] = RegKey.RegistryUser;
            if (RegKey.RegistrySID != string.Empty)
                baseDict[AppConstants.ShellItems.RegistrySid] = RegKey.RegistrySID;
            if (RegKey.RegistryPath != string.Empty)
                baseDict[AppConstants.ShellItems.RegistryPath] = RegKey.RegistryPath;
            if (RegKey.ShellbagPath != string.Empty)
                baseDict[AppConstants.ShellItems.ShellbagPath] = RegKey.ShellbagPath;
            if (RegKey.LastRegistryWriteDate != DateTime.MinValue)
                baseDict[AppConstants.ShellItems.LastRegWrite] = RegKey.LastRegistryWriteDate.ToString();
            if (RegKey.SlotModifiedDate != DateTime.MinValue)
                baseDict[AppConstants.ShellItems.SlotModifiedDate] = RegKey.SlotModifiedDate.ToString();


            return baseDict;
        }

        /// <summary>
        /// Nome dell'elemento. Se il parser principale non lo ricava (nome vuoto/"??", item generico, oppure tipo 0x2E
        /// che qui è interpretato come etichetta di volume) si usa il parser di Eric Zimmerman (ShellItemFactory),
        /// che gestisce profilo utente, cartelle GUID, MTP, URI ecc. Un nome comunque non ricavabile diventa un
        /// segnaposto esplicito, così il percorso dei figli non perde silenziosamente un livello.
        /// </summary>
        internal static string ResolveName(IShellItem item, byte[] raw)
        {
            var name = item.Name;

            var mtpDevice = TryDecodeMtpDevice(raw);
            if (mtpDevice != null)
                return mtpDevice;

            var knownFolder = TryDecodeGuidFolder(raw);
            if (knownFolder != null)
                return knownFolder;

            bool weak = string.IsNullOrWhiteSpace(name) || name == "??"
                        || item.Type == 0x2E || item.GetType() == typeof(ShellItem)
                        // Root item con GUID non riconosciuto ("{FOLDER_ID: guid}"): può essere una variante
                        // senza GUID, es. "Users property view: drive letter" che contiene "E:\"
                        || (item.Type == 0x1F && name.StartsWith("{", StringComparison.Ordinal) && name.Contains(": "));

            if (weak && raw != null && raw.Length >= 3)
            {
                try
                {
                    var value = ShellItemFactory.Create(raw)?.Value;
                    if (!string.IsNullOrWhiteSpace(value) && !value.StartsWith("!!!", StringComparison.Ordinal))
                        return value.Trim();
                }
                catch (Exception)
                { }
            }

            // 0x2E non riconosciuto: il nome "da volume" sono byte del GUID → meglio il GUID in chiaro
            if (item.Type == 0x2E && raw != null && raw.Length >= 0x14 && !IsPrintable(name))
                return "{" + ReadGuid(raw, 4) + "}";

            if (string.IsNullOrWhiteSpace(name) || name == "??")
                return $"[elemento 0x{item.Type:X2} non decodificato]";

            return name;
        }

        /// <summary>
        /// Item 0x2E con GUID di cartella all'offset 4 (es. {374DE290-…} = Downloads sotto "Questo PC"):
        /// nome della cartella nota, o null se il GUID non è noto.
        /// </summary>
        internal static string TryDecodeGuidFolder(byte[] raw)
        {
            if (raw == null || raw.Length < 0x14 || raw[2] != 0x2E) return null;
            return KnownGuids.dict.TryGetValue(ReadGuid(raw, 4), out var folder) ? folder : null;
        }

        private static string ReadGuid(byte[] raw, int offset)
        {
            var bytes = new byte[16];
            Buffer.BlockCopy(raw, offset, bytes, 0, 16);
            return new Guid(bytes).ToString();
        }

        /// <summary>
        /// Dispositivo portatile MTP/WPD (item 0x2E, es. smartphone collegato via USB):
        /// [size:2][0x2E][?:1][size:2][firma:4 = 0x08312006 / 0x10312005] … tre lunghezze in caratteri (NUL incluso)
        /// @0x1A, @0x1E, @0x22 e, da 0x26, le tre stringhe UTF-16LE consecutive: [?, di solito vuota][nome dispositivo]
        /// [percorso dispositivo \\?\usb#vid_…]. Il nome è la seconda.
        /// </summary>
        internal static string TryDecodeMtpDevice(byte[] raw)
        {
            if (raw == null || raw.Length < 0x2A || raw[2] != 0x2E) return null;

            var signature = BitConverter.ToUInt32(raw, 6);
            if (signature != 0x08312006 && signature != 0x10312005) return null;

            var firstChars = BitConverter.ToInt32(raw, 0x1A);
            var nameChars = BitConverter.ToInt32(raw, 0x1E);
            if (firstChars < 0 || firstChars > 260 || nameChars < 2 || nameChars > 260) return null;

            var start = 0x26 + firstChars * 2;
            var end = start + nameChars * 2;          // dopo il terminatore
            if (end > raw.Length || raw[end - 2] != 0 || raw[end - 1] != 0) return null;

            var name = System.Text.Encoding.Unicode.GetString(raw, start, (nameChars - 1) * 2);
            return IsPrintable(name) ? name : null;
        }

        private static bool IsPrintable(string s)
        {
            if (string.IsNullOrWhiteSpace(s)) return false;
            foreach (var c in s)
                if (char.IsControl(c) || c == '�' || char.IsSurrogate(c)) return false;
            return true;
        }

        /// <summary>
        /// Percorso = percorso del genitore + "\" + nome. Nessun segmento viene scartato (prima venivano eliminati
        /// quelli con "{" o "?", cioè le cartelle note come Documenti/Desktop e le cartelle con graffe nel nome).
        /// </summary>
        internal static string BuildAbsolutePath(IShellItem parentShellItem, string name)
        {
            if (parentShellItem == null)
                return name;

            var parentProperties = parentShellItem.GetAllProperties();
            if (!parentProperties.TryGetValue(AbsolutePathIdentifier, out string parentPath) || string.IsNullOrEmpty(parentPath))
                return name;

            var path = parentPath.TrimEnd('\\') + "\\" + name.TrimStart('\\');
            while (path.Contains(@"\\") && !path.StartsWith(@"\\"))
                path = path.Replace(@"\\", @"\");
            return path;
        }
    }
}
