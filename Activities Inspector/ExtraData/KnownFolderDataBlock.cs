using System;

namespace ProgettoInformaticaForense_Argentieri.ExtraData.ExtraData
{
    public class KnownFolderDataBlock : ExtraDataBase
    {
        public KnownFolderDataBlock(byte[] rawBytes)
        {
            Signature = ExtraDataTypes.KnownFolderDataBlock;

            Size = BitConverter.ToUInt32(rawBytes, 0);

            var kfBytes = new byte[16];
            Buffer.BlockCopy(rawBytes, 8, kfBytes, 0, 16);

            KnownFolderId = new Guid(kfBytes);

            KnownFolderName = ExtensionBlocks.Utils.GetFolderNameFromGuid(KnownFolderId.ToString());

            Offset = BitConverter.ToUInt32(rawBytes, 24);
        }

        public uint Offset { get; }

        public Guid KnownFolderId { get; }
        public string KnownFolderName { get; }

        public override string ToString()
        {
            return $"Known folder data block" +
                   $"\r\nKnown folder GUID: {KnownFolderId} ({KnownFolderName})";
        }
    }
}