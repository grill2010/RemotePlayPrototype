using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Org.BouncyCastle.Crypto;
using ProtoBuf;
using Ps4RemotePlay.Protocol.Crypto;
using Ps4RemotePlay.Protocol.Message;
using Ps4RemotePlay.Util;

namespace Ps4RemotePlay.Protocol.Handler
{
    public class RemotePlayHandler : IDisposable
    {
        private readonly Session session;

        private readonly Socket _udpClient;

        private readonly Thread _remotePlayHandlerThread;

        private const int RemotePlayPort = 9296;

        private const int MaxUdpPacketSize = 65_000;

        private ConnectionContext _connectionContext = new ConnectionContext();

        private bool _running = true;

        public RemotePlayHandler(Session session, IPEndPoint ps4Endpoint)
        {
            this.session = session;
            _udpClient = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _udpClient.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            _udpClient.ExclusiveAddressUse = false;
            _udpClient.ReceiveTimeout = 15000;
            _udpClient.Connect(ps4Endpoint.Address, RemotePlayPort);

            _remotePlayHandlerThread = new Thread(HandleRemotePlay)
            {
                IsBackground = true
            };
        }

        public void Dispose()
        {
            _udpClient.Dispose();
            _udpClient.Close();

            _remotePlayHandlerThread.Abort();
            if (_remotePlayHandlerThread.Join(1000))
            {
                _remotePlayHandlerThread.Interrupt();
            }

            _running = false;

        }

        public void StartRemotePlay()
        {
            try
            {
                _connectionContext = new ConnectionContext();
                MemoryStream memoryStream = new MemoryStream();
                BinaryWriter binaryWriter = new BinaryWriter(memoryStream);

                ControlMessage controlMessage =
                    new ControlMessage((byte)0, 0, 0, 0, (byte)1, (byte)0, 20, 18467, 102400);
                controlMessage.Serialize(binaryWriter);
                byte[] data = memoryStream.ToArray();
                byte[] unknownPayload = HexUtil.Unhexlify("0064006400004823");
                byte[] controlData = ByteUtil.ConcatenateArrays(data, unknownPayload);

                _udpClient.Send(controlData);
            }
            catch (Exception )
            {
                // ToDo handle;
            }
        }

        /***********************/
        /*** private methods ***/
        /***********************/

        private void HandleRemotePlay()
        {
            try
            {
                do
                {
                    byte[] message = new byte[MaxUdpPacketSize];
                    int received = _udpClient.Receive(message);
                    if (received > 0)
                    {
                        byte type = message[0];
                        switch (type)
                        {
                            case 0: // Control
                                ControlMessage controlMessage = new ControlMessage();
                                using (MemoryStream memoryStream = new MemoryStream(message, 0, received))
                                using (BinaryReader binaryWriter = new BinaryReader(memoryStream))
                                {
                                    controlMessage.Deserialize(binaryWriter);
                                }

                                HandleControlMessage(controlMessage);
                                break;
                            case 2: // Audio

                                break;
                            case 3: // Video

                                break;
                        }
                    }

                } while (_running);
            }
            catch (ThreadAbortException)
            {
                // Ignore
            }
            catch (ObjectDisposedException)
            {
                // Socket got closed from outside
            }
            catch (Exception)
            {
                // ToDo handle;
            }
        }

        private void HandleControlMessage(ControlMessage controlMessage)
        {
            switch (controlMessage.Flag1)
            {
                case 2:
                    byte[] unParsedPayload = controlMessage.UnParsedPayload;
                    MemoryStream memoryBuffer = new MemoryStream(unParsedPayload);
                    memoryBuffer.Position = 8;
                    byte[] funcIncrBuffer = new byte[4];
                    memoryBuffer.Read(funcIncrBuffer, 0, funcIncrBuffer.Length);
                    int funcIncrValue = ByteUtil.ByteArrayToInt(funcIncrBuffer);

                    MemoryStream memoryStream = new MemoryStream();
                    BinaryWriter binaryWriter = new BinaryWriter(memoryStream);
                    ControlMessage controlMessage2 = new ControlMessage((byte)0, controlMessage.FuncIncr, 0, 0, (byte)10, (byte)0, 36, funcIncrValue, controlMessage.ReceiverId);

                    memoryBuffer.Position = 28;
                    byte[] lastAnswerPart = new byte[memoryBuffer.Length - memoryBuffer.Position];
                    memoryBuffer.Read(lastAnswerPart, 0, lastAnswerPart.Length);
                    byte[] funcIncr = ByteUtil.IntToByteArray(controlMessage.FuncIncr);
                    byte[] unknown = ByteUtil.IntToByteArray(102400);

                    byte[] unknownPayload2 = ByteUtil.ConcatenateArrays(funcIncr, unknown, funcIncr, lastAnswerPart);

                    controlMessage2.Serialize(binaryWriter);
                    byte[] data = memoryStream.ToArray();
                    byte[] controlData = ByteUtil.ConcatenateArrays(data, unknownPayload2);

                    _udpClient.Send(controlData);
                    _connectionContext._cachedControlMessage = controlMessage;
                    break;
                case 11:
                    ControlMessage lastCachedControlMessage = _connectionContext._cachedControlMessage;
                    if (lastCachedControlMessage == null || lastCachedControlMessage.Flag1 != 2)
                    {
                        return;
                    }
                    // Not yet clear what the encrypted key buffer is used for. Seems not be used in this context
                    byte[] encryptedKeyBuffer = { 0, 0, 0, 0 };

                    // Generate random handshake key, for ECDH pubkey signature calculation
                    byte[] handshakeKey = new byte[16];
                    new Random().NextBytes(handshakeKey);

                    // Generate ECDH keypair
                    _connectionContext._ecdhKeyPair = CryptoService.GenerateEcdhKeyPair();
                    // Get public key bytes
                    var ownPublicKey = Session.GetPublicKeyBytesFromKeyPair(_connectionContext._ecdhKeyPair);
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

                    ControlMessage controlMessage3 = new ControlMessage((byte)0, lastCachedControlMessage.FuncIncr, 0, 0, (byte)0, (byte)1, 1326, 18467, 65536);
                    // Add padding byte
                    controlMessage3.UnParsedPayload = ByteUtil.ConcatenateArrays(new byte[1], bytes);
                    controlMessage3.Serialize(binaryWriter);

                    controlData = memoryStream.ToArray();

                    /*OnPs4LogInfo?.Invoke(this, Environment.NewLine + "Sending big payload:");
                    OnPs4LogInfo?.Invoke(this, "ECDH pubkey: " + HexUtil.Hexlify(bigPayload.ecdhPubKey));
                    OnPs4LogInfo?.Invoke(this, "ECDH sig: " + HexUtil.Hexlify(bigPayload.ecdhSig));
                    OnPs4LogInfo?.Invoke(this, "Session key: " + bigPayload.sessionKey + Environment.NewLine);*/

                    _udpClient.Send(controlData);
                    break;
                case 0:
                    if (controlMessage.ProtoBuffFlag == 1)
                    {
                        TakionMessage payload = Serializer.Deserialize<TakionMessage>(new MemoryStream(controlMessage.UnParsedPayload));
                        if (payload.bangPayload != null)
                        {
                            /*OnPs4LogInfo?.Invoke(this, Environment.NewLine + "Received bang payload:");
                            OnPs4LogInfo?.Invoke(this, "ECDH pubkey: " + HexUtil.Hexlify(bangPayload.bangPayload.ecdhPubKey));
                            OnPs4LogInfo?.Invoke(this, "ECDH sig: " + HexUtil.Hexlify(bangPayload.bangPayload.ecdhSig));
                            OnPs4LogInfo?.Invoke(this, "Session key: " + bangPayload.bangPayload.sessionKey);*/

                            /* Derive ECDH shared secret */
                            var foreignPubkeyParams = Session.ConvertPubkeyBytesToCipherParams(payload.bangPayload.ecdhPubKey);
                            _connectionContext._sharedSecret = Session.GenerateSharedSecret(_connectionContext._ecdhKeyPair.Private, foreignPubkeyParams);
                            //OnPs4LogInfo?.Invoke(this, "SHARED SECRET: " + HexUtil.Hexlify(sharedSecret));
                        }

                    }
                    break;
            }
        }

        /*********************/
        /*** inner classes ***/
        /*********************/

        public class ConnectionContext
        {
            public AsymmetricCipherKeyPair _ecdhKeyPair { get; set; }

            public byte[] _sharedSecret { get; set; }

            public ControlMessage _cachedControlMessage { get; set; }
        } 
    }
}
