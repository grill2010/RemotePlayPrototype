using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using ProtoBuf;
using Ps4RemotePlay.Protocol.Crypto;
using Ps4RemotePlay.Protocol.Message;
using Ps4RemotePlay.Util;
using Timer = System.Timers.Timer;

namespace Ps4RemotePlay.Protocol.Connection
{
    public class PS4ConnectionService : IDisposable
    {
        private const int ControlPort = 9295;

        private const int RemotePlayPort = 9296;

        private const int UnknonwPort = 9297; // Senkusha port for determine the mtu and rtt

        private const int MaxUdpPacketSize = 65_000;

        public EventHandler OnPs4ConnectionSuccess;

        public EventHandler<string> OnPs4Disconnected;

        public EventHandler<string> OnPs4ConnectionError;

        public EventHandler<string> OnPs4LogInfo;

        private Socket _clientSocket;

        private Socket _udpClient;

        /************ ping pong variables ************/

        private readonly Timer _timeoutTimer;

        private const int PingPongTimeout = 30000;

        // 4 bytes = payload size
        // 2 bytes = 1FE = control message type
        // 2 bytes = padding
        private static readonly byte[] StatusPacket = HexUtil.Unhexlify("0000000001FE0000"); 

        /************ lock object ************/

        private readonly object _lockObject = new object();

        public PS4ConnectionService()
        {
            _timeoutTimer = new Timer();
            _timeoutTimer.Elapsed += PingPongTimeoutTimer;
            _timeoutTimer.Interval = PingPongTimeout;
        }

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

        public void CloseConnection()
        {
            lock (_lockObject)
            {
                _clientSocket?.Close();
                _clientSocket?.Dispose();

                _udpClient?.Close();
                _udpClient?.Dispose();

                _timeoutTimer?.Stop();
            }
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                _clientSocket?.Close();
                _clientSocket?.Dispose();

                _udpClient?.Close();
                _udpClient?.Dispose();

                _timeoutTimer?.Close();
                _timeoutTimer?.Dispose();
            }
        }

        /***********************/
        /*** private methods ***/
        /***********************/

        /*********** Session request ***********/

        private void HandleSessionRequest(IPEndPoint ps4Endpoint, PS4RemotePlayData ps4RemotePlayData)
        {
            try
            {
                IPEndPoint ipEndPoint = new IPEndPoint(ps4Endpoint.Address, ControlPort);
                using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp))
                {
                    socket.ReceiveTimeout = 5000;
                    socket.Connect(ipEndPoint);

                    string requestData = "GET /sce/rp/session HTTP/1.1\r\n" +
                                         $"HOST: {ps4Endpoint.Address}\r\n" +
                                         "User-Agent: remoteplay Windows\r\n" +
                                         "Connection: close\r\n" +
                                         "Content-Length: 0\r\n" +
                                         $"RP-Registkey: {ps4RemotePlayData.RemotePlay.RegistrationKey}\r\n" +
                                         "RP-Version: 9.0\r\n" +
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
                    else
                    {
                        OnPs4ConnectionError?.Invoke(this, "Connecting to PS4 was not successful, result code was " + statusCode);
                    }
                }
            }
            catch (Exception e)
            {
                OnPs4ConnectionError?.Invoke(this, "Exception occured by sending /sce/rp/session" + e);
            }
        }

        /*********** Control request ***********/

        private void HandleControlRequest(string rpNonce, IPEndPoint ps4Endpoint, PS4RemotePlayData ps4RemotePlayData)
        {
            bool connectedSuccess = false;
            Socket socket = null;
            try
            {
                IPEndPoint ipEndPoint = new IPEndPoint(ps4Endpoint.Address, ControlPort);
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Connect(ipEndPoint);

                byte[] rpKeyBuffer = HexUtil.Unhexlify(ps4RemotePlayData.RemotePlay.RpKey);
                byte[] rpNonceDecoded = Convert.FromBase64String(rpNonce);
                OnPs4LogInfo?.Invoke(this,
                    "RP-Nonce from \"/sce/rp/session\" response: " + HexUtil.Hexlify(rpNonceDecoded));

                Session session = CryptoService.GetSessionForControl(rpKeyBuffer, rpNonceDecoded);

                string controlAesKey = HexUtil.Hexlify(CryptoService.GetSessionAesKeyForControl(rpKeyBuffer, rpNonceDecoded));
                string controlNonce = HexUtil.Hexlify(CryptoService.GetSessionNonceValueForControl(rpNonceDecoded));
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

                string host = ps4Endpoint.Address + ":" + ControlPort;

                string requestData = "GET /sce/rp/session/ctrl HTTP/1.1\r\n" +
                                     $"HOST: {host}\r\n" +
                                     "User-Agent: remoteplay Windows\r\n" +
                                     "Connection: keep-alive\r\n" +
                                     "Content-Length: 0\r\n" +
                                     $"RP-Auth: {encodedRegistrationKey}\r\n" +
                                     "RP-Version: 9.0\r\n" +
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
                    OnPs4LogInfo?.Invoke(this, "\"/sce/rp/session/ctrl\" response: " + Environment.NewLine + httpResponse.Trim() + Environment.NewLine);
                    OnPs4LogInfo?.Invoke(this, "TCP connection to PS4 established" + Environment.NewLine);
                    _clientSocket = socket;
                    _clientSocket.ReceiveTimeout = 0;
                    PingPongAsyncResult connectionStateObject = new PingPongAsyncResult { RemoteSocket = _clientSocket };
                    connectionStateObject.RemoteSocket.BeginReceive(connectionStateObject.Buffer, 0,
                        connectionStateObject.Buffer.Length, SocketFlags.None, PingPongHandler,
                        connectionStateObject);
                    OnPs4ConnectionSuccess?.Invoke(this, EventArgs.Empty);
                    connectedSuccess = true;
                    InitializeRemotePlayChannel(session, ps4Endpoint);
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

        /*********** UDP request ***********/

        public void InitializeRemotePlayChannel(Session session, IPEndPoint ps4Endpoint)
        {
            const int retry = 3;
            for (int i = 0; i < retry; i++)
            {
                Socket udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                udpClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpClient.ExclusiveAddressUse = false;
                udpClient.ReceiveTimeout = 5500;
                udpClient.Connect(ps4Endpoint.Address, RemotePlayPort);

                RemotePlayContext remotePlayContext = SendInitControlMessages(udpClient);
                if (remotePlayContext == null)
                {
                    udpClient.Close();
                    udpClient.Dispose();
                    Thread.Sleep(2500);
                    continue;
                }

                remotePlayContext = SendBigBangMessages(udpClient, session, remotePlayContext);
                if (remotePlayContext == null)
                {
                    udpClient.Close();
                    udpClient.Dispose();
                    Thread.Sleep(2500);
                    continue;
                }

                remotePlayContext = SendStreamInfo(udpClient, session, remotePlayContext);
                if (remotePlayContext == null)
                {
                    udpClient.Close();
                    udpClient.Dispose();
                    Thread.Sleep(2500);
                    continue;
                }

                OnPs4LogInfo?.Invoke(this, "!!!!!!!!!!!!! Stream initialization successfully completed" + Environment.NewLine);

                _udpClient = udpClient;
                RemotePlayAsyncState remotePlayAsyncState = new RemotePlayAsyncState(remotePlayContext, session);
                //HandleFeedbackData(_udpClient, remotePlayContext);
                HandleHeartBeat(_udpClient, remotePlayContext);
                _udpClient.BeginReceive(remotePlayAsyncState.Buffer, 0, remotePlayAsyncState.Buffer.Length, SocketFlags.None, HandleRemotePlayStream, remotePlayAsyncState);

                break;
            }
        }

        /*********** Stream handling ***********/

        private void HandleRemotePlayStream(IAsyncResult result)
        {
            try
            {
                RemotePlayAsyncState remotePlayAsyncState = (RemotePlayAsyncState) result.AsyncState;

                int bytesRead = _udpClient.EndReceive(result);
                if (bytesRead > 0)
                {
                    if (remotePlayAsyncState.Buffer[0] == 0) // Control message
                    {
                        // ToDo check takion_handle_packet_message

                        ControlMessage controlMessage = new ControlMessage();
                        using (MemoryStream memoryStream = new MemoryStream(remotePlayAsyncState.Buffer, 0, bytesRead))
                        using (BinaryReader binaryWriter = new BinaryReader(memoryStream))
                        {
                            controlMessage.Deserialize(binaryWriter);
                            OnPs4LogInfo?.Invoke(this, "Received remote play stream control package, crypto " + controlMessage.Crypto + ", tagPos " + controlMessage.TagPos);
                        }

                        byte[] ackBangPayload = HexUtil.Unhexlify("00000000");
                        ushort ackBangPayloadSize = (ushort)(12 + ackBangPayload.Length);
                        ControlMessage ackControlMessage = new ControlMessage(0, remotePlayAsyncState.RemotePlayContext.ReceiverId, 0, 0,  3, 0, ackBangPayloadSize, controlMessage.FuncIncr, 0x19000);
                        ackControlMessage.UnParsedPayload = ackBangPayload;


                        SendCryptedControlMessage(ackControlMessage, remotePlayAsyncState.RemotePlayContext,_udpClient);
                        //SendData(_udpClient, GetByteArrayForControlMessage(ackControlMessage));

                        /*if (controlMessage.ProtoBuffFlag == 1 && controlMessage.UnParsedPayload.Length > 0 && controlMessage.UnParsedPayload.Length != 7) WIP
                        {
                            TakionMessage takionMessage = Serializer.Deserialize<TakionMessage>(new MemoryStream(controlMessage.UnParsedPayload));
                            if (takionMessage.Type == TakionMessage.PayloadType.Heartbeat)
                            {
                                byte[] heartbeatPayload = HexUtil.Unhexlify("000803");
                                short heartbeatPayloadSize = (short) (12 + heartbeatPayload.Length);
                                ControlMessage heartbeatControlMessage = new ControlMessage(0, remotePlayAsyncState.RemotePlayContext.ReceiverId, new Random().Next(), controlMessage.TagPos + 0x10, 0, 1, heartbeatPayloadSize, controlMessage.FuncIncr, 0x10000);
                                heartbeatControlMessage.UnParsedPayload = heartbeatPayload;

                                SendData(_udpClient, GetByteArrayForControlMessage(heartbeatControlMessage));
                            }
                        }*/
                    }

                    _udpClient.BeginReceive(remotePlayAsyncState.Buffer, 0, remotePlayAsyncState.Buffer.Length, SocketFlags.None, HandleRemotePlayStream, remotePlayAsyncState);
                }
                else
                {
                    // Close connection
                }

            }
            catch (ObjectDisposedException)
            {
                // Ignore closed from outside
            }
            catch (Exception exception)
            {
                OnPs4ConnectionError?.Invoke(this, "Connection error while handling stream data. Exception: " + exception);
            }
        }

        private RemotePlayContext SendInitControlMessages(Socket udpClient)
        {
            /******** Initial control message 1 ********/

            byte[] controlMessage1Payload = HexUtil.Unhexlify("0064006400004823");
            ushort initialControlMessage1PayloadSize = (ushort)(12 + controlMessage1Payload.Length);
            ControlMessage initialMessage1 = new ControlMessage(0, 0, 0, 0, 1, 0, initialControlMessage1PayloadSize, 0x4823, 0x19000);
            initialMessage1.UnParsedPayload = controlMessage1Payload;
            byte[] initialControlMessage1Data = GetByteArrayForControlMessage(initialMessage1);
            ControlResult initControlResult1 = SendControlDataAndWaitForAnswer(udpClient, initialControlMessage1Data, 1, "Send Init Control message 1");

            if (!initControlResult1.WasSuccessful)
                return null;

            ControlMessage initialAnswer1 = initControlResult1.ControlMessages[0];

            /******** Initial control message 2 ********/

            byte[] initialAnswer1Payload = initialAnswer1.UnParsedPayload;
            MemoryStream memoryBuffer = new MemoryStream(initialAnswer1Payload) {Position = 8};
            byte[] funcIncrBuffer = new byte[4];
            memoryBuffer.Read(funcIncrBuffer, 0, funcIncrBuffer.Length);
            uint funcIncrValue = ByteUtil.ByteArrayToUInt(funcIncrBuffer);

            memoryBuffer.Position = 28;
            byte[] lastAnswerPart = new byte[memoryBuffer.Length - memoryBuffer.Position];
            memoryBuffer.Read(lastAnswerPart, 0, lastAnswerPart.Length);
            byte[] funcIncr = ByteUtil.UIntToByteArray(initialAnswer1.FuncIncr);
            byte[] classValue = ByteUtil.UIntToByteArray(initialAnswer1.ClassValue);

            byte[] controlMessage2Payload = ByteUtil.ConcatenateArrays(funcIncr, classValue, funcIncr, lastAnswerPart);
            ushort initialControlMessage2PayloadSize = (ushort)(12 + controlMessage2Payload.Length);

            ControlMessage controlMessage2 = new ControlMessage(0, initialAnswer1.FuncIncr, 0, 0, 10, 0, initialControlMessage2PayloadSize, funcIncrValue, initialAnswer1.ReceiverId);
            controlMessage2.UnParsedPayload = controlMessage2Payload;
            byte[] initialControlMessage2Data = GetByteArrayForControlMessage(controlMessage2);

            ControlResult initControlResult2 = SendControlDataAndWaitForAnswer(udpClient, initialControlMessage2Data, 1, "Send Init Control message 2");

            if (!initControlResult2.WasSuccessful)
                return null;

            RemotePlayContext remotePlayContext = new RemotePlayContext()
            {
                ReceiverId = initialAnswer1.FuncIncr,
                FuncIncr = initialMessage1.FuncIncr
            };
            return remotePlayContext;
        }

        private RemotePlayContext SendBigBangMessages(Socket udpClient, Session session, RemotePlayContext remotePlayContext)
        {
            /******** Big Payload send ********/

            // Generate random handshake key, for ECDH pubkey signature calculation
            byte[] handshakeKey = new byte[16];
            new Random().NextBytes(handshakeKey);

            // Generate ECDH keypair
            var ecdhKeyPair = CryptoService.GenerateEcdhKeyPair();
            // Get public key bytes
            var ownPublicKey = Session.GetPublicKeyBytesFromKeyPair(ecdhKeyPair);

            // Calculate ECDH pubkey signature
            var ecdhSignature = Session.CalculateHMAC(handshakeKey, ownPublicKey);

            int unixTimestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            string timestampUnix = unixTimestamp.ToString();
            string sessionKey = timestampUnix + CryptoService.GetUniqueKey(64);

            LaunchSpecification launchSpecs = LaunchSpecification.GetStandardSpecs("sessionId123", handshakeKey);
            byte[] launchSpecBuffer = Encoding.UTF8.GetBytes(launchSpecs.Serialize());

            byte[] cryptoBuffer = new byte[launchSpecBuffer.Length];
            cryptoBuffer = session.Encrypt(cryptoBuffer, 0);
            byte[] newLaunchSpec = new byte[launchSpecBuffer.Length];
            for (int i = 0; i < launchSpecBuffer.Length; i++)
            {
                newLaunchSpec[i] = (byte)(launchSpecBuffer[i] ^ cryptoBuffer[i]);
            }

            TakionMessage takionBigPayloadMessage = new TakionMessage
            {
                Type = TakionMessage.PayloadType.Big,
                bigPayload = new BigPayload
                {
                    clientVersion = 9,
                    sessionKey = sessionKey,
                    launchSpec = Convert.ToBase64String(newLaunchSpec),
                    encryptedKey = new byte[] { 0, 0, 0, 0 },
                    ecdhPubKey = ownPublicKey,
                    ecdhSig = ecdhSignature
                }
            };

            MemoryStream bigPayloadStream = new MemoryStream();
            Serializer.Serialize(bigPayloadStream, takionBigPayloadMessage);
            byte[] bigPayloadBuffer = ByteUtil.ConcatenateArrays(new byte[1], bigPayloadStream.ToArray()); // Padding byte + BigPayload
            ushort bigPayloadSize = (ushort) (12 + bigPayloadBuffer.Length);

            ControlMessage controlMessageBigPayload = new ControlMessage(0, remotePlayContext.ReceiverId, 0, 0, 0, 1, bigPayloadSize, remotePlayContext.FuncIncr, 0x10000);
            controlMessageBigPayload.UnParsedPayload = bigPayloadBuffer;
            byte[] initialControlMessage2Data = GetByteArrayForControlMessage(controlMessageBigPayload);

            OnPs4LogInfo?.Invoke(this, Environment.NewLine + "Sending big payload:");
            OnPs4LogInfo?.Invoke(this, "ECDH pubkey: " + HexUtil.Hexlify(takionBigPayloadMessage.bigPayload.ecdhPubKey));
            OnPs4LogInfo?.Invoke(this, "ECDH sig: " + HexUtil.Hexlify(takionBigPayloadMessage.bigPayload.ecdhSig));
            OnPs4LogInfo?.Invoke(this, "Session key: " + takionBigPayloadMessage.bigPayload.sessionKey + Environment.NewLine);

            ControlResult bigPayloadResult = SendControlDataAndWaitForAnswer(udpClient, initialControlMessage2Data, 2, "Send BigPayload");

            if (!bigPayloadResult.WasSuccessful)
                return null;

            /******** Bang Payload receive ********/

            ControlMessage answerPacket1 = bigPayloadResult.ControlMessages[0];
            ControlMessage answerPacket2 = bigPayloadResult.ControlMessages[1];

            if (answerPacket1.ProtoBuffFlag != 1 && answerPacket2.ProtoBuffFlag != 1)
                return null;

            TakionMessage bangPayload = answerPacket1.ProtoBuffFlag == 1 ?
                Serializer.Deserialize<TakionMessage>(new MemoryStream(answerPacket1.UnParsedPayload)) :
                Serializer.Deserialize<TakionMessage>(new MemoryStream(answerPacket2.UnParsedPayload));
            if (bangPayload.bangPayload == null)
                return null;

            ControlMessage bangPayloadControl = answerPacket1.ProtoBuffFlag == 1 ? answerPacket1 : answerPacket2;

            OnPs4LogInfo?.Invoke(this, Environment.NewLine + "Received bang payload:");
            OnPs4LogInfo?.Invoke(this, "ECDH pubkey: " + HexUtil.Hexlify(bangPayload.bangPayload.ecdhPubKey));
            OnPs4LogInfo?.Invoke(this, "ECDH sig: " + HexUtil.Hexlify(bangPayload.bangPayload.ecdhSig));
            OnPs4LogInfo?.Invoke(this, "Session key: " + bangPayload.bangPayload.sessionKey);

            /* Derive ECDH shared secret */
            var foreignPubkeyParams = Session.ConvertPubkeyBytesToCipherParams(bangPayload.bangPayload.ecdhPubKey);
            remotePlayContext.SharedSecret = Session.GenerateSharedSecret(ecdhKeyPair.Private, foreignPubkeyParams);
            remotePlayContext.LocalGmacInfo = CryptoService.SetUpGmac(2, handshakeKey, remotePlayContext.SharedSecret);
            remotePlayContext.RemoteGmacInfo = CryptoService.SetUpGmac(3, handshakeKey, remotePlayContext.SharedSecret);
            OnPs4LogInfo?.Invoke(this, "HANDSHAKE KEY: " + HexUtil.Hexlify(handshakeKey));
            OnPs4LogInfo?.Invoke(this, "SHARED SECRET: " + HexUtil.Hexlify(remotePlayContext.SharedSecret));

            byte[] ackBangPayload = HexUtil.Unhexlify("00000000");
            ushort ackBangPayloadSize = (ushort) (12 + ackBangPayload.Length);
            ControlMessage ackBangPayloadMessage = new ControlMessage(0, bangPayloadControl.FuncIncr, 0, 0, 3, 0, ackBangPayloadSize, bangPayloadControl.FuncIncr, 0x19000);
            ackBangPayloadMessage.UnParsedPayload = ackBangPayload;
            byte[] ackBangPayloadMessageData = GetByteArrayForControlMessage(ackBangPayloadMessage);
            remotePlayContext.LastSentMessage = ackBangPayloadMessageData;

            SendData(udpClient, ackBangPayloadMessageData);

            return remotePlayContext;
        }

        private RemotePlayContext SendStreamInfo(Socket udpClient, Session session, RemotePlayContext remotePlayContext)
        {
            /******** Stream info receive ********/

            ControlResult resolutionInfoPayload = WaitForControlMessage(udpClient, 1, "Wait for stream info payload").Result;
            if (!resolutionInfoPayload.WasSuccessful)
            {
                if (remotePlayContext.LastSentMessage.Length > 0)
                {
                    resolutionInfoPayload = SendControlDataAndWaitForAnswer(udpClient, remotePlayContext.LastSentMessage, 1, "Resend bang ack control message and wait for stream info payload");
                    if (!resolutionInfoPayload.WasSuccessful)
                        return null;
                }
                else
                    return null;
            }

            ControlMessage resolutionInfoControlMessage = resolutionInfoPayload.ControlMessages[0];
            TakionMessage resolutionPayload = Serializer.Deserialize<TakionMessage>(new MemoryStream(resolutionInfoControlMessage.UnParsedPayload));
            if (resolutionPayload.streamInfoPayload == null)
                return null;

            remotePlayContext.StreamInfoPayload = resolutionPayload.streamInfoPayload;


            byte[] ackResolutionInfoPayload = HexUtil.Unhexlify("00000000");
            ushort ackResolutionInfoSize = (ushort)(12 + ackResolutionInfoPayload.Length);
            ControlMessage ackResolutionInfoControlMessage = new ControlMessage((byte)0, remotePlayContext.ReceiverId, 0, 0, 3, 0, ackResolutionInfoSize, resolutionInfoControlMessage.FuncIncr, 0x19000);
            ackResolutionInfoControlMessage.UnParsedPayload = ackResolutionInfoPayload;

            byte[] ackResolutionInfoControlMessageData = GetByteArrayForControlMessage(ackResolutionInfoControlMessage);
            remotePlayContext.LastSentMessage = ackResolutionInfoControlMessageData;

            SendCryptedControlMessage(ackResolutionInfoControlMessage, remotePlayContext, udpClient); // ToDo handle when there is no further response and and resolution info is sent again
            //SendData(udpClient, ackResolutionInfoControlMessageData);

            /******** Stream info ack send ********/

            remotePlayContext.LastSentMessage = new byte[0];
            byte[] streamInfoAckPayload = HexUtil.Unhexlify("00080e");
            ushort streamInfoAckPayloadSize = (ushort) (12 + streamInfoAckPayload.Length);

            ControlMessage streamInfoAckControlMessage = new ControlMessage((byte)0, remotePlayContext.ReceiverId, 0, 0, 0, 1, streamInfoAckPayloadSize, remotePlayContext.FuncIncr, 0x90000);
            streamInfoAckControlMessage.UnParsedPayload = streamInfoAckPayload;

            ControlResult controlResult = SendCryptedControlMessageAndWaitForAnswer(streamInfoAckControlMessage, remotePlayContext, udpClient, 1, "Sending stream info ack message" + Environment.NewLine);
            if (!controlResult.WasSuccessful)
            {
                const int retry = 0;
                for (int i = 0; i < retry; i++)
                {
                    SendData(udpClient, remotePlayContext.LastSentMessage);
                    controlResult = SendCryptedControlMessageAndWaitForAnswer(streamInfoAckControlMessage, remotePlayContext, udpClient, 1, "Resend stream info ack message" + Environment.NewLine);
                    if (controlResult.WasSuccessful)
                    {
                        break;
                    }
                }
            }

            if (!controlResult.WasSuccessful)
                return null;

            return remotePlayContext;
        }

        private void SendCryptedFeedbackMessage(FeedbackMessage feedbackMessage, byte[] feebdackBuffer, RemotePlayContext remotePlayContext, Socket udpClient)
        {
            feedbackMessage.TagPos = (uint)remotePlayContext.LocalKeyPosition;
            byte[] feedbackMessageData = GetByteArrayForFeedbackMessage(feedbackMessage);
            int macOffset = GetMacOffset(6); // crypto position
            CryptoService.AddGmacToBuffer(remotePlayContext.LocalGmacInfo, feedbackMessage.TagPos, feedbackMessageData, macOffset);
            feebdackBuffer = CryptoService.EncryptGmacMessage(remotePlayContext.LocalGmacInfo, feedbackMessage.TagPos, feebdackBuffer);
            remotePlayContext.LocalKeyPosition += 16; //controlMessage.UnParsedPayload.Length < 16 ? 16 : controlMessage.UnParsedPayload.Length;

            SendData(udpClient, ByteUtil.ConcatenateArrays(feedbackMessageData, feebdackBuffer));
        }

        private void SendCryptedControlMessage(ControlMessage controlMessage, RemotePlayContext remotePlayContext, Socket udpClient)
        {
            controlMessage.TagPos = 0;
            byte[] controlMessageData = GetByteArrayForControlMessage(controlMessage);
            int macOffset = GetMacOffset(0); // crypto position
            CryptoService.AddGmacToBuffer(remotePlayContext.LocalGmacInfo, remotePlayContext.LocalKeyPosition, controlMessageData, macOffset);

            byte[] keyPosBuffer = ByteUtil.UIntToByteArray(remotePlayContext.LocalKeyPosition);
            int position = GetTagPosOffset(0);
            for (int i = 0; i < keyPosBuffer.Length; i++)
            {
                controlMessageData[position + i] = keyPosBuffer[i];
            }

            remotePlayContext.LocalKeyPosition += 16; //controlMessage.UnParsedPayload.Length < 16 ? 16 : controlMessage.UnParsedPayload.Length;

            SendData(udpClient, controlMessageData);
        }

        private ControlResult SendCryptedControlMessageAndWaitForAnswer(ControlMessage controlMessage, RemotePlayContext remotePlayContext, Socket udpClient, int expectedPackets, string info)
        {
            controlMessage.TagPos = 0;
            byte[] controlMessageData = GetByteArrayForControlMessage(controlMessage);
            int macOffset = GetMacOffset(0); // crypto position
            CryptoService.AddGmacToBuffer(remotePlayContext.LocalGmacInfo, remotePlayContext.LocalKeyPosition, controlMessageData, macOffset);

            byte[] keyPosBuffer = ByteUtil.UIntToByteArray(remotePlayContext.LocalKeyPosition);
            int position = GetTagPosOffset(0);
            for (int i = 0; i < keyPosBuffer.Length; i++)
            {
                controlMessageData[position + i] = keyPosBuffer[i];
            }

            remotePlayContext.LocalKeyPosition += 16; // controlMessage.UnParsedPayload.Length < 16 ? 16 : controlMessage.UnParsedPayload.Length;

            return SendControlDataAndWaitForAnswer(udpClient, controlMessageData, expectedPackets, info);
        }

        private ControlResult SendControlDataAndWaitForAnswer(Socket udpClient, byte[] data, int expectedPackets, string info)
        {
            const int retry = 3;
            ControlResult controlResult = null;
            for (int i = 1; i <= retry; i++)
            {
                Task<ControlResult> controlResultFuture = WaitForControlMessage(udpClient, expectedPackets, info);

                SendData(udpClient, data);

                controlResult = controlResultFuture.Result;
                if (controlResult.WasSuccessful)
                {
                    break;
                }
            }

            return controlResult;
        }

        private void SendData(Socket udpClient, byte[] data)
        {
            try
            {
                udpClient.Send(data);
            }
            catch (Exception exception)
            {
                Console.WriteLine("Exception occurred while sending udp packets: " + exception);
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

                        if (received > 0 && message[0] == 0)
                        {
                            ControlMessage controlMessage = new ControlMessage();
                            using (MemoryStream memoryStream = new MemoryStream(message, 0, received))
                            using (BinaryReader binaryWriter = new BinaryReader(memoryStream))
                            {
                                controlMessage.Deserialize(binaryWriter);
                                controlMessages.Add(controlMessage);
                                OnPs4LogInfo?.Invoke(this, "Received: " + info + "_" + i);
                            }
                        }
                        else
                        {
                            --i;
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

        private int GetMacOffset(int type)
        {
            switch (type)
            {
                case 0: // Control message
                    return 5;
                case 2: // Video
                case 3: //Audio
                    return 0xa;
                case 6: //Feedback
                    return 8;
                default:
                    return -1;
            }
        }

        private int GetTagPosOffset(int type)
        {
            switch (type)
            {
                case 0: // Control message
                    return 9;
                case 2: // Video
                case 3: //Audio
                    return 0xe;
                default:
                    return -1;
            }
        }

        private byte[] GetByteArrayForControlMessage(ControlMessage controlMessage)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                controlMessage.Serialize(binaryWriter);
                return ((MemoryStream) binaryWriter.BaseStream).ToArray();
            }
        }

        private byte[] GetByteArrayForFeedbackMessage(FeedbackMessage feedbackMessage)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            using (BinaryWriter binaryWriter = new BinaryWriter(memoryStream))
            {
                feedbackMessage.Serialize(binaryWriter);
                return ((MemoryStream)binaryWriter.BaseStream).ToArray();
            }
        }

        /*********** Ping Pong handling ***********/

        /// <summary>
        /// Receives the ping messages and sends pong messages.
        /// </summary>
        /// <param name="result">The ping ping async result.</param>
        public void PingPongHandler(IAsyncResult result)
        {
            PingPongAsyncResult state = (PingPongAsyncResult)result.AsyncState;
            Socket remoteSocket = state.RemoteSocket;
            try
            {
                _timeoutTimer.Stop();
                int bytesRead = remoteSocket.EndReceive(result);
                if (bytesRead > 0)
                {
                    byte[] pingPacketBuffer = new byte[bytesRead];
                    Buffer.BlockCopy(state.Buffer, 0, pingPacketBuffer, 0, pingPacketBuffer.Length);

                    remoteSocket.Send(StatusPacket, StatusPacket.Length, SocketFlags.None);

                    _timeoutTimer.Start();
                    remoteSocket.BeginReceive(state.Buffer, 0, state.Buffer.Length, SocketFlags.None, PingPongHandler,
                        state);
                }
                else
                {
                    Thread.Sleep(250);
                    CloseConnection();
                    OnPs4Disconnected?.Invoke(this, "PS4 disconnected. Ping Pong socket got closed.");
                }
            }
            catch (SocketException)
            {
                if (remoteSocket != null)
                {
                    CloseConnection();
                    OnPs4Disconnected?.Invoke(this, "PS4 disconnected. Ping Pong socket exception.");
                }
            }
            catch (ObjectDisposedException) // When the socket gets closed from outside (session already removed)
            {
            }
            catch (Exception e)
            {
                OnPs4Disconnected?.Invoke(this, "PS4 disconnected. Unknown reason: " + e);
            }
        }

        private void PingPongTimeoutTimer(object sender, ElapsedEventArgs e)
        {
            CloseConnection();
            OnPs4Disconnected?.Invoke(this, "PS4 disconnected. Ping Pong timeout occurred.");
        }

        /*********** heartbeat handling ***********/

        private void HandleHeartBeat(Socket udpSocket, RemotePlayContext remotePlayContext)
        {
            Thread heartBeatThread = new Thread(() =>
            {
                try
                {
                    do
                    {
                        Thread.Sleep(1000);
                        byte[] heartbeatPayload = HexUtil.Unhexlify("000803");
                        ushort heartbeatPayloadSize = (ushort)(12 + heartbeatPayload.Length);
                        ControlMessage heartbeat = new ControlMessage((byte)0, remotePlayContext.ReceiverId, 0, 0, 0, 1, heartbeatPayloadSize, remotePlayContext.FuncIncr, 0x10000);
                        heartbeat.UnParsedPayload = heartbeatPayload;

                        SendCryptedControlMessage(heartbeat, remotePlayContext, udpSocket);

                    } while (true);
                }
                catch (Exception)
                {
                    // ignore
                }

            });
            heartBeatThread.IsBackground = true;
            heartBeatThread.Start();
        }

        private volatile ushort _feedbackSequenceNumber = 0;

        private void HandleFeedbackData(Socket udpSocket, RemotePlayContext remotePlayContext)
        {
            Thread heartBeatThread = new Thread(() =>
            {
                try
                {
                    do
                    {
                        FeedbackMessage feedbackMessage = new FeedbackMessage(_feedbackSequenceNumber, 0, 0, 0);
                        byte[] buf = new byte[25];
                        buf[0x0] = 0xa0; // TODO
                        buf[0x1] = 0xff; // TODO
                        buf[0x2] = 0x7f; // TODO
                        buf[0x3] = 0xff; // TODO
                        buf[0x4] = 0x7f; // TODO
                        buf[0x5] = 0xff; // TODO
                        buf[0x6] = 0x7f; // TODO
                        buf[0x7] = 0xff; // TODO
                        buf[0x8] = 0x7f; // TODO
                        buf[0x9] = 0x99; // TODO
                        buf[0xa] = 0x99; // TODO
                        buf[0xb] = 0xff; // TODO
                        buf[0xc] = 0x7f; // TODO
                        buf[0xd] = 0xfe; // TODO
                        buf[0xe] = 0xf7; // TODO
                        buf[0xf] = 0xef; // TODO
                        buf[0x10] = 0x1f; // TODO
                        buf[0x11] = 0; // TODO
                        buf[0x12] = 0; // TODO
                        buf[0x13] = 0; // TODO
                        buf[0x14] = 0; // TODO
                        buf[0x15] = 0; // TODO
                        buf[0x16] = 0; // TODO
                        buf[0x17] = 0; // TODO
                        buf[0x18] = 0; // TODO


                        SendCryptedFeedbackMessage(feedbackMessage, buf, remotePlayContext, udpSocket);
                        ++_feedbackSequenceNumber;
                        Thread.Sleep(200);
                    } while (true);
                }
                catch (Exception)
                {
                    // ignore
                }

            });
            heartBeatThread.IsBackground = true;
            heartBeatThread.Start();
        }

        /*********************/
        /*** inner classes ***/
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
        }

        public class RemotePlayContext
        {
            public uint ReceiverId { get; set; }

            private uint _funcIncr;

            public uint FuncIncr
            {
                get
                {
                    var result = _funcIncr;
                    ++_funcIncr;
                    return result;
                }
                internal set => _funcIncr = value;
            }

            public byte[] SharedSecret { get; set; }

            public StreamInfoPayload StreamInfoPayload { get; set; }

            public GmacInfo LocalGmacInfo { get; set; }

            public GmacInfo RemoteGmacInfo { get; set; }

            public uint LocalKeyPosition { get; set; }

            internal byte[] LastSentMessage { get; set; }


            public RemotePlayContext()
            {
                ReceiverId = 0;
                FuncIncr = 0x4823;
                SharedSecret = new byte[0];
                LastSentMessage = new byte[0];
            }
        }

        public class RemotePlayAsyncState
        {
            public RemotePlayContext RemotePlayContext { get; }

            public Session Session { get; set; }

            public byte[] Buffer = new byte[MaxUdpPacketSize];

            public RemotePlayAsyncState(RemotePlayContext remotePlayContext, Session session)
            {
                this.RemotePlayContext = remotePlayContext;
                this.Session = session;
            }
        }

        public class PingPongAsyncResult
        {
            // Size of receive buffer.
            public const int BufferLength = 1024;

            // Receive buffer.
            public readonly byte[] Buffer = new byte[BufferLength];

            // Client  socket.
            public Socket RemoteSocket = null;
        }
    }
}
