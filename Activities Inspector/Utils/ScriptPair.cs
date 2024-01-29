using System;
using System.Collections.Generic;
using System.Text;

namespace ProgettoInformaticaForense_Argentieri.Utils
{
    public class ScriptPair
    {

        public int typeidentifier { get; set; }

        private string decodedScript;
        public string script
        {
            get { return decodedScript; }
            set
            {
                decodedScript = Base64Decode(value);
            }
        }

        public ScriptPair() { }

        private static string Base64Decode(string base64EncodedData)
        {
            var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
            return Encoding.UTF8.GetString(base64EncodedBytes);
        }
    }

    public class DecodedScriptPair
    {

        public int typeidentifier { get; set; }
        public string script { get; set; }

        public DecodedScriptPair() { }

        public KeyValuePair<int, string> getScript()
        {
            return new KeyValuePair<int, string>(typeidentifier, script);
        }
    }
}
