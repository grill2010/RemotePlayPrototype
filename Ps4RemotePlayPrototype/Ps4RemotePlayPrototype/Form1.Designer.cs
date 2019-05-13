namespace Ps4RemotePlayPrototype
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
            this.labelRegistryAesKey.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelRegistryAesKey.Location = new System.Drawing.Point(578, 146);
            this.labelRegistryAesKey.Name = "labelRegistryAesKey";
            this.labelRegistryAesKey.Size = new System.Drawing.Size(244, 15);
            this.labelRegistryAesKey.TabIndex = 10;
            this.labelRegistryAesKey.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // labelRegistryAesKeyHeading
            // 
            this.labelRegistryAesKeyHeading.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.labelRegistryAesKeyHeading.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.labelRegistryAesKeyHeading.Location = new System.Drawing.Point(575, 128);
            this.labelRegistryAesKeyHeading.Name = "labelRegistryAesKeyHeading";
            this.labelRegistryAesKeyHeading.Size = new System.Drawing.Size(247, 15);
            this.labelRegistryAesKeyHeading.TabIndex = 11;
            this.labelRegistryAesKeyHeading.Text = "Used AES-Regist Key:";
            this.labelRegistryAesKeyHeading.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.labelRegistryAesKeyHeading.Visible = false;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(834, 450);
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
    }
}

