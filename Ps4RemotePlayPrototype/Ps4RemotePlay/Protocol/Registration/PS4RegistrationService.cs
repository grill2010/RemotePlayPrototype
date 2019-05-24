using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Org.BouncyCastle.Security;
using Ps4RemotePlay.Protocol.Crypto;
using Ps4RemotePlay.Protocol.Model;
using Ps4RemotePlay.Util;

namespace Ps4RemotePlay.Protocol.Registration
{
    public class PS4RegistrationService
    {
        public EventHandler<PS4RegisterModel> OnPs4RegisterSuccess;

        public EventHandler<string> OnPs4RegisterError;

        private readonly object _lockObject = new object();

        public const int RpControlPort = 9295;

        private const int MaxUdpDatagramSize = 65000;

        public void PairConsole(string psnId, int pin)
        {
            Task.Factory.StartNew(() =>
            {
                lock (_lockObject)
                {
                    try
                    {

                        IPEndPoint ps4EndPoint = FindConsole();
                        if (ps4EndPoint == null)
                        {
                            OnPs4RegisterError?.Invoke(this,
                                "Could not connect to PS4. PS4 not found or not answering");
                            return;
                        }

                        Session session = CryptoService.GetSessionForPin(pin);

                        Dictionary<string, string> registrationHeaders = new Dictionary<string, string>();
                        registrationHeaders.Add("Client-Type", "Windows");
                        registrationHeaders.Add("Np-Online-Id", psnId);
                        byte[] payload = session.Encrypt(ByteUtil.HttpHeadersToByteArray(registrationHeaders));

                        SecureRandom random = new SecureRandom();
                        byte[] buffer = new byte[480];
                        random.NextBytes(buffer);

                        byte[] paddedPayload = ByteUtil.ConcatenateArrays(buffer, payload);
                        byte[] nonceDerivative = session.GetNonceDerivative();
                        byte[] finalPaddedPayload;
                        using (var ms = new MemoryStream())
                        {
                            ms.SetLength(paddedPayload.Length);
                            ms.Write(paddedPayload, 0, paddedPayload.Length);
                            ms.Seek(284, SeekOrigin.Begin);
                            ms.Write(nonceDerivative, 0, nonceDerivative.Length);
                            finalPaddedPayload = ms.ToArray();
                        }

                        IPEndPoint ipEndPoint = new IPEndPoint(ps4EndPoint.Address, RpControlPort);
                        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                        socket.Connect(ipEndPoint);

                        string requestData = "POST /sce/rp/regist HTTP/1.1\r\n" +
                                             $"HOST: {ps4EndPoint.Address}\r\n" +
                                             "User-Agent: remoteplay Windows\r\n" +
                                             "Connection: close\r\n" +
                                             $"Content-Length: {finalPaddedPayload.Length}\r\n" +
                                             "\r\n";

                        socket.Send(ByteUtil.ConcatenateArrays(Encoding.UTF8.GetBytes(requestData),
                            finalPaddedPayload));

                        byte[] receiveBuffer = new byte[8192];
                        int readBytes = socket.Receive(receiveBuffer);
                        byte[] response = new byte[readBytes];
                        Buffer.BlockCopy(receiveBuffer, 0, response, 0, response.Length);
                        string httpResponse = Encoding.ASCII.GetString(receiveBuffer, 0, readBytes);

                        HttpStatusCode statusCode = HttpUtils.GetStatusCode(httpResponse);
                        if (statusCode == HttpStatusCode.OK)
                        {
                            byte[] responseData = HttpUtils.GetBodyPayload(response);
                            byte[] decryptedData = session.Decrypt(responseData);
                            string registerHeaderInfoComplete = Encoding.UTF8.GetString(decryptedData);
                            Dictionary<string, string> httpHeaders = ByteUtil.ByteArrayToHttpHeader(decryptedData);
                            httpHeaders.TryGetValue("AP-Ssid", out var apSsid);
                            httpHeaders.TryGetValue("AP-Bssid", out var apBssid);
                            httpHeaders.TryGetValue("AP-Key", out var apKey);
                            httpHeaders.TryGetValue("AP-Name", out var name);
                            httpHeaders.TryGetValue("PS4-Mac", out var mac);
                            httpHeaders.TryGetValue("PS4-RegistKey", out var registrationKey);
                            httpHeaders.TryGetValue("PS4-Nickname", out var nickname);
                            httpHeaders.TryGetValue("RP-KeyType", out var rpKeyType);
                            httpHeaders.TryGetValue("RP-Key", out var rpKey);

                            OnPs4RegisterSuccess?.Invoke(this, new PS4RegisterModel(apSsid, apBssid, apKey, name, mac, registrationKey, nickname, rpKeyType, rpKey, registerHeaderInfoComplete));
                        }
                        else
                        {
                            OnPs4RegisterError?.Invoke(this, "sce/rp/regist was not successful, return code was: " + statusCode);
                        }

                    }
                    catch (Exception e)
                    {
                        OnPs4RegisterError?.Invoke(this, "Could not connect to PS4. Exception: " + e);
                    }
                }
            });
        }

        private IPEndPoint FindConsole()
        {
            try
            {
                using (Socket udpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
                {
                    udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    udpSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, true);
                    udpSocket.ExclusiveAddressUse = false;
                    udpSocket.ReceiveTimeout = 3000;

                    byte[] packetData = Encoding.ASCII.GetBytes("SRC2");
                    byte[] buffer = new byte[MaxUdpDatagramSize];
                    IPAddress broadcatAddress = NetworkUtils.GetBroadcastIp();
                    IPEndPoint broadcastEndpoint = new IPEndPoint(broadcatAddress, RpControlPort);
                    EndPoint ps4Ip = new IPEndPoint(IPAddress.Any, 0);

                    for (int i = 0; i < 3; i++)
                    {
                        udpSocket.SendTo(packetData, broadcastEndpoint);

                        try
                        {
                            udpSocket.ReceiveFrom(buffer, ref ps4Ip);
                            return (IPEndPoint)ps4Ip;
                        }
                        catch (SocketException)
                        {
                            // Ignore
                        }
                    }
                }
            }
            catch (Exception)
            {
                // Ignore
            }

            return null;
        }
    }
}
