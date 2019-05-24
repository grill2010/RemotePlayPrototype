namespace Ps4RemotePlay.Ui
{
    partial class Form1
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            this.textBoxRegisterInfo = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.textBoxLogOutput = new System.Windows.Forms.TextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.textBoxPsnId = new System.Windows.Forms.TextBox();
            this.textBoxRpKey = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.button2 = new System.Windows.Forms.Button();
            this.labelRegistryAesKey = new System.Windows.Forms.Label();
            this.labelRegistryAesKeyHeading = new System.Windows.Forms.Label();
            this.textBoxPcapLogOutput = new System.Windows.Forms.TextBox();
            this.button3 = new System.Windows.Forms.Button();
            this.label5 = new System.Windows.Forms.Label();
            this.comboBoxNetworkAdapter = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.button4 = new System.Windows.Forms.Button();
            this.labelCapturingIndication = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // textBoxRegisterInfo
            // 
            this.textBoxRegisterInfo.Location = new System.Drawing.Point(12, 25);
            this.textBoxRegisterInfo.Multiline = true;
            this.textBoxRegisterInfo.Name = "textBoxRegisterInfo";
            this.textBoxRegisterInfo.ReadOnly = true;
            this.textBoxRegisterInfo.Size = new System.Drawing.Size(560, 183);
            this.textBoxRegisterInfo.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(12, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(66, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Register info";
            // 
            // textBoxLogOutput
            // 
            this.textBoxLogOutput.Location = new System.Drawing.Point(12, 246);
            this.textBoxLogOutput.Multiline = true;
            this.textBoxLogOutput.Name = "textBoxLogOutput";
            this.textBoxLogOutput.ReadOnly = true;
            this.textBoxLogOutput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxLogOutput.Size = new System.Drawing.Size(560, 183);
            this.textBoxLogOutput.TabIndex = 2;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(12, 230);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(58, 13);
            this.label2.TabIndex = 3;
            this.label2.Text = "Log output";
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(578, 185);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(244, 23);
            this.button1.TabIndex = 4;
            this.button1.Text = "Register";
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // textBoxPsnId
            // 
            this.textBoxPsnId.Location = new System.Drawing.Point(578, 41);
            this.textBoxPsnId.Name = "textBoxPsnId";
            this.textBoxPsnId.Size = new System.Drawing.Size(244, 20);
            this.textBoxPsnId.TabIndex = 5;
            // 
            // textBoxRpKey
            // 
            this.textBoxRpKey.Location = new System.Drawing.Point(578, 90);
            this.textBoxRpKey.MaxLength = 8;
            this.textBoxRpKey.Name = "textBoxRpKey";
            this.textBoxRpKey.Size = new System.Drawing.Size(244, 20);
            this.textBoxRpKey.TabIndex = 6;
            this.textBoxRpKey.TextChanged += new System.EventHandler(this.textBoxRpKey_TextChanged);
            this.textBoxRpKey.KeyPress += new System.Windows.Forms.KeyPressEventHandler(this.textBoxRpKey_KeyPress);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(575, 25);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(66, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "PS4 PSN-ID";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(575, 74);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(43, 13);
            this.label4.TabIndex = 8;
            this.label4.Text = "RP-Key";
            // 
            // button2
            // 
            this.button2.Enabled = false;
            this.button2.Location = new System.Drawing.Point(578, 406);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(244, 23);
            this.button2.TabIndex = 9;
            this.button2.Text = "Connect";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // labelRegistryAesKey
            // 
            this.labelRegistryAesKey.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.labelRegistryAesKey.Location = new System.Drawing.Point(578, 140);
            this.labelRegistryAesKey.Name = "labelRegistryAesKey";
            this.labelRegistryAesKey.Size = new System.Drawing.Size(244, 36);
            this.labelRegistryAesKey.TabIndex = 10;
            this.labelRegistryAesKey.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelRegistryAesKeyHeading
            // 
            this.labelRegistryAesKeyHeading.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.labelRegistryAesKeyHeading.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelRegistryAesKeyHeading.Location = new System.Drawing.Point(575, 122);
            this.labelRegistryAesKeyHeading.Name = "labelRegistryAesKeyHeading";
            this.labelRegistryAesKeyHeading.Size = new System.Drawing.Size(247, 18);
            this.labelRegistryAesKeyHeading.TabIndex = 11;
            this.labelRegistryAesKeyHeading.Text = "Used AES-Regist Key:";
            this.labelRegistryAesKeyHeading.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelRegistryAesKeyHeading.Visible = false;
            // 
            // textBoxPcapLogOutput
            // 
            this.textBoxPcapLogOutput.Location = new System.Drawing.Point(15, 469);
            this.textBoxPcapLogOutput.Multiline = true;
            this.textBoxPcapLogOutput.Name = "textBoxPcapLogOutput";
            this.textBoxPcapLogOutput.ReadOnly = true;
            this.textBoxPcapLogOutput.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxPcapLogOutput.Size = new System.Drawing.Size(560, 183);
            this.textBoxPcapLogOutput.TabIndex = 12;
            // 
            // button3
            // 
            this.button3.Enabled = false;
            this.button3.Location = new System.Drawing.Point(581, 629);
            this.button3.Name = "button3";
            this.button3.Size = new System.Drawing.Size(244, 23);
            this.button3.TabIndex = 13;
            this.button3.Text = "Load Pcap file";
            this.button3.UseVisualStyleBackColor = true;
            this.button3.Click += new System.EventHandler(this.button3_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(12, 453);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(108, 13);
            this.label5.TabIndex = 14;
            this.label5.Text = "Log output from pcap";
            // 
            // comboBoxNetworkAdapter
            // 
            this.comboBoxNetworkAdapter.FormattingEnabled = true;
            this.comboBoxNetworkAdapter.Location = new System.Drawing.Point(582, 488);
            this.comboBoxNetworkAdapter.Name = "comboBoxNetworkAdapter";
            this.comboBoxNetworkAdapter.Size = new System.Drawing.Size(240, 21);
            this.comboBoxNetworkAdapter.TabIndex = 15;
            this.comboBoxNetworkAdapter.SelectedIndexChanged += new System.EventHandler(this.comboBoxNetworkAdapter_SelectedIndexChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(582, 469);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(86, 13);
            this.label6.TabIndex = 16;
            this.label6.Text = "Network adapter";
            // 
            // button4
            // 
            this.button4.Enabled = false;
            this.button4.Location = new System.Drawing.Point(582, 515);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(244, 23);
            this.button4.TabIndex = 17;
            this.button4.Text = "Live Pcap parsing";
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // labelCapturingIndication
            // 
            this.labelCapturingIndication.AutoSize = true;
            this.labelCapturingIndication.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelCapturingIndication.Location = new System.Drawing.Point(651, 541);
            this.labelCapturingIndication.Name = "labelCapturingIndication";
            this.labelCapturingIndication.Size = new System.Drawing.Size(104, 13);
            this.labelCapturingIndication.TabIndex = 18;
            this.labelCapturingIndication.Text = "Capturing started";
            this.labelCapturingIndication.Visible = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(834, 661);
            this.Controls.Add(this.labelCapturingIndication);
            this.Controls.Add(this.button4);
            this.Controls.Add(this.label6);
            this.Controls.Add(this.comboBoxNetworkAdapter);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.button3);
            this.Controls.Add(this.textBoxPcapLogOutput);
            this.Controls.Add(this.labelRegistryAesKeyHeading);
            this.Controls.Add(this.labelRegistryAesKey);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.textBoxRpKey);
            this.Controls.Add(this.textBoxPsnId);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.textBoxLogOutput);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBoxRegisterInfo);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "Form1";
            this.Text = "Form1";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBoxRegisterInfo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox textBoxLogOutput;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.TextBox textBoxPsnId;
        private System.Windows.Forms.TextBox textBoxRpKey;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.Label labelRegistryAesKey;
        private System.Windows.Forms.Label labelRegistryAesKeyHeading;
        private System.Windows.Forms.TextBox textBoxPcapLogOutput;
        private System.Windows.Forms.Button button3;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.ComboBox comboBoxNetworkAdapter;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.Label labelCapturingIndication;
    }
}

