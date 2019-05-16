using System;
using System.Collections.Generic;
using System.ComponentModel.Design.Serialization;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Google.Protobuf;
using Org.BouncyCastle.Utilities.Encoders;
using Org.BouncyCastle.Utilities.IO;
using ProtoBuf;
using Ps4RemotePlayPrototype.Protocol.Crypto;
using Ps4RemotePlayPrototype.Protocol.Message;
using Ps4RemotePlayPrototype.Util;

namespace Ps4RemotePlayPrototype.Protocol.Connection
{
    public class PS4ConnectionService : IDisposable
    {
        private const int RpControlPort = 9295;

        private const int RpRemotePlayPort = 9296;

        private const int RpUnknonwPort = 9297; // it is used by the official ps4 remote play client but I don't know yet how and why it is used (3rd party client is not using this port at all)

        private const int MaxUdpPacketSize = 65_000;

        public EventHandler OnPs4ConnectionSuccess;

        public EventHandler OnPs4Disconnected;

        public EventHandler<string> OnPs4ConnectionError;

        public EventHandler<string> OnPs4LogInfo;

        private Socket clientSocket;

        private Session currentSession;

        private readonly object _lockObject = new object();

        public void ConnectToPS4(IPEndPoint ps4Endpoint, PS4RemotePlayData ps4RemotePlayData)
        {
            Task.Factory.StartNew(() =>
            {
                lock (_lockObject)
                {
                    HandleSessionRequest(ps4Endpoint, ps4RemotePlayData);
                }
            });
        }

        public void Dispose()
        {
            clientSocket?.Close();
            clientSocket?.Dispose();
            clientSocket = null;

            currentSession = null;
        }

        /***********************/
        /*** private methods ***/
        /***********************/

        /*********** session request ***********/

        private void HandleSessionRequest(IPEndPoint ps4Endpoint, PS4RemotePlayData ps4RemotePlayData)
        {
            try
            {
                IPEndPoint ipEndPoint = new IPEndPoint(ps4Endpoint.Address, RpControlPort);
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.Connect(ipEndPoint);

                    string requestData = "GET /sce/rp/session HTTP/1.1\r\n" +
                                         $"HOST: {ps4Endpoint.Address}\r\n" +
                                         "User-Agent: remoteplay Windows\r\n" +
                                         "Connection: close\r\n" +
                                         "Content-Length: 0\r\n" +
                                         $"RP-Registkey: {ps4RemotePlayData.RemotePlay.RegistrationKey}\r\n" +
                                         "RP-Version: 8.0\r\n" +
                                         "\r\n";

                    socket.Send(Encoding.UTF8.GetBytes(requestData));

                    byte[] receiveBuffer = new byte[8192];
                    int readBytes = socket.Receive(receiveBuffer);
                    byte[] response = new byte[readBytes];
                    Buffer.BlockCopy(receiveBuffer, 0, response, 0, response.Length);
                    string httpResponse = Encoding.ASCII.GetString(receiveBuffer, 0, readBytes);

                    HttpStatusCode statusCode = HttpUtils.GetStatusCode(httpResponse);
                    if (statusCode == HttpStatusCode.OK)
                    {
                        OnPs4LogInfo?.Invoke(this, "\"/sce/rp/session\" response: " + Environment.NewLine + httpResponse.Trim() + Environment.NewLine);
                        Dictionary<string, string> responseHeader = HttpUtils.SplitHttpResponse(httpResponse);
                        responseHeader.TryGetValue("RP-Nonce", out var rpNonce);
                        if (rpNonce == null)
                        {
                            socket.Close();
                        }
                        else
                        {
                            socket.Close();
                            this.HandleControlRequest(rpNonce, ps4Endpoint, ps4RemotePlayData);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                OnPs4ConnectionError?.Invoke(this, "Exception occured by sending /sce/rp/session" + e);
            }
        }

        /*********** control request ***********/

        private void HandleControlRequest(string rpNonce, IPEndPoint ps4Endpoint, PS4RemotePlayData ps4RemotePlayData)
        {
            bool connectedSuccess = false;
            Socket socket = null;
            try
            {
                IPEndPoint ipEndPoint = new IPEndPoint(ps4Endpoint.Address, RpControlPort);
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(ipEndPoint);

                byte[] rpKeyBuffer = HexUtil.Unhexlify(ps4RemotePlayData.RemotePlay.RpKey);
                byte[] rpNonceDecoded = Convert.FromBase64String(rpNonce);
                OnPs4LogInfo?.Invoke(this,
                    "RP-Nonce from \"/sce/rp/session\" response: " + HexUtil.Hexlify(rpNonceDecoded));

                Session session = CryptoService.GetSessionForControl(rpKeyBuffer, rpNonceDecoded);

                string controlAesKey = CryptoService.GetSessionAesKeyForControl(rpKeyBuffer, rpNonceDecoded);
                string controlNonce = CryptoService.GetSessionNonceValueForControl(rpNonceDecoded);
                OnPs4LogInfo?.Invoke(this, "!!! Control AES Key: " + controlAesKey);
                OnPs4LogInfo?.Invoke(this, "!!! Control AES Nonce: " + controlNonce + Environment.NewLine);

                byte[] registrationKeyBuffer = HexUtil.Unhexlify(ps4RemotePlayData.RemotePlay.RegistrationKey);
                byte[] registrationKeyPadding = { 0, 0, 0, 0, 0, 0, 0, 0 };
                byte[] encryptedRegistrationKey =
                    session.Encrypt(ByteUtil.ConcatenateArrays(registrationKeyBuffer, registrationKeyPadding));
                string encodedRegistrationKey = Convert.ToBase64String(encryptedRegistrationKey);

                byte[] randomDid = Guid.NewGuid().ToByteArray();
                byte[] didPrefix = { 0x00, 0x18, 0x00, 0x00, 0x00, 0x07, 0x00, 0x00, 0x80 };
                byte[] didPadding = { 48, 48, 48, 48, 48, 48, 48 };
                byte[] encryptedDid = session.Encrypt(ByteUtil.ConcatenateArrays(didPrefix, randomDid, didPadding));
                string encodedDid = Convert.ToBase64String(encryptedDid);

                string osType = "Win10.0.0";
                byte[] osTypeBuffer = Encoding.UTF8.GetBytes(osType);
                byte[] osTypePadding = { 0 };
                byte[] encryptedOsType = session.Encrypt(ByteUtil.ConcatenateArrays(osTypeBuffer, osTypePadding));
                string encodedOsType = Convert.ToBase64String(encryptedOsType);

                string host = ps4Endpoint.Address + ":" + RpControlPort;

                string requestData = "GET /sce/rp/session/ctrl HTTP/1.1\r\n" +
                                     $"HOST: {host}\r\n" +
                                     "User-Agent: remoteplay Windows\r\n" +
                                     "Connection: keep-alive\r\n" +
                                     "Content-Length: 0\r\n" +
                                     $"RP-Auth: {encodedRegistrationKey}\r\n" +
                                     "RP-Version: 8.0\r\n" +
                                     $"RP-Did: {encodedDid}\r\n" +
                                     "RP-ControllerType: 3\r\n" +
                                     "RP-ClientType: 11\r\n" +
                                     $"RP-OSType: {encodedOsType}\r\n" +
                                     "RP-ConPath: 1\r\n" +
                                     "\r\n";

                socket.Send(Encoding.UTF8.GetBytes(requestData));
                byte[] receiveBuffer = new byte[8192];
                int readBytes = socket.Receive(receiveBuffer);
                byte[] response = new byte[readBytes];
                Buffer.BlockCopy(receiveBuffer, 0, response, 0, response.Length);
                string httpResponse = Encoding.ASCII.GetString(receiveBuffer, 0, readBytes);

                HttpStatusCode statusCode = HttpUtils.GetStatusCode(httpResponse);
                if (statusCode == HttpStatusCode.OK)
                {
                    connectedSuccess = true;
                    OnPs4LogInfo?.Invoke(this, "\"/sce/rp/session/ctrl\" response: " + Environment.NewLine + httpResponse.Trim() + Environment.NewLine);
                    OnPs4LogInfo?.Invoke(this, "TCP connection to PS4 established" + Environment.NewLine);
                    HandleOpenRemotePlayChannel(session, ps4Endpoint);
                    OnPs4ConnectionSuccess?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (Exception e)
            {
                OnPs4ConnectionError?.Invoke(this, "Exception occured /sce/rp/session/ctrl" + e);
            }
            finally
            {
                if (!connectedSuccess)
                {
                    socket?.Close();
                }
            }
        }

        /*********** udp request ***********/

        /***
         * WIP not working yet
         * This is currently really ugly and only dirty protoytpe code like
         * the whole project if you need to enhace it feel free to do it.
         */
        public void HandleOpenRemotePlayChannel(Session session, IPEndPoint ps4Endpoint)
        {
            const int retry = 5;
            Socket udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            udpClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            udpClient.ExclusiveAddressUse = false;
            udpClient.ReceiveTimeout = 5500;
            udpClient.Connect(ps4Endpoint.Address, RpRemotePlayPort);

            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                ControlMessage controlMessage = new ControlMessage((byte)0, 0, 0, 0, (byte)1, (byte)0, 20, 18467, 102400);
                controlMessage.Serialize(binaryWriter);
                byte[] data = memoryStream.ToArray();
                byte[] unknownPayload = HexUtil.Unhexlify("0064006400004823");
                byte[] controlData = ByteUtil.ConcatenateArrays(data, unknownPayload);

                ControlResult controlResult = null;
                for (int i = 1; i <= retry; i++)
                {
                    Task<ControlResult> controlResultFuture = WaitForControlMessage(udpClient, 1, "Packet1");

                    try
                    {
                        udpClient.Send(controlData);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine("Exception occurred while sending udp packets: " + exception);
                    }

                    controlResult = controlResultFuture.Result;
                    if (controlResult.WasSuccessful)
                    {
                        break;
                    }

                    if (i == retry)
                    {
                        return;
                    }
                }

                if (controlResult == null)
                    return;

                ControlMessage answerPacket1 = controlResult.ControlMessages[0];

                /*********** Packet 2 ***********/

                byte[] unParsedPayload = answerPacket1.UnParsedPayload;
                MemoryStream memoryBuffer = new MemoryStream(unParsedPayload);
                memoryBuffer.Position = 8;
                byte[] funcIncrBuffer = new byte[4];
                memoryBuffer.Read(funcIncrBuffer, 0, funcIncrBuffer.Length);
                int funcIncrValue = ByteUtil.ByteArrayToInt(funcIncrBuffer);

                binaryWriter.Flush();
                binaryWriter.Seek(0, SeekOrigin.Begin);
                ControlMessage controlMessage2 = new ControlMessage((byte)0, answerPacket1.FuncIncr, 0, 0, (byte)10, (byte)0, 36, funcIncrValue, answerPacket1.ReceiverId);

                memoryBuffer.Position = 28;
                byte[] lastAnswerPart = new byte[memoryBuffer.Length - memoryBuffer.Position];
                memoryBuffer.Read(lastAnswerPart, 0, lastAnswerPart.Length);
                byte[] funcIncr = ByteUtil.IntToByteArray(answerPacket1.FuncIncr);
                byte[] unknown = ByteUtil.IntToByteArray(102400);

                byte[] unknownPayload2 = ByteUtil.ConcatenateArrays(funcIncr, unknown, funcIncr, lastAnswerPart);

                controlMessage2.Serialize(binaryWriter);
                data = memoryStream.ToArray();
                controlData = ByteUtil.ConcatenateArrays(data, unknownPayload2);

                controlResult = null;
                for (int i = 1; i <= retry; i++)
                {
                    Task<ControlResult> controlResultFuture = WaitForControlMessage(udpClient, 1, "Packet2");

                    try
                    {
                        udpClient.Send(controlData);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine("Exception occurred while sending udp packets: " + exception);
                    }

                    controlResult = controlResultFuture.Result;
                    if (controlResult.WasSuccessful)
                    {
                        break;
                    }

                    if (i == retry)
                    {
                        return;
                    }
                }

                if (controlResult == null)
                    return;

                ControlMessage answerPacket2 = controlResult.ControlMessages[0];

                /*************** Message 3 Big Payload *******/

                int unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
                string timestampUnix = unixTimestamp.ToString();
                string sessionKey = timestampUnix + "FFDB2Q2CWNQO2RTR7WHNBZPVMXEEHT2TUQ3ETHG7LDVB3WNFDY3KVKDAX2LQTUNT";
                //byte[] sessionKeyPart2 = new byte[64];
                //new Random().NextBytes(sessionKeyPart2);
                //byte[] sessionKeyBuffer = Encoding.ASCII.GetBytes(sessionKey);

                byte[] handshakeKeyBuffer = new byte[16];
                new Random().NextBytes(handshakeKeyBuffer);
                byte[] handshakeKey = session.Encrypt(handshakeKeyBuffer);

                string handshakeKeyValue = Convert.ToBase64String(handshakeKey);
                string launchSpecValues ="{\"sessionId\":\"sessionId4321\",\"streamResolutions\":[{\"resolution\":{\"width\":1280,\"height\":720},\"maxFps\":60,\"score\":10}],\"network\":{\"bwKbpsSent\":10000,\"bwLoss\":0.001000,\"mtu\":1454,\"rtt\":5,\"ports\":[53,2053]},\"slotId\":1,\"appSpecification\":{\"minFps\":60,\"minBandwidth\":0,\"extTitleId\":\"ps3\",\"version\":1,\"timeLimit\":1,\"startTimeout\":100,\"afkTimeout\":100,\"afkTimeoutDisconnect\":100},\"konan\":{\"ps3AccessToken\":\"accessToken\",\"ps3RefreshToken\":\"refreshToken\"},\"requestGameSpecification\":{\"model\":\"bravia_tv\",\"platform\":\"android\",\"audioChannels\":\"5.1\",\"language\":\"sp\",\"acceptButton\":\"X\",\"connectedControllers\":[\"xinput\",\"ds3\",\"ds4\"],\"yuvCoefficient\":\"bt601\",\"videoEncoderProfile\":\"hw4.1\",\"audioEncoderProfile\":\"audio1\"},\"userProfile\":{\"onlineId\":\"psnId\",\"npId\":\"npId\",\"region\":\"US\",\"languagesUsed\":[\"en\",\"jp\"]},\"handshakeKey\":\"" + handshakeKeyValue +  "\"}\u0000";
                byte[] launchSpecBuffer = Encoding.UTF8.GetBytes(launchSpecValues);
                byte[] cryptoBuffer = new byte[launchSpecBuffer.Length];
                cryptoBuffer = session.Encrypt(cryptoBuffer, 0);
                byte[] newLaunchSpec = new byte[launchSpecBuffer.Length];
                for (int j = 0; j < launchSpecBuffer.Length; j++)
                {
                    newLaunchSpec[j] = (byte)(launchSpecBuffer[j] ^ cryptoBuffer[j]);
                }

                string encryptedLaunchSpecs = Convert.ToBase64String(newLaunchSpec);
                byte[] encryptedKeyBuffer = { 0, 0, 0, 0 };

                string ecdhPubKey = "04ba6a85f4a3b697e263bb7bde7da44c892790c30923d04ea7459fe254c7e31092878f0722b36c60eb0d0eef7adfbecd7167731c632d91056a0b903c7d3f0bef78";
                byte[] ecdhPubKeyBuffer = HexUtil.Unhexlify(ecdhPubKey);

                string ecdhSig = "5bad371cdc748528e9d83eab419dd04655942e564e8740a84c17d538d51fbc0d";
                byte[] ecdhSigBuffer = HexUtil.Unhexlify(ecdhSig);

                BigPayload bigPayload = new BigPayload
                {
                    clientVersion = 9,
                    sessionKey = sessionKey,
                    launchSpec = encryptedLaunchSpecs,
                    encryptedKey = encryptedKeyBuffer,
                    ecdhPubKey = ecdhPubKeyBuffer,
                    ecdhSig = ecdhSigBuffer
                };
                TakionMessage takionMessage = new TakionMessage
                {
                    Type = TakionMessage.PayloadType.Big,
                    bigPayload = bigPayload
                };
                

                MemoryStream bigPayloadStream = new MemoryStream();
                Serializer.Serialize(bigPayloadStream, takionMessage);
                byte[] bytes = bigPayloadStream.ToArray();
                binaryWriter.Flush();
                binaryWriter.Seek(0, SeekOrigin.Begin);

                ControlMessage controlMessage3 = new ControlMessage((byte)0, answerPacket1.FuncIncr, 0, 0, (byte)0, (byte)1, 1326, 18467, 65536);
                controlMessage3.UnParsedPayload = ByteUtil.ConcatenateArrays(new byte[1], bytes); // I don't know why I have to add this empty 0 byte here but it seems otherwise there is some missing byte between the Control part and the Takion part
                controlMessage3.Serialize(binaryWriter);

                controlData = memoryStream.ToArray();

                OnPs4LogInfo?.Invoke(this, Environment.NewLine + "Sending big payload:");
                OnPs4LogInfo?.Invoke(this, "ECDH pubkey: " + HexUtil.Hexlify(bigPayload.ecdhPubKey));
                OnPs4LogInfo?.Invoke(this, "ECDH sig: " + HexUtil.Hexlify(bigPayload.ecdhSig));
                OnPs4LogInfo?.Invoke(this, "Session key: " + bigPayload.sessionKey + Environment.NewLine);

                controlResult = null;
                for (int i = 1; i <= retry; i++)
                {
                    Task<ControlResult> controlResultFuture = WaitForControlMessage(udpClient, 2, "Packet3");

                    try
                    {
                        udpClient.Send(controlData);
                    }
                    catch (Exception exception)
                    {
                        Console.WriteLine("Exception occurred while sending udp packets: " + exception);
                    }

                    controlResult = controlResultFuture.Result;
                    if (controlResult.WasSuccessful)
                    {
                        break;
                    }

                    if (i == retry)
                    {
                        return;
                    }
                }

                if (controlResult == null)
                    return;

                ControlMessage answerPacket3 = controlResult.ControlMessages[0];
                ControlMessage bangPayloadControl = controlResult.ControlMessages[1];

                TakionMessage bangPayload = Serializer.Deserialize<TakionMessage>(new MemoryStream(bangPayloadControl.UnParsedPayload));

                OnPs4LogInfo?.Invoke(this, Environment.NewLine + "Received bang payload:");
                OnPs4LogInfo?.Invoke(this, "ECDH pubkey: " + HexUtil.Hexlify(bangPayload.bangPayload.ecdhPubKey));
                OnPs4LogInfo?.Invoke(this, "ECDH sig: " + HexUtil.Hexlify(bangPayload.bangPayload.ecdhSig));
                OnPs4LogInfo?.Invoke(this, "Session key: " + bangPayload.bangPayload.sessionKey);

                /******************* StreamInfoPayload *******/

                string test = "";
            }

        }

        private async Task<ControlResult> WaitForControlMessage(Socket socket, int expectedPackets, string info)
        {
            return await Task.Run(() =>
            {
                try
                {
                    List<ControlMessage> controlMessages = new List<ControlMessage>();
                    for (int i = 0; i < expectedPackets; i++)
                    {
                        byte[] message = new byte[MaxUdpPacketSize];
                        int received = socket.Receive(message);

                        ControlMessage controlMessage = new ControlMessage();
                        using (MemoryStream memoryStream = new MemoryStream(message, 0, received))
                        using (BinaryReader binaryWriter = new BinaryReader(memoryStream))
                        {
                            controlMessage.Deserialize(binaryWriter);
                            controlMessages.Add(controlMessage);
                            OnPs4LogInfo?.Invoke(this, "Received: " + info + "_" + i);
                        }
                    }
                    return new ControlResult(true, controlMessages);
                }
                catch (Exception)
                {
                    // ignore
                }
                return new ControlResult(false);
            });
        }

        /*********************/
        /*** inner methods ***/
        /*********************/

        public class ControlResult
        {
            public bool WasSuccessful { get; }
            public List<ControlMessage> ControlMessages { get; }

            public ControlResult(bool wasSuccessful)
            {
                this.WasSuccessful = wasSuccessful;
                this.ControlMessages = new List<ControlMessage>();
            }

            public ControlResult(bool wasSuccessful, List<ControlMessage> controlMessages)
            {
                this.WasSuccessful = wasSuccessful;
                this.ControlMessages = controlMessages;
            }

            public ControlResult(bool wasSuccessful, ControlMessage controlMessage)
            {
                this.WasSuccessful = wasSuccessful;
                this.ControlMessages = new List<ControlMessage>();
                this.ControlMessages.Add(controlMessage);
            }
        }
    }
}
