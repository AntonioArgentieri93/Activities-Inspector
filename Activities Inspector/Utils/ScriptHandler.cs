using ProgettoInformaticaForense_Argentieri.Models;
using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Utils
{
    public static class ScriptHandler
    {
        public static Dictionary<int, string> scripts { get; set; }

        static ScriptHandler()
        {
            scripts = new Dictionary<int, string>();
        }

        public static bool HasScriptForShellItem(int identifier)
        {
            return scripts.ContainsKey(identifier);
        }

        public static IShellItem ParseShellItem(byte[] buf, int identifier)
        {
            scripts.TryGetValue(identifier, out string script);

            return new LuaShellItem(buf, identifier, script);

        }
    }
}
