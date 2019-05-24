using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using PcapDotNet.Core;
using PcapDotNet.Packets;
using PcapDotNet.Packets.Http;
using PcapDotNet.Packets.IpV4;
using PcapDotNet.Packets.Transport;
using Ps4RemotePlay;
using Ps4RemotePlay.Protocol.Connection;
using Ps4RemotePlay.Protocol.Crypto;
using Ps4RemotePlay.Protocol.Discovery;
using Ps4RemotePlay.Protocol.Message;
using Ps4RemotePlay.Protocol.Model;
using Ps4RemotePlay.Protocol.Registration;
using Ps4RemotePlay.Setting;
using Ps4RemotePlay.Util;
using Tx.Network;
using UdpDatagram = Tx.Network.UdpDatagram;

namespace Ps4RemotePlay.Ui
{
    public partial class Form1 : Form
    {
        private readonly SettingManager _settingManager;
        private readonly PS4RegistrationService _ps4RegistrationService;
        private readonly PS4DiscoveryService _ps4DiscoveryService;
        private readonly PS4ConnectionService _ps4ConnectionService;

        private LivePcapContext _livePcapContext;

        private readonly List<LivePacketDevice> networkAdapters = new List<LivePacketDevice>();

        public Form1()
        {
            InitializeComponent();

            _settingManager = SettingManager.GetInstance();
            _ps4RegistrationService = new PS4RegistrationService();
            _ps4DiscoveryService = new PS4DiscoveryService();
            _ps4ConnectionService = new PS4ConnectionService();
            _livePcapContext = new LivePcapContext();

            _ps4RegistrationService.OnPs4RegisterSuccess += OnPs4RegisterSuccess;
            _ps4RegistrationService.OnPs4RegisterError += OnPs4RegisterError;

            _ps4ConnectionService.OnPs4ConnectionSuccess += OnPs4ConnectionSuccess;
            _ps4ConnectionService.OnPs4Disconnected += OnPs4Disconnected;
            _ps4ConnectionService.OnPs4ConnectionError += OnPs4ConnectionError;
            _ps4ConnectionService.OnPs4LogInfo += OnPs4LogInfo;

            PS4RemotePlayData remotePlayData = _settingManager.GetRemotePlayData();
            if (remotePlayData != null)
            {
                UpdateRegisterInfoTextBox(remotePlayData.RemotePlay.RegisterHeaderInfoComplete);
                EnableConnectButton();
                EnablePcapButton();
                SetUpComboBoxNetworkAdapter();
            }
            else
            {
                MessageBox.Show("Please register firts with your PS4 in order to use all features", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        /*********************/
        /*** event handler ***/
        /*********************/

        private void OnPs4RegisterSuccess(object sender, PS4RegisterModel ps4RegisterModel)
        {
            this.button1.Invoke(new MethodInvoker(EnableRegistryButton));
            PS4RemotePlayData ps4RemotePlayData = new PS4RemotePlayData()
            {
                RemotePlay = new PS4RemotePlayDataRemotePlay()
                {
                    ApSsid = ps4RegisterModel.ApSsid,
                    ApBsid = ps4RegisterModel.ApBsid,
                    ApKey = ps4RegisterModel.ApKey,
                    Name = ps4RegisterModel.Name,
                    Mac = ps4RegisterModel.Mac,
                    RegistrationKey = ps4RegisterModel.RegistrationKey,
                    Nickname = ps4RegisterModel.Nickname,
                    RpKeyType = ps4RegisterModel.RpKeyType,
                    RpKey = ps4RegisterModel.RpKey,
                    RegisterHeaderInfoComplete = ps4RegisterModel.RegisterHeaderInfoComplete
                }
            };

            _settingManager.SavePS4RemotePlayData(ps4RemotePlayData);
            if (this.textBoxRegisterInfo.InvokeRequired)
            {
                this.textBoxRegisterInfo.Invoke(new MethodInvoker(() =>
                {
                    UpdateRegisterInfoTextBox(ps4RegisterModel.RegisterHeaderInfoComplete);
                    EnableConnectButton();
                    EnablePcapButton();
                    MessageBox.Show("Successfully registered with PS4", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }));
            }
            else
            {
                UpdateRegisterInfoTextBox(ps4RegisterModel.RegisterHeaderInfoComplete);
                EnableConnectButton();
                EnablePcapButton();
                MessageBox.Show("Successfully registered with PS4", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void OnPs4RegisterError(object sender, string errorMessage)
        {
            this.button1.Invoke(new MethodInvoker(EnableRegistryButton));
            MessageBox.Show("Could not register with PS4, error: " + errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void OnPs4ConnectionSuccess(object sender, EventArgs eventArgs)
        {
            this.button2.Invoke(new MethodInvoker(DisableConnectButton));
        }

        private void OnPs4Disconnected(object sender, string errorMessage)
        {
            this.button2.Invoke(new MethodInvoker(EnableConnectButton));
            MessageBox.Show(errorMessage, "Connection lost", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void OnPs4ConnectionError(object sender, string errorMessage)
        {
            this.button2.Invoke(new MethodInvoker(EnableConnectButton));
            MessageBox.Show("Could not connect to PS4, error: " + errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void OnPs4LogInfo(object sender, string logText)
        {
            this.textBoxLogOutput.Invoke(new MethodInvoker(() => AppendLogOutput(logText)));
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this._ps4ConnectionService.Dispose();
        }

        /*******************/
        /*** gui methods ***/
        /*******************/

        private void button1_Click(object sender, EventArgs e)
        {
            DisableRegistryButton();
            string psnId = textBoxPsnId.Text;
            string pin = textBoxRpKey.Text;

            if (psnId != "" && pin != "")
            {
                if (pin.Length == 8 && int.TryParse(pin, out var parsedPin))
                {
                    _ps4RegistrationService.PairConsole(psnId, parsedPin);
                }
                else
                {
                    MessageBox.Show("Please provide a valid pin", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Please enter valid registration info", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            DisableConnectButton();
            ClearLogOutput();
            _ps4DiscoveryService.DiscoverConsole(pS4DiscoveryInfo =>
            {
                if (pS4DiscoveryInfo == null)
                {
                    this.button2.Invoke(new MethodInvoker(EnableConnectButton));
                    MessageBox.Show("PS4 not found in network or not answering!", "Not found", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else
                {
                    this.textBoxLogOutput.Invoke(new MethodInvoker(() => AppendLogOutput("Discovery response:" + Environment.NewLine + pS4DiscoveryInfo.RawResponseData)));
                    if (pS4DiscoveryInfo.Status == 620)
                    {
                        MessageBox.Show("PS4 Found but status is 620, wake up is currently not implemented", "Found", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    else
                    {
                        PS4RemotePlayData remotePlayData = _settingManager.GetRemotePlayData();
                        if (remotePlayData != null)
                        {
                            _ps4ConnectionService.ConnectToPS4(pS4DiscoveryInfo.Ps4EndPoint, remotePlayData);
                        }
                        else
                        {
                            MessageBox.Show("Could not connect to PS4. No register data is available.", "No PS4 Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                }
            });
        }

        private void button3_Click(object sender, EventArgs e)
        {
            PS4RemotePlayData remotePlayData = _settingManager.GetRemotePlayData();
            if (remotePlayData != null)
            {
                OpenFileDialog openFileDialog = new OpenFileDialog();
                openFileDialog.Filter = @"PCAP files|*.pcap";
                DialogResult result = openFileDialog.ShowDialog(); // Show the dialog.
                if (result == DialogResult.OK) // Test result.
                {
                    ClearPcapOutputLogOutput();
                    string file = openFileDialog.FileName;
                    try
                    {
                        var packets = Pcap.ReadFile(file).TrySelect(record => PacketParser.Parse(new ArraySegment<byte>(record.Data.Skip(14).ToArray())));
                        var ipPackets = packets as IpPacket[] ?? packets.ToArray();
                        var tcpRemotePlayPackets = ipPackets.
                            Where(p => p != null).
                            Where(p => p.ProtocolType == ProtocolType.Tcp).
                            Where(p =>
                            {
                                var packetData = p.PacketData.AsByteArraySegment();
                                ushort sourcePort = p.PacketData.Array.ReadNetOrderUShort(packetData.Offset);
                                ushort destinationPort = p.PacketData.Array.ReadNetOrderUShort(2 + packetData.Offset);
                                return sourcePort == 9295 || destinationPort == 9295;
                            }).
                            ToArray();

                        var udpRemotePlayPackets = ipPackets.
                            Where(p => p != null).
                            Where(p => p.ProtocolType == ProtocolType.Udp).
                            Select(p => p.ToUdpDatagram()).
                            Where(p => p.UdpDatagramHeader.SourcePort == 9296 ||
                                       p.UdpDatagramHeader.DestinationPort == 9296).
                            ToArray();

                        Session session = CheckForConnectionAesKey(tcpRemotePlayPackets);
                        CheckForBigBangPayload(udpRemotePlayPackets, session);
                    }
                    catch (IOException)
                    {
                    }
                }
            }
            else
            {
                MessageBox.Show("Could not search for AES keys. No register data is available.", "No PS4 Data", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button4_Click(object sender, EventArgs e)
        {
            int selectedIndex = this.comboBoxNetworkAdapter.SelectedIndex;
            if (selectedIndex >= 0 && selectedIndex <= (this.networkAdapters.Count - 1))
            {
                DisableLivePcapParsingButton();
                DisablePcapButton();
                labelCapturingIndication.Visible = true;
                comboBoxNetworkAdapter.Enabled = false;
                Task.Factory.StartNew(() =>
                {
                    LivePacketDevice selectedDevice = this.networkAdapters[selectedIndex];
                    // 65536 guarantees that the whole packet will be captured on all the link layers
                    using (PacketCommunicator communicator = selectedDevice.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 1000))
                    {
                        using (BerkeleyPacketFilter filter = communicator.CreateFilter("tcp dst port 9295 or tcp src port 9295"))
                        {
                            // Set the filter
                            communicator.SetFilter(filter);
                        }

                        // start the capture
                        communicator.ReceivePackets(0, PacketHandlerTcp);
                    }
                });
                Task.Factory.StartNew(() =>
                {
                    LivePacketDevice selectedDevice = this.networkAdapters[selectedIndex];

                    // 65536 guarantees that the whole packet will be captured on all the link layers
                    using (PacketCommunicator communicator = selectedDevice.Open(65536, PacketDeviceOpenAttributes.Promiscuous, 1000))
                    {
                        using (BerkeleyPacketFilter filter = communicator.CreateFilter("udp dst port 9296 or udp src port 9296"))
                        {
                            // Set the filter
                            communicator.SetFilter(filter);
                        }

                        // start the capture
                        communicator.ReceivePackets(0, PacketHandlerUdp);
                    }
                });
            }
        }

        private void comboBoxNetworkAdapter_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            if (comboBox.SelectedIndex >= 0)
            {
                EnableLivePcapParsingButton();
            }
            else
            {
                DisableLivePcapParsingButton();
            }
        }

        private void textBoxRpKey_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) &&
                (e.KeyChar != '.'))
            {
                e.Handled = true;
            }

            // only allow one decimal point
            if ((e.KeyChar == '.') && (((TextBox)sender).Text.IndexOf('.') > -1))
            {
                e.Handled = true;
            }
        }

        private void textBoxRpKey_TextChanged(object sender, EventArgs e)
        {
            string pin = textBoxRpKey.Text;
            if (pin.Length == 8 && int.TryParse(pin, out var parsedPin))
            {
                labelRegistryAesKey.Text = HexUtil.Hexlify(CryptoService.GetRegistryAesKeyForPin(parsedPin));
                labelRegistryAesKeyHeading.Visible = true;
            }
            else
            {
                labelRegistryAesKey.Text = "";
                labelRegistryAesKeyHeading.Visible = false;
            }
        }

        private void UpdateRegisterInfoTextBox(string ps4RegisterData)
        {
            this.textBoxRegisterInfo.Text = ps4RegisterData;
            this.textBoxRegisterInfo.SelectionStart = 0;
        }

        private void EnableConnectButton()
        {
            this.button2.Enabled = true;
        }

        private void DisableConnectButton()
        {
            this.button2.Enabled = false;
        }

        private void EnablePcapButton()
        {
            this.button3.Enabled = true;
        }

        private void DisablePcapButton()
        {
            this.button3.Enabled = false;
        }

        private void EnableRegistryButton()
        {
            this.button1.Enabled = true;
        }

        private void DisableRegistryButton()
        {
            this.button1.Enabled = false;
        }

        private void EnableLivePcapParsingButton()
        {
            this.button4.Enabled = true;
        }

        private void DisableLivePcapParsingButton()
        {
            this.button4.Enabled = false;
        }

        private void ClearLogOutput()
        {
            this.textBoxLogOutput.Text = "";
        }

        private void AppendLogOutput(string text)
        {
            this.textBoxLogOutput.Text += text;
            this.textBoxLogOutput.Text += Environment.NewLine;
        }

        private void ClearPcapOutputLogOutput()
        {
            this.textBoxPcapLogOutput.Text = "";
        }

        private void AppendLogOutputToPcapLogTextBox(string text)
        {
            this.textBoxPcapLogOutput.Text += text;
            this.textBoxPcapLogOutput.Text += Environment.NewLine;
        }

        private void SetUpComboBoxNetworkAdapter()
        {
            this.comboBoxNetworkAdapter.Items.Clear();
            this.comboBoxNetworkAdapter.DropDownStyle = ComboBoxStyle.DropDownList;
            IList<LivePacketDevice> allDevices = LivePacketDevice.AllLocalMachine;
            if (allDevices.Count == 0)
            {
                MessageBox.Show("No network adapter found. Could not use WinPcap for live capturing", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                networkAdapters.AddRange(allDevices);
                foreach (var networkAdapter in networkAdapters)
                {
                    string description = networkAdapter.Description ?? networkAdapter.Name;
                    description = description.Replace("Network adapter", "");
                    description += String.Format(" ({0})", networkAdapter.Addresses.Last().Address);
                    this.comboBoxNetworkAdapter.Items.Add(description);
                }

            }
        }

        /***********************/
        /*** private methods ***/
        /***********************/


        private Session CheckForConnectionAesKey(IpPacket[] tcpRemotePlayPackets)
        {
            int ipProtocolHeaderSize = 20;
            for (int i = 0; i < tcpRemotePlayPackets.Length; i++)
            {
                var tcpRemotePlayPacket = tcpRemotePlayPackets[i];
                if (tcpRemotePlayPacket.PacketData.Array != null && tcpRemotePlayPacket.PacketData.Array.Length > 0)
                {
                    int length = tcpRemotePlayPacket.PacketData.Count - ipProtocolHeaderSize;
                    byte[] payload = new byte[length];
                    Buffer.BlockCopy(tcpRemotePlayPacket.PacketData.Array, tcpRemotePlayPacket.PacketData.Offset + ipProtocolHeaderSize, payload, 0, payload.Length);

                    string encodedPayload = Encoding.ASCII.GetString(payload);
                    if (encodedPayload.StartsWith("GET /sce/rp/session HTTP/1.1\r\n"))
                    {
                        Dictionary<string, string> httpHeaders = ByteUtil.ByteArrayToHttpHeader(payload);
                        httpHeaders.TryGetValue("RP-Registkey", out var rpRegistKey);
                        AppendLogOutputToPcapLogTextBox("RP-Registkey: " + rpRegistKey);
                        if (i < tcpRemotePlayPackets.Length - 1)
                        {
                            var sessionResponse = tcpRemotePlayPackets[i + 1];
                            if (sessionResponse.PacketData.Array == null ||
                                sessionResponse.PacketData.Array.Length < 1)
                            {
                                return null;
                            }

                            length = sessionResponse.PacketData.Count - ipProtocolHeaderSize;
                            payload = new byte[length];
                            Buffer.BlockCopy(sessionResponse.PacketData.Array, sessionResponse.PacketData.Offset + ipProtocolHeaderSize, payload, 0, payload.Length);

                            encodedPayload = Encoding.ASCII.GetString(payload);
                            if (encodedPayload.StartsWith("HTTP/1.1 200 OK\r\n"))
                            {
                                httpHeaders = ByteUtil.ByteArrayToHttpHeader(payload);
                                httpHeaders.TryGetValue("RP-Nonce", out var rpNonce);
                                if (rpNonce == null)
                                {
                                    return null;
                                }

                                byte[] rpKeyBuffer = HexUtil.Unhexlify(_settingManager.GetRemotePlayData().RemotePlay.RpKey);
                                byte[] rpNonceDecoded = Convert.FromBase64String(rpNonce);

                                AppendLogOutputToPcapLogTextBox("RP-Nonce from \"/sce/rp/session\" response: " + HexUtil.Hexlify(rpNonceDecoded));

                                string controlAesKey = HexUtil.Hexlify(CryptoService.GetSessionAesKeyForControl(rpKeyBuffer, rpNonceDecoded));
                                string controlNonce = HexUtil.Hexlify(CryptoService.GetSessionNonceValueForControl(rpNonceDecoded));
                                AppendLogOutputToPcapLogTextBox("!!! Control AES Key: " + controlAesKey);
                                AppendLogOutputToPcapLogTextBox("!!! Control AES Nonce: " + controlNonce + Environment.NewLine);
                                return CryptoService.GetSessionForControl(rpKeyBuffer, rpNonceDecoded);
                            }
                        }
                    }
                }
            }

            return null;
        }

        private void CheckForBigBangPayload(UdpDatagram[] udpRemotePlayPackets, Session session)
        {
            foreach (var udpRemotePlayPacket in udpRemotePlayPackets)
            {
                if (udpRemotePlayPacket.UdpData.Array != null && udpRemotePlayPacket.UdpData.Array.Length > 0)
                {
                    int length = udpRemotePlayPacket.UdpData.Count;
                    byte[] payload = new byte[length];
                    Buffer.BlockCopy(udpRemotePlayPacket.UdpData.Array, udpRemotePlayPacket.UdpData.Offset, payload, 0, payload.Length);

                    HandleControlMessage(payload, session);
                }
            }
        }

        private void HandleControlMessage(byte[] payload, Session session)
        {
            if (payload[0] == 0) // Control Packet
            {
                byte[] message = payload;
                ControlMessage controlMessage = new ControlMessage();
                using (MemoryStream memoryStream = new MemoryStream(message, 0, message.Length))
                using (BinaryReader binaryWriter = new BinaryReader(memoryStream))
                {
                    controlMessage.Deserialize(binaryWriter);
                }

                if (controlMessage.ProtoBuffFlag == 1 && controlMessage.PLoadSize > 100)
                {
                    TakionMessage takionMessage = ProtobufUtil.Deserialize<TakionMessage>(controlMessage.UnParsedPayload);

                    if (takionMessage.bigPayload != null)
                    {
                        this.textBoxPcapLogOutput.Invoke(new MethodInvoker(() => AppendLogOutputToPcapLogTextBox("!!! Big payload session key: " + takionMessage.bigPayload.sessionKey)));
                        this.textBoxPcapLogOutput.Invoke(new MethodInvoker(() => AppendLogOutputToPcapLogTextBox("!!! Big payload ecdh pub key in hex: " + HexUtil.Hexlify(takionMessage.bigPayload.ecdhPubKey))));
                        this.textBoxPcapLogOutput.Invoke(new MethodInvoker(() => AppendLogOutputToPcapLogTextBox("!!! Big payload ecdh sig in hex: " + HexUtil.Hexlify(takionMessage.bigPayload.ecdhSig))));
                        if (session != null)
                        {
                            byte[] launchSpecBuffer = Convert.FromBase64String(takionMessage.bigPayload.launchSpec);
                            byte[] cryptoBuffer = new byte[launchSpecBuffer.Length];
                            cryptoBuffer = session.Encrypt(cryptoBuffer, 0);
                            byte[] newLaunchSpec = new byte[launchSpecBuffer.Length];
                            for (int j = 0; j < launchSpecBuffer.Length; j++)
                            {
                                newLaunchSpec[j] = (byte)(launchSpecBuffer[j] ^ cryptoBuffer[j]);
                            }

                            string launchSpecs = Encoding.UTF8.GetString(newLaunchSpec);
                            LaunchSpecification launchSpecJsonObject = LaunchSpecification.Deserialize(launchSpecs);
                            byte[] handshakeKey = launchSpecJsonObject.HandshakeKey;

                            if (handshakeKey != null)
                            {
                                this.textBoxPcapLogOutput.Invoke(new MethodInvoker(() => AppendLogOutputToPcapLogTextBox("!!! Big payload handshake key in launchSpec in hex: " + HexUtil.Hexlify(handshakeKey))));
                                var ecdhSignatureVerification = Session.CalculateHMAC(handshakeKey, takionMessage.bigPayload.ecdhPubKey);

                                if (ecdhSignatureVerification.SequenceEqual(takionMessage.bigPayload.ecdhSig))
                                    this.textBoxPcapLogOutput.Invoke(new MethodInvoker(() => AppendLogOutputToPcapLogTextBox("ECDH Signature matches!")));
                                else
                                    this.textBoxPcapLogOutput.Invoke(new MethodInvoker(() => AppendLogOutputToPcapLogTextBox("!!!! ECDH Signature mismatch")));
                            }
                            this.textBoxPcapLogOutput.Invoke(new MethodInvoker(() => AppendLogOutputToPcapLogTextBox("!!! Big payload full launchSpec: " + Environment.NewLine + launchSpecJsonObject.Serialize() + Environment.NewLine)));
                        }
                        else
                        {
                            this.textBoxPcapLogOutput.Invoke(new MethodInvoker(() => AppendLogOutputToPcapLogTextBox(Environment.NewLine)));
                        }
                    }
                    else if (takionMessage.bangPayload != null)
                    {
                        this.textBoxPcapLogOutput.Invoke(new MethodInvoker(() => AppendLogOutputToPcapLogTextBox("!!! Bang payload session key: " + takionMessage.bangPayload.sessionKey)));
                        this.textBoxPcapLogOutput.Invoke(new MethodInvoker(() => AppendLogOutputToPcapLogTextBox("!!! Bang payload ecdh pub key in hex: " + HexUtil.Hexlify(takionMessage.bangPayload.ecdhPubKey))));
                        this.textBoxPcapLogOutput.Invoke(new MethodInvoker(() => AppendLogOutputToPcapLogTextBox("!!! Bang payload ecdh sig in hex: " + HexUtil.Hexlify(takionMessage.bangPayload.ecdhSig))));
                    }
                }
            }
        }

        // Callback function invoked by Pcap.Net for ps4 tcp messages
        private void PacketHandlerTcp(Packet packet)
        {
            IpV4Datagram ip = packet.Ethernet.IpV4;
            IpV4Protocol protocol = ip.Protocol;
            if (protocol == IpV4Protocol.Tcp)
            {
                TcpDatagram tcpDatagram = ip.Tcp;
                HttpDatagram httpDatagram = tcpDatagram.Http;
                if (httpDatagram.Length > 0 && httpDatagram.Header != null)
                {
                    string httpPacket = httpDatagram.Decode(Encoding.UTF8);
                    if (httpPacket.StartsWith("GET /sce/rp/session HTTP/1.1\r\n"))
                    {
                        Dictionary<string, string> header = HttpUtils.SplitHttpResponse(httpPacket);
                        header.TryGetValue("RP-Registkey", out var registKey);
                        this.textBoxPcapLogOutput.Invoke(new MethodInvoker(() => AppendLogOutputToPcapLogTextBox("RP-Registkey: " + registKey)));

                        _livePcapContext.LivePcapState = LivePcapState.SESSION_REQUEST;
                        _livePcapContext.Session = null;
                    }
                    else if (httpDatagram.IsResponse && httpPacket.StartsWith("HTTP/1.1 200 OK\r\n") && _livePcapContext.LivePcapState == LivePcapState.SESSION_REQUEST)
                    {
                        Dictionary<string, string> header = HttpUtils.SplitHttpResponse(httpPacket);
                        header.TryGetValue("RP-Nonce", out var rpNonce);
                        if (rpNonce == null)
                        {
                            return;
                        }

                        byte[] rpKeyBuffer = HexUtil.Unhexlify(_settingManager.GetRemotePlayData().RemotePlay.RpKey);
                        byte[] rpNonceDecoded = Convert.FromBase64String(rpNonce);

                        this.textBoxPcapLogOutput.Invoke(new MethodInvoker(() => AppendLogOutputToPcapLogTextBox("RP-Nonce from \"/sce/rp/session\" response: " + HexUtil.Hexlify(rpNonceDecoded))));

                        string controlAesKey = HexUtil.Hexlify(CryptoService.GetSessionAesKeyForControl(rpKeyBuffer, rpNonceDecoded));
                        string controlNonce = HexUtil.Hexlify(CryptoService.GetSessionNonceValueForControl(rpNonceDecoded));
                        this.textBoxPcapLogOutput.Invoke(new MethodInvoker(() => AppendLogOutputToPcapLogTextBox("!!! Control AES Key: " + controlAesKey)));
                        this.textBoxPcapLogOutput.Invoke(new MethodInvoker(() => AppendLogOutputToPcapLogTextBox("!!! Control AES Nonce: " + controlNonce + Environment.NewLine)));
                        _livePcapContext.LivePcapState = LivePcapState.SESSION_RESPONSE;
                        _livePcapContext.Session = CryptoService.GetSessionForControl(rpKeyBuffer, rpNonceDecoded);
                    }
                }
            }
        }

        // Callback function invoked by Pcap.Net for ps4 udp messages
        private void PacketHandlerUdp(Packet packet)
        {
            IpV4Datagram ip = packet.Ethernet.IpV4;
            IpV4Protocol protocol = ip.Protocol;
            if (protocol == IpV4Protocol.Udp)
            {
                PcapDotNet.Packets.Transport.UdpDatagram udpDatagram = ip.Udp;
                if (udpDatagram.Length > 0 && udpDatagram.Payload.Length > 0 && _livePcapContext.Session != null)
                {
                    byte[] payload = udpDatagram.Payload.ToMemoryStream().ToArray();

                    HandleControlMessage(payload, _livePcapContext.Session);
                }
            }
        }
    }

    /*********************/
    /*** inner classes ***/
    /*********************/

    public class LivePcapContext
    {
        public LivePcapState LivePcapState { get; set; }
        public Session Session { get; set; }

        public LivePcapContext()
        {
            LivePcapState = LivePcapState.UNKNOWN;
            Session = null;
        }
    }

    public enum LivePcapState
    {
        SESSION_REQUEST,
        SESSION_RESPONSE,
        UNKNOWN
    }
}
