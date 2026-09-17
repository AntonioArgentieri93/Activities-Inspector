using Activities_Inspector.Models;
using Activities_Inspector.Services;
using System;
using System.Collections.Generic;

namespace Activities_Inspector.Utils
{
    public static class ShellBagParser
    {
        /// <summary>
        /// Identifies and gathers ShellBag items from raw binary registry data.
        /// A failing registry key no longer aborts the whole collection:
        /// items gathered so far are kept and <c>Truncated</c> is set.
        /// </summary>
        /// <returns>the gathered items plus whether collection was interrupted by an error</returns>
        public static (List<IShellItem> Items, bool Truncated) GetShellItems(IRegistryReader registryReader)
        {
            List<IShellItem> shellItems = new List<IShellItem>();
            Dictionary<RegistryKeyWrapper, IShellItem> keyShellMappings = new Dictionary<RegistryKeyWrapper, IShellItem>();
            var truncated = false;

            List<RegistryKeyWrapper> keys;
            try
            {
                keys = registryReader.GetRegistryKeys();
            }
            catch
            {
                return (shellItems, true);
            }

            foreach (RegistryKeyWrapper keyWrapper in keys)
            {
                try
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
                catch
                {
                    truncated = true;
                }
            }
            return (shellItems, truncated);
        }
    }
}
