using ProgettoInformaticaForense_Argentieri.Utils;
using System;
using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class ShellItemList : Block
    {
        public ShellItemList(byte[] buffer) : base(buffer, 0) { }

        protected IShellItem GetItem(int off)
        {
            int identifier = unpack_byte(off + 2); 

            if (ScriptHandler.HasScriptForShellItem(identifier))
            {
                try
                {
                    return ScriptHandler.ParseShellItem(buf, identifier);
                }
                catch (ArgumentNullException)
                {}
                catch (Exception)
                {}
            }

            string postfix = unpack_byte(off + 2).ToString("X2");
            Type type = Type.GetType("ProgettoInformaticaForense_Argentieri.Models.ShellItem0x" + postfix);

            if (type == null)
            {
                return new ShellItem(buf);
            }

            try
            {
                return (IShellItem)Activator.CreateInstance(type, buf);
            }
            catch (Exception)
            {}

            return new ShellItem(buf);
        }

        public IEnumerable<IShellItem> Items()
        {
            int off = offset;
            int size = 0;
            while (true)
            {
                if (size == off && off != 0)
                    break;

                size = unpack_word(off);

                if (size == 0)
                    break;

                IShellItem item = GetItem(off);

                size = item.Size;

                if (size > 0)
                {
                    yield return item;
                    off += size;
                }
                else
                {
                    break;
                }
            }
        }
    }
}
