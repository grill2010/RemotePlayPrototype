using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;
using Ps4RemotePlayPrototype.Protocol.Connection;
using Ps4RemotePlayPrototype.Protocol.Crypto;
using Ps4RemotePlayPrototype.Protocol.Discovery;
using Ps4RemotePlayPrototype.Protocol.Message;
using Ps4RemotePlayPrototype.Protocol.Model;
using Ps4RemotePlayPrototype.Protocol.Registration;
using Ps4RemotePlayPrototype.Util;
using Ps4RemotePlayPrototype.Setting;
using Tx.Network;

namespace Ps4RemotePlayPrototype
{
    public partial class Form1 : Form
    {
        private readonly SettingManager _settingManager;
        private readonly PS4RegistrationService _ps4RegistrationService;
        private readonly PS4DiscoveryService _ps4DiscoveryService;
        private readonly PS4ConnectionService _ps4ConnectionService;

        public Form1()
        {
            InitializeComponent();

            _settingManager = SettingManager.GetInstance();
            _ps4RegistrationService = new PS4RegistrationService();
            _ps4DiscoveryService = new PS4DiscoveryService();
            _ps4ConnectionService = new PS4ConnectionService();

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
                    MessageBox.Show("Successfully registered with PS4", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }));
            }
            else
            {
                UpdateRegisterInfoTextBox(ps4RegisterModel.RegisterHeaderInfoComplete);
                EnableConnectButton();
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
            this.button2.Invoke(new MethodInvoker(EnableConnectButton));
        }

        private void OnPs4Disconnected(object sender, string errorMessage)
        {
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

                    this.button2.Invoke(new MethodInvoker(EnableConnectButton));
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
                        var tcpRemotePlayPackets = packets.
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

                        var udpRemotePlayPackets = packets.
                            Where(p => p != null).
                            Where(p => p.ProtocolType == ProtocolType.Udp).
                            Select(p => p.ToUdpDatagram()).
                            Where(p => p.UdpDatagramHeader.SourcePort == 9296 ||
                                       p.UdpDatagramHeader.DestinationPort == 9296).
                            ToArray();

                        CheckForConnectionAesKey(tcpRemotePlayPackets);
                        // CheckForBigBangPayload(udpRemotePlayPackets); Not yet working
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

        private void EnableRegistryButton()
        {
            this.button1.Enabled = true;
        }

        private void DisableRegistryButton()
        {
            this.button1.Enabled = false;
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

        /***********************/
        /*** private methods ***/
        /***********************/


        private void CheckForConnectionAesKey(IpPacket[] tcpRemotePlayPackets)
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
                                return;
                            }

                            length = sessionResponse.PacketData.Count - ipProtocolHeaderSize;
                            payload = new byte[length];
                            Buffer.BlockCopy(sessionResponse.PacketData.Array, sessionResponse.PacketData.Offset + ipProtocolHeaderSize, payload, 0, payload.Length);

                            encodedPayload = Encoding.ASCII.GetString(payload);
                            if (encodedPayload.StartsWith("HTTP/1.1 200 OK\r\n"))
                            {
                                httpHeaders = ByteUtil.ByteArrayToHttpHeader(payload);
                                httpHeaders.TryGetValue("RP-Nonce", out var rpNonce);


                                byte[] rpKeyBuffer = HexUtil.Unhexlify(_settingManager.GetRemotePlayData().RemotePlay.RpKey);
                                byte[] rpNonceDecoded = Convert.FromBase64String(rpNonce);

                                AppendLogOutputToPcapLogTextBox("RP-Nonce from \"/sce/rp/session\" response: " + HexUtil.Hexlify(rpNonceDecoded));

                                string controlAesKey = HexUtil.Hexlify(CryptoService.GetSessionAesKeyForControl(rpKeyBuffer, rpNonceDecoded));
                                string controlNonce = HexUtil.Hexlify(CryptoService.GetSessionNonceValueForControl(rpNonceDecoded));
                                AppendLogOutputToPcapLogTextBox("!!! Control AES Key: " + controlAesKey);
                                AppendLogOutputToPcapLogTextBox("!!! Control AES Nonce: " + controlNonce + Environment.NewLine);
                            }
                        }
                    }
                }
            }
        }

        private void CheckForBigBangPayload(UdpDatagram[] udpRemotePlayPackets)
        {
            int ipProtocolHeaderSize = 20;
            foreach (var udpRemotePlayPacket in udpRemotePlayPackets)
            {
                if (udpRemotePlayPacket.UdpData.Array != null && udpRemotePlayPacket.UdpData.Array.Length > 0)
                {
                    int length = udpRemotePlayPacket.UdpData.Count - ipProtocolHeaderSize;
                    byte[] payload = new byte[length];
                    Buffer.BlockCopy(udpRemotePlayPacket.UdpData.Array, udpRemotePlayPacket.UdpData.Offset + ipProtocolHeaderSize, payload, 0, payload.Length);

                    if (payload[0] == 0) // Control Packet
                    {
                        byte[] message = payload;
                        ControlMessage controlMessage = new ControlMessage();
                        using (MemoryStream memoryStream = new MemoryStream(message, 0, message.Length))
                        using (BinaryReader binaryWriter = new BinaryReader(memoryStream))
                        {
                            controlMessage.Deserialize(binaryWriter);
                        }

                        string sads = "";
                    }
                }
            }
        }
    }
}
