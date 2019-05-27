using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ProtoBuf;
using Ps4RemotePlay.Protocol.Crypto;
using Ps4RemotePlay.Protocol.Message;
using Ps4RemotePlay.Util;

namespace Ps4RemotePlay.Protocol.Connection
{
    public class PS4ConnectionService : IDisposable
    {
        private const int RpControlPort = 9295;

        private const int RpRemotePlayPort = 9296;

        private const int RpUnknonwPort = 9297; // it is used by the official ps4 remote play client but I don't know yet how and why it is used (3rd party client is not using this port at all)

        private const int MaxUdpPacketSize = 65_000;

        public EventHandler OnPs4ConnectionSuccess;

        public EventHandler<string> OnPs4Disconnected;

        public EventHandler<string> OnPs4ConnectionError;

        public EventHandler<string> OnPs4LogInfo;

        private IPEndPoint _consoleEp;
        private HttpClient _httpClient;
        private Session _currentSession;

        /************ ping pong variables ************/

        private static readonly byte[] StatusPacket = HexUtil.Unhexlify("0000000001FE0000");

        /************ lock object ************/

        private CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();

        private readonly object _lockObject = new object();

        public PS4ConnectionService(IPEndPoint consoleEp)
        {
            _consoleEp = consoleEp;
            _httpClient = new HttpClient()
            {
                BaseAddress = new Uri($"http://{_consoleEp.Address}:{RpControlPort}/")
            };
            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("remoteplay Windows");
        }

        public async Task ConnectToPS4(IPEndPoint ps4Endpoint, PS4RemotePlayData ps4RemotePlayData)
        {
            await Task.Factory.StartNew(async () =>
            {
                await HandleSessionRequest(ps4Endpoint, ps4RemotePlayData);
            });
        }

        public void CloseConnection()
        {
            lock (_lockObject)
            {
                _cancellationTokenSource?.Cancel();
            }
        }

        public void Dispose()
        {
            lock (_lockObject)
            {
                _currentSession = null;
            }
        }

        /***********************/
        /*** private methods ***/
        /***********************/

        /*********** Session request ***********/

        private async Task HandleSessionRequest(IPEndPoint ps4Endpoint, PS4RemotePlayData ps4RemotePlayData)
        {
            try
            {
                var request = new HttpRequestMessage(HttpMethod.Get, "sce/rp/session");

                request.Headers.Host = $"{ps4Endpoint.Address}:{RpControlPort}";
                request.Headers.ConnectionClose = true;
                request.Content = new ByteArrayContent(new byte[0]);

                // Custom header fields
                request.Headers.Add("RP-Registkey", ps4RemotePlayData.RemotePlay.RegistrationKey);
                request.Headers.Add("RP-Version", "8.0");

                string rpNonce = null;
                using (var response = await _httpClient.SendAsync(request))
                {
                    if (response.StatusCode != HttpStatusCode.OK)
                    {
                        OnPs4ConnectionError?.Invoke(this, $"Invalid response from sending /sce/rp/session, Code: {response.StatusCode}");
                        return;
                    }

                    OnPs4LogInfo?.Invoke(this, "\"/sce/rp/session\" response: " + Environment.NewLine + response.ToString() + Environment.NewLine);
                    rpNonce = response.Headers.GetValues("RP-Nonce").First();
                }

                await this.HandleControlRequest(rpNonce, ps4Endpoint, ps4RemotePlayData);
            }
            catch (Exception)
            {
                // This exception will be thrown
                // ToDo remove
            }

        }

        /*********** Control request ***********/

        private async Task HandleControlRequest(string rpNonce, IPEndPoint ps4Endpoint, PS4RemotePlayData ps4RemotePlayData)
        {
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

            var request = new HttpRequestMessage(HttpMethod.Get, "sce/rp/session/ctrl");

            request.Headers.Host = $"{ps4Endpoint.Address}:{RpControlPort}";
            request.Headers.ConnectionClose = false;
            request.Content = new ByteArrayContent(new byte[0]);

            // Custom header fields
            request.Headers.Add("RP-Auth", encodedRegistrationKey);
            request.Headers.Add("RP-Version", "8.0");
            request.Headers.Add("RP-Did", encodedDid);
            request.Headers.Add("RP-ControllerType", "3");
            request.Headers.Add("RP-ClientType", "11");
            request.Headers.Add("RP-OSType", encodedOsType);
            request.Headers.Add("RP-ConPath", "1");


            using (var response = await _httpClient.SendAsync(request))
            {
                if (response.StatusCode != HttpStatusCode.OK)
                {
                    OnPs4ConnectionError?.Invoke(this, $"Invalid response from sending /sce/rp/session/ctrl, Code: {response.StatusCode}");
                    return;
                }

                OnPs4LogInfo?.Invoke(this, "\"/sce/rp/session/ctrl\" response: " + Environment.NewLine + response.ToString() + Environment.NewLine);
                OnPs4LogInfo?.Invoke(this, "TCP connection to PS4 established" + Environment.NewLine);
            }

            StartKeepAlive($"http://{ps4Endpoint.Address}:{RpControlPort}");

            OnPs4ConnectionSuccess?.Invoke(this, EventArgs.Empty);
            HandleOpenRemotePlayChannel(session, ps4Endpoint);
        }

        /*********** UDP request ***********/

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


            MemoryStream memoryStream = new MemoryStream();
            BinaryWriter binaryWriter = new BinaryWriter(memoryStream);

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

            memoryStream = new MemoryStream();
            binaryWriter = new BinaryWriter(memoryStream);
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

            // Setup ECDH session

            // What does this encryptedKeyBuffer do?
            byte[] encryptedKeyBuffer = { 0, 0, 0, 0 };

            // Generate random handshake key, for ECDH pubkey signature calculation
            byte[] handshakeKey = new byte[16];
            new Random().NextBytes(handshakeKey);

            // Generate ECDH keypair
            var ecdhKeyPair = CryptoService.GenerateEcdhKeyPair();
            // Get public key bytes
            var ownPublicKey = Session.GetPublicKeyBytesFromKeyPair(ecdhKeyPair);
            // Calculate ECDH pubkey signature
            var ecdhSignature = Session.CalculateHMAC(handshakeKey, ownPublicKey);

            int unixTimestamp = (Int32)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;
            string timestampUnix = unixTimestamp.ToString();
            string sessionKey = timestampUnix + "FFDB2Q2CWNQO2RTR7WHNBZPVMXEEHT2TUQ3ETHG7LDVB3WNFDY3KVKDAX2LQTUNT";

            LaunchSpecification launchSpecs = LaunchSpecification.GetStandardSpecs("sessionId123", handshakeKey);

            byte[] launchSpecBuffer = Encoding.UTF8.GetBytes(launchSpecs.Serialize());

            byte[] cryptoBuffer = new byte[launchSpecBuffer.Length];
            cryptoBuffer = session.Encrypt(cryptoBuffer, 0);
            byte[] newLaunchSpec = new byte[launchSpecBuffer.Length];
            for (int j = 0; j < launchSpecBuffer.Length; j++)
            {
                newLaunchSpec[j] = (byte)(launchSpecBuffer[j] ^ cryptoBuffer[j]);
            }

            string encryptedLaunchSpecs = Convert.ToBase64String(newLaunchSpec);

            BigPayload bigPayload = new BigPayload
            {
                clientVersion = 9,
                sessionKey = sessionKey,
                launchSpec = encryptedLaunchSpecs,
                encryptedKey = encryptedKeyBuffer,
                ecdhPubKey = ownPublicKey,
                ecdhSig = ecdhSignature
            };
            TakionMessage takionMessage = new TakionMessage
            {
                Type = TakionMessage.PayloadType.Big,
                bigPayload = bigPayload
            };

            MemoryStream bigPayloadStream = new MemoryStream();
            Serializer.Serialize(bigPayloadStream, takionMessage);
            byte[] bytes = bigPayloadStream.ToArray();
            memoryStream = new MemoryStream();
            binaryWriter = new BinaryWriter(memoryStream);

            ControlMessage controlMessage3 = new ControlMessage((byte)0, answerPacket1.FuncIncr, 0, 0, (byte)0, (byte)1, 1326, 18467, 65536);
            // I don't know why I have to add this empty 0 byte here but it seems otherwise there is some missing byte between the Control part and the Takion part
            // Maybe padding?
            controlMessage3.UnParsedPayload = ByteUtil.ConcatenateArrays(new byte[1], bytes);
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

            /* Derive ECDH shared secret */
            var foreignPubkeyParams = Session.ConvertPubkeyBytesToCipherParams(bangPayload.bangPayload.ecdhPubKey);
            var sharedSecret = Session.GenerateSharedSecret(ecdhKeyPair.Private, foreignPubkeyParams);
            OnPs4LogInfo?.Invoke(this, "SHARED SECRET: " + HexUtil.Hexlify(sharedSecret));

            /******************* StreamInfoPayload *******/

            memoryStream = new MemoryStream();
            binaryWriter = new BinaryWriter(memoryStream);
            ControlMessage controlMessage4 = new ControlMessage((byte)0, bangPayloadControl.FuncIncr, 0, 0, (byte)3, (byte)0, 16, bangPayloadControl.FuncIncr, 102400);
            controlMessage4.UnParsedPayload = HexUtil.Unhexlify("00000000");
            controlMessage4.Serialize(binaryWriter);

            controlData = memoryStream.ToArray();

            controlResult = null;
            for (int i = 1; i <= retry; i++)
            {
                Task<ControlResult> controlResultFuture = WaitForControlMessage(udpClient, 1, "Packet4");

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

            ControlMessage streamInfoControl = controlResult.ControlMessages[0];
            TakionMessage streamInfoPayload = Serializer.Deserialize<TakionMessage>(new MemoryStream(streamInfoControl.UnParsedPayload));
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

        /*********** Ping Pong handling ***********/

        /// <summary>
        /// Receives the ping messages and sends pong messages.
        /// </summary>
        /// <param name="result">The ping ping async result.</param>
        public void StartKeepAlive(string url)
        {
            Task.Run(() =>
            {
                while (!_cancellationTokenSource.IsCancellationRequested)
                {
                    try
                    {
                        var request = HttpWebRequest.Create(url);
                        request.Timeout = 3000; // milliseconds

                        request.GetRequestStream().Write(StatusPacket, 0, StatusPacket.Length);

                        var response = request.GetResponse();
                    }
                    catch (TimeoutException)
                    {
                        OnPs4Disconnected?.Invoke(this, "PS4 disconnected. Ping Pong socket exception.");
                    }
                }
            }, _cancellationTokenSource.Token);
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

            public ControlResult(bool wasSuccessful, ControlMessage controlMessage)
            {
                this.WasSuccessful = wasSuccessful;
                this.ControlMessages = new List<ControlMessage>();
                this.ControlMessages.Add(controlMessage);
            }
        }
    }
}
