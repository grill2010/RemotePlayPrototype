using System;
using System.Windows.Forms;
using Ps4RemotePlayPrototype.Protocol.Connection;
using Ps4RemotePlayPrototype.Protocol.Crypto;
using Ps4RemotePlayPrototype.Protocol.Discovery;
using Ps4RemotePlayPrototype.Protocol.Model;
using Ps4RemotePlayPrototype.Protocol.Registration;
using Ps4RemotePlayPrototype.Setting;

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
            _ps4ConnectionService.OnPs4ConnectionError += OnPs4ConnectionError;
            _ps4ConnectionService.OnPs4LogInfo += OnPs4LogInfo; 

            PS4RemotePlayData remotePlayData = _settingManager.GetRemotePlayData();
            if (remotePlayData != null)
            {
                UpdateRegisterInfoTextBox(remotePlayData.RemotePlay.RegisterHeaderInfoComplete);
                EnableConnectButton();
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

        private void OnPs4ConnectionError(object sender, string errorMessage)
        {
            this.button2.Invoke(new MethodInvoker(EnableConnectButton));
            MessageBox.Show("Could not connect to PS4, error: " + errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private void OnPs4LogInfo(object sender, string logText)
        {
            this.textBoxLogOutput.Invoke(new MethodInvoker(() => AppendLogOutput(logText)));
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
                            // ToDo add listener
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
                labelRegistryAesKey.Text = CryptoService.GetRegistryAesKeyForPin(parsedPin);
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

        /***********************/
        /*** private methods ***/
        /***********************/


    }
}
