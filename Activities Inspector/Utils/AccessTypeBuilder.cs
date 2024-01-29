using System;

namespace ProgettoInformaticaForense_Argentieri.Utils
{
    public static class AccessTypeBuilder
    {
        public static string BuildStringSessionType(string value)
        {
            switch (value)
            {
                case "2":
                    return Activities_Inspector.Resources.Sessions_AccessType_Interactive;
                case "4":
                    return Activities_Inspector.Resources.Sessions_AccessType_Batch;
                case "8":
                    return Activities_Inspector.Resources.Sessions_AccessType_NetworkCleartext;
                case "9":
                    return Activities_Inspector.Resources.Sessions_AccessType_NewCredentials;
                case "10":
                    return Activities_Inspector.Resources.Sessions_AccessType_RemoteInteractive;
                case "11":
                    return Activities_Inspector.Resources.Sessions_AccessType_CachedInteractive;
                case "12":
                    return Activities_Inspector.Resources.Sessions_AccessType_CachedRemoteInteractive;
                case "13":
                    return Activities_Inspector.Resources.Sessions_AccessType_CachedUnlock;

                default: throw new ArgumentException(nameof(value));
            }
        }
    }
}
