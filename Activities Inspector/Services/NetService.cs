using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;

namespace ProgettoInformaticaForense_Argentieri.Services
{
    public class NetService : INetService
    {
        private readonly IDialogService _dialogService;

        public NetService(IDialogService dialogService)
        {
            _dialogService = dialogService;
        }

        public IEnumerable<string> GetAvailablePrivateIPs()
        {
            var strHostName = string.Empty;

            var ipEntry = Dns.GetHostEntry(Dns.GetHostName());
            IPAddress[] addr = ipEntry.AddressList;

            if (addr.Length > 0)
            {
                var ipV4Addresses = addr.Where(ip => ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
                    .Select(ip => ip.ToString());

                foreach (var address in ipV4Addresses)
                    yield return address;
            }
        }

        public string GetPublicIPAddress()
        {
            var address = string.Empty;

            try
            {
                WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");
                using (WebResponse response = request.GetResponse())
                using (StreamReader stream = new StreamReader(response.GetResponseStream()))
                {
                    address = stream.ReadToEnd();
                }

                int first = address.IndexOf("Address: ") + 9;
                int last = address.LastIndexOf("</body>");
                address = address.Substring(first, last - first);

                return address;
            }
            catch (Exception ex)
            {
                _dialogService.ShowError(ex.Message);

                return string.Empty;
            }
        }
    }
}
