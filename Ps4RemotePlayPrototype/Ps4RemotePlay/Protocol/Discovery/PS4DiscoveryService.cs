using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Ps4RemotePlay.Protocol.Model;
using Ps4RemotePlay.Util;

namespace Ps4RemotePlay.Protocol.Discovery
{
    public class PS4DiscoveryService
    {
        private readonly object _lockObject = new object();

        private const int DiscoveryPort = 987;

        private const int MaxUdpDatagramSize = 65000;

        public void DiscoverConsole(Action<PS4DiscoveryInfo> callback)
        {
            Task.Factory.StartNew(() =>
            {
                lock (_lockObject)
                {
                    using (Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                    {
                        udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                        udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                        udpSocket.ExclusiveAddressUse = false;
                        udpSocket.ReceiveTimeout = 3000;

                        string data = "SRCH * HTTP/1.1\r\n";
                        data += "device - discovery - protocol - version:0020020\r\n";
                        data += "\r\n";
                        byte[] packetData = Encoding.ASCII.GetBytes(data);
                        byte[] buffer = new byte[MaxUdpDatagramSize];

                        IPAddress broadcatAddress = NetworkUtils.GetBroadcastIp();
                        IPEndPoint broadcastEndpoint = new IPEndPoint(broadcatAddress, DiscoveryPort);
                        EndPoint ps4Ip = new IPEndPoint(IPAddress.Any, 0);
                        PS4DiscoveryInfo ps4DiscoveryInfo = null;

                        for (int i = 0; i < 3; i++)
                        {
                            udpSocket.SendTo(packetData, broadcastEndpoint);

                            try
                            {
                                int received = udpSocket.ReceiveFrom(buffer, ref ps4Ip);
                                string response = Encoding.ASCII.GetString(buffer, 0, received);
                                DiscoveryHeaderResponse discoveryHeaderResponse = SplitHttpResponse(response);
                                ps4DiscoveryInfo = new PS4DiscoveryInfo(discoveryHeaderResponse.Status, (IPEndPoint)ps4Ip, response.Replace("\n", Environment.NewLine));
                                break;
                            }
                            catch (SocketException)
                            {
                                // Ignore
                            }
                        }

                        callback.Invoke(ps4DiscoveryInfo);
                    }
                }
            });
        }

        private DiscoveryHeaderResponse SplitHttpResponse(string data)
        {
            string[] splitData = data.Split('\n');
            string firstLine = splitData[0];
            string code = Regex.Split(firstLine, @"\s+")[1];

            Dictionary<string, string> httpHeaders = new Dictionary<string, string>();
            for (int i = 1; i < splitData.Length; i++)
            {
                string pair = splitData[i];
                string[] keyValue = pair.Split(':');
                if (keyValue.Length == 2)
                {
                    httpHeaders.Add(keyValue[0], keyValue[1]);
                }
            }

            int.TryParse(code, out var codeValue);
            return new DiscoveryHeaderResponse(codeValue, httpHeaders);
        }

        /*******************/
        /** inner classes **/
        /*******************/

        private class DiscoveryHeaderResponse
        {

            public int Status { get; }
            public Dictionary<string, string> HttpHeaders { get; }

            public DiscoveryHeaderResponse(int status, Dictionary<string, string> httpHeaders)
            {
                this.Status = status;
                this.HttpHeaders = httpHeaders;
            }
        }
    }
}
