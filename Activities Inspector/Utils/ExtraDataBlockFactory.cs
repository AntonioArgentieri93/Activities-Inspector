using ProgettoInformaticaForense_Argentieri.ExtraData;
using ProgettoInformaticaForense_Argentieri.ExtraData.ExtraData;
using System;

namespace ProgettoInformaticaForense_Argentieri.Utility
{
    /// <summary>
    /// Crea l'ExtraDataBase appropriato in base alla signature del blocco.
    /// Logica estratta dal costruttore di LnkFile, comportamento invariato.
    /// </summary>
    public static class ExtraDataBlockFactory
    {
        public static ExtraDataBase Create(byte[] extraBlock)
        {
            try
            {
                var sig = (ExtraDataTypes)BitConverter.ToInt32(extraBlock, 4);

                switch (sig)
                {
                    case ExtraDataTypes.TrackerDataBlock:
                        return new TrackerDataBaseBlock(extraBlock);
                    case ExtraDataTypes.ConsoleDataBlock:
                        return new ConsoleDataBlock(extraBlock);
                    case ExtraDataTypes.ConsoleFeDataBlock:
                        return new ConsoleFeDataBlock(extraBlock);
                    case ExtraDataTypes.DarwinDataBlock:
                        return new DarwinDataBlock(extraBlock);
                    case ExtraDataTypes.EnvironmentVariableDataBlock:
                        return new EnvironmentVariableDataBlock(extraBlock);
                    case ExtraDataTypes.IconEnvironmentDataBlock:
                        return new IconEnvironmentDataBlock(extraBlock);
                    case ExtraDataTypes.KnownFolderDataBlock:
                        return new KnownFolderDataBlock(extraBlock);
                    case ExtraDataTypes.PropertyStoreDataBlock:
                        return new PropertyStoreDataBlock(extraBlock);
                    case ExtraDataTypes.ShimDataBlock:
                        // NOTA: il codice originale istanzia qui KnownFolderDataBlock
                        // invece di ShimDataBlock (probabile refuso upstream).
                        // Mantenuto intenzionalmente per non alterare il comportamento.
                        return new KnownFolderDataBlock(extraBlock);
                    case ExtraDataTypes.SpecialFolderDataBlock:
                        return new SpecialFolderDataBlock(extraBlock);
                    case ExtraDataTypes.VistaAndAboveIdListDataBlock:
                        return new VistaAndAboveIdListDataBlock(extraBlock);
                    default:
                        throw new Exception(
                            $"Unknown extra data block signature: 0x{sig:X}. Please send lnk file to saericzimmerman@gmail.com so support can be added");
                }
            }
            catch (Exception e)
            {
                return new DamagedDataBlock(extraBlock, e.Message);
            }
        }
    }
}
