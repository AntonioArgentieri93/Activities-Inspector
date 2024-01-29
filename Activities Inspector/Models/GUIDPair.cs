using Newtonsoft.Json;
using System.Collections.Generic;

namespace ProgettoInformaticaForense_Argentieri.Models
{
    public class GUIDPair
    {
        [JsonProperty(propertyName: "guid", Required = Required.Always)]
        public string GUID { private get; set; }

        [JsonProperty(propertyName: "name", Required = Required.Always)]
        public string Name { private get; set; }
        public GUIDPair(string guid, string name)
        {
            GUID = guid;
            Name = name;
        }

        public GUIDPair()
        {
        }

        public KeyValuePair<string, string> getKnownGUID()
            => new KeyValuePair<string, string>(GUID, Name);
    }
}
