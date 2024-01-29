using System;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Windows;

namespace ProgettoInformaticaForense_Argentieri
{
    public partial class App : Application
    {
        private async void Application_StartupAsync(object sender, StartupEventArgs e)
        {
            var localDate = DateTime.Now;
            var ntpDate = await GetNetworkTime();

            var isValid = IsValidLocalDateTime(localDate, ntpDate);

            if (!isValid)
            {
                MessageBox.Show(Activities_Inspector.Resources.App_DateTime_Error_Message,
                    Activities_Inspector.Resources.AppName,
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static async Task<DateTime?> GetNetworkTime(string ntpServer = "time.windows.com")
        {
            if (ntpServer == null) throw new ArgumentNullException(nameof(ntpServer));

            try
            {
                const int daysTo1900 = 1900 * 365 + 95;
                const long ticksPerSecond = 10000000L;
                const long ticksPerDay = 24 * 60 * 60 * ticksPerSecond;
                const long ticksTo1900 = daysTo1900 * ticksPerDay;

                var ntpData = new byte[48];
                ntpData[0] = 0x1B;

                var addresses = Dns.GetHostEntry(ntpServer).AddressList;
                var ipEndPoint = new IPEndPoint(addresses[0], 123);

                var pingDuration = Stopwatch.GetTimestamp();

                using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    await socket.ConnectAsync(ipEndPoint);
                    socket.ReceiveTimeout = 5000;
                    socket.Send(ntpData);
                    pingDuration = Stopwatch.GetTimestamp();

                    socket.Receive(ntpData);
                    pingDuration = Stopwatch.GetTimestamp() - pingDuration;
                }

                var pingTicks = pingDuration * ticksPerSecond / Stopwatch.Frequency;

                var intPart = (long)ntpData[40] << 24 | (long)ntpData[41] << 16 | (long)ntpData[42] << 8 | ntpData[43];
                var fractPart = (long)ntpData[44] << 24 | (long)ntpData[45] << 16 | (long)ntpData[46] << 8 | ntpData[47];
                var netTicks = intPart * ticksPerSecond + (fractPart * ticksPerSecond >> 32);

                var networkDateTime = new DateTime(ticksTo1900 + netTicks + pingTicks / 2);

                return networkDateTime.ToLocalTime(); // without ToLocalTime() = faster
            }
            catch
            {
                return null;
            }
        }

        private static bool IsValidLocalDateTime(DateTime local, DateTime? ntp)
        {
            if (ntp.HasValue == false) return false;

            var timeSpan = ntp.Value.Subtract(local);

            return timeSpan.TotalMinutes > 1 ? false : true;
        }
    }
}
