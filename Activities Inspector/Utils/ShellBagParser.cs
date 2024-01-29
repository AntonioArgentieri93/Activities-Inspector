using ProgettoInformaticaForense_Argentieri.Models;
using ProgettoInformaticaForense_Argentieri.Services;
using System;
using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Utils
{
    public static class ShellBagParser
    {
        /// <summary>
        /// Identifies and gathers ShellBag items from raw binary registry data.
        /// </summary>
        /// <returns>a list of different variety ShellBag items</returns>
        public static List<IShellItem> GetShellItems(IRegistryReader registryReader)
        {
            List<IShellItem> shellItems = new List<IShellItem>();
            Dictionary<RegistryKeyWrapper, IShellItem> keyShellMappings = new Dictionary<RegistryKeyWrapper, IShellItem>();
            
            foreach (RegistryKeyWrapper keyWrapper in registryReader.GetRegistryKeys())
            {
                if (keyWrapper.Value != null) // Some Registry Keys are null
                {
                    ShellItemList shellItemList = new ShellItemList(keyWrapper.Value);
                    foreach (IShellItem shellItem in shellItemList.Items())
                    {

                        IShellItem parentShellItem = null;
                        //obtain the parent shellitem from the parent registry key (if it exists)
                        if (keyWrapper.Parent != null)
                        {
                            if (keyShellMappings.TryGetValue(keyWrapper.Parent, out IShellItem pShellItem))
                            {
                                parentShellItem = pShellItem;
                            }
                        }

                        RegistryShellItemDecorator decoratedShellItem = new RegistryShellItemDecorator(shellItem, keyWrapper, parentShellItem);
                        try
                        {
                            keyShellMappings.Add(keyWrapper, decoratedShellItem);
                        }
                        catch (ArgumentException)
                        { }

                        shellItems.Add(decoratedShellItem);
                    }
                }
            }
            return shellItems;
        }
    }
}
