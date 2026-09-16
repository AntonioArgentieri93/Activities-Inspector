using ExtensionBlocks;
using RecentFolder.ShellBags;
using RecentFolder.ShellBags.ShellBags;
using System;

namespace ProgettoInformaticaForense_Argentieri.Utility
{
    /// <summary>
    /// Crea lo ShellBag appropriato in base al tipo di shell item.
    /// Logica estratta dal costruttore di LnkFile, comportamento invariato.
    /// </summary>
    public static class ShellItemFactory
    {
        public static ShellBag Create(byte[] shellItem)
        {
            if (shellItem.Length >= 0x28)
            {
                var sig1 = BitConverter.ToInt64(shellItem, 0x8);
                var sig2 = BitConverter.ToInt64(shellItem, 0x18);

                if (sig1 == 0 && sig2 == 0)
                {
                    //double check
                    if (shellItem[0x28] == 0x2f || shellItem[0x26] == 0x2f || shellItem[0x1a] == 0x2f ||
                        shellItem[0x1c] == 0x2f)
                    // forward slash in date or N / A
                    {
                        //zip?
                        return new ShellBagZipContents(shellItem);
                    }
                }
            }

            switch (shellItem[2])
            {
                case 0x1f:
                    return new ShellBag0X1F(shellItem);
                case 0x22:
                case 0x23:
                    return new ShellBag0X23(shellItem);
                case 0x2f:
                    return new ShellBag0X2F(shellItem);
                case 0x2e:
                    return new ShellBag0X2E(shellItem);
                case 0xb1:
                case 0x31:
                case 0x35:
                    return new RecentFolder.ShellBags.ShellBag0X31(shellItem);
                case 0x32:
                case 0x36:
                    return new ShellBag0X32(shellItem);
                case 0x00:
                    return new ShellBag0X00(shellItem);
                case 0x01:
                    return new ShellBag0X01(shellItem);
                case 0x71:
                    return new ShellBag0X71(shellItem);
                case 0x61:
                    return new ShellBag0X61(shellItem);
                case 0xC3:
                    return new ShellBag0Xc3(shellItem);
                case 0x74:
                case 0x77:
                    return new ShellBag0X74(shellItem);
                case 0xae:
                case 0xaa:
                case 0x79:
                    return new ShellBagZipContents(shellItem);
                case 0x41:
                case 0x42:
                case 0x43:
                case 0x46:
                case 0x47:
                    return new ShellBag0X40(shellItem);
                case 0x4C:
                    return new ShellBag0X4C(shellItem);
                default:
                    throw new Exception(
                        $"Unknown shell item ID: 0x{shellItem[2]:X}. Please send to saericzimmerman@gmail.com so support can be added.");
            }
        }
    }
}
