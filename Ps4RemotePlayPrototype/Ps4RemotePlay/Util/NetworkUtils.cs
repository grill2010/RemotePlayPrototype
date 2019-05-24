using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace Ps4RemotePlay.Util
{
    public class NetworkUtils
    {
        /***
         * Source
         * https://stackoverflow.com/questions/18551686/how-do-you-get-hosts-broadcast-address-of-the-default-network-adapter-c-sharp
         */
        public static IPAddress GetBroadcastIp()
        {
            IPAddress maskIp = GetHostMask();
            IPAddress hostIp = GetHostIp();

            if (maskIp == null || hostIp == null)
                return null;

            byte[] complementedMaskBytes = new byte[4];
            byte[] broadcastIpBytes = new byte[4];

            for (int i = 0; i < 4; i++)
            {
                complementedMaskBytes[i] = (byte)~(maskIp.GetAddressBytes().ElementAt(i));
                broadcastIpBytes[i] = (byte)((hostIp.GetAddressBytes().ElementAt(i)) | complementedMaskBytes[i]);
            }

            return new IPAddress(broadcastIpBytes);

        }


        private static IPAddress GetHostMask()
        {

            NetworkInterface[] interfaces = NetworkInterface.GetAllNetworkInterfaces();

            foreach (NetworkInterface Interface in interfaces)
            {

                IPAddress hostIp = GetHostIp();

                UnicastIPAddressInformationCollection unicastIpInfoCol = Interface.GetIPProperties().UnicastAddresses;

                foreach (UnicastIPAddressInformation unicatIpInfo in unicastIpInfoCol)
                {
                    if (unicatIpInfo.Address.ToString() == hostIp.ToString())
                    {
                        return unicatIpInfo.IPv4Mask;
                    }
                }
            }

            return null;
        }

        private static IPAddress GetHostIp()
        {
            // Get preffered network
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                socket.Connect("8.8.8.8", 23456);
                if (socket.LocalEndPoint is IPEndPoint endPoint) return endPoint.Address;
            }
            // Use as fallback
            return (Dns.GetHostEntry(Dns.GetHostName())).AddressList.FirstOrDefault(ip => ip.AddressFamily == AddressFamily.InterNetwork);
        }
    }
}