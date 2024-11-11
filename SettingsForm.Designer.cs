namespace TelitLoader {
    partial class SettingsForm {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing) {
            if (disposing && (components != null)) {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent() {
            this.components = new System.ComponentModel.Container();
            this.radioButton1 = new System.Windows.Forms.RadioButton();
            this.radioButton2 = new System.Windows.Forms.RadioButton();
            this.radioButton0 = new System.Windows.Forms.RadioButton();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.toolTip = new System.Windows.Forms.ToolTip(this.components);
            this.scriptStartTo = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.buttonOK = new System.Windows.Forms.Button();
            this.checkBoxSecure = new System.Windows.Forms.CheckBox();
            this.checkBoxRemove = new System.Windows.Forms.CheckBox();
            this.checkBoxAuto = new System.Windows.Forms.CheckBox();
            this.numericUpDownAutoSec = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.label5 = new System.Windows.Forms.Label();
            this.textBoxScriptTelit = new System.Windows.Forms.TextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.textBoxScriptCommon = new System.Windows.Forms.TextBox();
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.buttonPortCheck = new System.Windows.Forms.Button();
            this.comboBoxCOMSpeed = new System.Windows.Forms.ComboBox();
            this.comboBoxCOMPort = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scriptStartTo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownAutoSec)).BeginInit();
            this.groupBox2.SuspendLayout();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // radioButton1
            // 
            this.radioButton1.AutoSize = true;
            this.radioButton1.Location = new System.Drawing.Point(20, 59);
            this.radioButton1.Name = "radioButton1";
            this.radioButton1.Size = new System.Drawing.Size(76, 21);
            this.radioButton1.TabIndex = 0;
            this.radioButton1.TabStop = true;
            this.radioButton1.Text = "mode 1";
            this.radioButton1.UseVisualStyleBackColor = true;
            this.radioButton1.CheckedChanged += new System.EventHandler(this.radioButton1_CheckedChanged);
            // 
            // radioButton2
            // 
            this.radioButton2.AutoSize = true;
            this.radioButton2.Location = new System.Drawing.Point(20, 86);
            this.radioButton2.Name = "radioButton2";
            this.radioButton2.Size = new System.Drawing.Size(76, 21);
            this.radioButton2.TabIndex = 0;
            this.radioButton2.TabStop = true;
            this.radioButton2.Text = "mode 2";
            this.radioButton2.UseVisualStyleBackColor = true;
            this.radioButton2.CheckedChanged += new System.EventHandler(this.radioButton2_CheckedChanged);
            // 
            // radioButton0
            // 
            this.radioButton0.AutoSize = true;
            this.radioButton0.Location = new System.Drawing.Point(19, 32);
            this.radioButton0.Name = "radioButton0";
            this.radioButton0.Size = new System.Drawing.Size(76, 21);
            this.radioButton0.TabIndex = 0;
            this.radioButton0.TabStop = true;
            this.radioButton0.Text = "mode 0";
            this.radioButton0.UseVisualStyleBackColor = true;
            this.radioButton0.CheckedChanged += new System.EventHandler(this.radioButton0_CheckedChanged);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.radioButton1);
            this.groupBox1.Controls.Add(this.radioButton0);
            this.groupBox1.Controls.Add(this.radioButton2);
            this.groupBox1.Location = new System.Drawing.Point(13, 103);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(357, 118);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "AT#startmodescr";
            // 
            // scriptStartTo
            // 
            this.scriptStartTo.Location = new System.Drawing.Point(214, 162);
            this.scriptStartTo.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.scriptStartTo.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.scriptStartTo.Name = "scriptStartTo";
            this.scriptStartTo.Size = new System.Drawing.Size(61, 22);
            this.scriptStartTo.TabIndex = 2;
            this.scriptStartTo.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(155, 164);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 17);
            this.label1.TabIndex = 3;
            this.label1.Text = "Start in";
            // 
            // buttonOK
            // 
            this.buttonOK.Location = new System.Drawing.Point(140, 405);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(99, 25);
            this.buttonOK.TabIndex = 4;
            this.buttonOK.Text = "OK";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += new System.EventHandler(this.buttonOK_Click);
            // 
            // checkBoxSecure
            // 
            this.checkBoxSecure.AutoSize = true;
            this.checkBoxSecure.Location = new System.Drawing.Point(32, 22);
            this.checkBoxSecure.Name = "checkBoxSecure";
            this.checkBoxSecure.Size = new System.Drawing.Size(149, 21);
            this.checkBoxSecure.TabIndex = 5;
            this.checkBoxSecure.Text = "Secure device files";
            this.checkBoxSecure.UseVisualStyleBackColor = true;
            // 
            // checkBoxRemove
            // 
            this.checkBoxRemove.AutoSize = true;
            this.checkBoxRemove.Location = new System.Drawing.Point(32, 49);
            this.checkBoxRemove.Name = "checkBoxRemove";
            this.checkBoxRemove.Size = new System.Drawing.Size(133, 21);
            this.checkBoxRemove.TabIndex = 5;
            this.checkBoxRemove.Text = "Remove unused";
            this.checkBoxRemove.UseVisualStyleBackColor = true;
            // 
            // checkBoxAuto
            // 
            this.checkBoxAuto.AutoSize = true;
            this.checkBoxAuto.Location = new System.Drawing.Point(32, 76);
            this.checkBoxAuto.Name = "checkBoxAuto";
            this.checkBoxAuto.Size = new System.Drawing.Size(131, 21);
            this.checkBoxAuto.TabIndex = 5;
            this.checkBoxAuto.Text = "Auto sync every";
            this.checkBoxAuto.UseVisualStyleBackColor = true;
            this.checkBoxAuto.CheckedChanged += new System.EventHandler(this.checkBoxAuto_CheckedChanged);
            // 
            // numericUpDownAutoSec
            // 
            this.numericUpDownAutoSec.Enabled = false;
            this.numericUpDownAutoSec.Location = new System.Drawing.Point(214, 75);
            this.numericUpDownAutoSec.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
            this.numericUpDownAutoSec.Minimum = new decimal(new int[] {
            10,
            0,
            0,
            0});
            this.numericUpDownAutoSec.Name = "numericUpDownAutoSec";
            this.numericUpDownAutoSec.Size = new System.Drawing.Size(61, 22);
            this.numericUpDownAutoSec.TabIndex = 2;
            this.numericUpDownAutoSec.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(281, 75);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(61, 17);
            this.label2.TabIndex = 3;
            this.label2.Text = "seconds";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(281, 164);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(61, 17);
            this.label3.TabIndex = 6;
            this.label3.Text = "seconds";
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.label5);
            this.groupBox2.Controls.Add(this.textBoxScriptTelit);
            this.groupBox2.Controls.Add(this.label4);
            this.groupBox2.Controls.Add(this.textBoxScriptCommon);
            this.groupBox2.Location = new System.Drawing.Point(13, 227);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(357, 90);
            this.groupBox2.TabIndex = 2;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Initial script sources (URLs)";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(34, 59);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(80, 17);
            this.label5.TabIndex = 3;
            this.label5.Text = "Telit scripts";
            // 
            // textBoxScriptTelit
            // 
            this.textBoxScriptTelit.Location = new System.Drawing.Point(120, 56);
            this.textBoxScriptTelit.Name = "textBoxScriptTelit";
            this.textBoxScriptTelit.Size = new System.Drawing.Size(232, 22);
            this.textBoxScriptTelit.TabIndex = 2;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(6, 31);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(108, 17);
            this.label4.TabIndex = 1;
            this.label4.Text = "Common scripts";
            // 
            // textBoxScriptCommon
            // 
            this.textBoxScriptCommon.Location = new System.Drawing.Point(120, 28);
            this.textBoxScriptCommon.Name = "textBoxScriptCommon";
            this.textBoxScriptCommon.Size = new System.Drawing.Size(232, 22);
            this.textBoxScriptCommon.TabIndex = 0;
            // 
            // groupBox3
            // 
            this.groupBox3.Controls.Add(this.buttonPortCheck);
            this.groupBox3.Controls.Add(this.comboBoxCOMSpeed);
            this.groupBox3.Controls.Add(this.comboBoxCOMPort);
            this.groupBox3.Controls.Add(this.label6);
            this.groupBox3.Location = new System.Drawing.Point(13, 323);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(357, 76);
            this.groupBox3.TabIndex = 4;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Debug settings";
            // 
            // buttonPortCheck
            // 
            this.buttonPortCheck.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.buttonPortCheck.Enabled = false;
            this.buttonPortCheck.Location = new System.Drawing.Point(312, 17);
            this.buttonPortCheck.Name = "buttonPortCheck";
            this.buttonPortCheck.Size = new System.Drawing.Size(30, 30);
            this.buttonPortCheck.TabIndex = 5;
            this.buttonPortCheck.UseVisualStyleBackColor = true;
            // 
            // comboBoxCOMSpeed
            // 
            this.comboBoxCOMSpeed.FormattingEnabled = true;
            this.comboBoxCOMSpeed.Location = new System.Drawing.Point(202, 27);
            this.comboBoxCOMSpeed.Name = "comboBoxCOMSpeed";
            this.comboBoxCOMSpeed.Size = new System.Drawing.Size(104, 24);
            this.comboBoxCOMSpeed.TabIndex = 4;
            this.comboBoxCOMSpeed.SelectedIndexChanged += new System.EventHandler(this.comboBoxCOMSpeed_SelectedIndexChanged);
            // 
            // comboBoxCOMPort
            // 
            this.comboBoxCOMPort.FormattingEnabled = true;
            this.comboBoxCOMPort.Location = new System.Drawing.Point(97, 27);
            this.comboBoxCOMPort.Name = "comboBoxCOMPort";
            this.comboBoxCOMPort.Size = new System.Drawing.Size(99, 24);
            this.comboBoxCOMPort.TabIndex = 4;
            this.comboBoxCOMPort.SelectedIndexChanged += new System.EventHandler(this.comboBoxCOMPort_SelectedIndexChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(23, 30);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(68, 17);
            this.label6.TabIndex = 1;
            this.label6.Text = "COM port";
            // 
            // SettingsForm
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(382, 442);
            this.Controls.Add(this.groupBox3);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.checkBoxRemove);
            this.Controls.Add(this.checkBoxAuto);
            this.Controls.Add(this.checkBoxSecure);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.numericUpDownAutoSec);
            this.Controls.Add(this.scriptStartTo);
            this.Controls.Add(this.groupBox1);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Parameters";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.SettingsForm_FormClosed);
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scriptStartTo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownAutoSec)).EndInit();
            this.groupBox2.ResumeLayout(false);
            this.groupBox2.PerformLayout();
            this.groupBox3.ResumeLayout(false);
            this.groupBox3.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.RadioButton radioButton1;
        private System.Windows.Forms.RadioButton radioButton2;
        private System.Windows.Forms.RadioButton radioButton0;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.ToolTip toolTip;
        private System.Windows.Forms.NumericUpDown scriptStartTo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button buttonOK;
        private System.Windows.Forms.CheckBox checkBoxSecure;
        private System.Windows.Forms.CheckBox checkBoxRemove;
        private System.Windows.Forms.CheckBox checkBoxAuto;
        private System.Windows.Forms.NumericUpDown numericUpDownAutoSec;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label label6;
        public System.Windows.Forms.TextBox textBoxScriptTelit;
        public System.Windows.Forms.TextBox textBoxScriptCommon;
        public System.Windows.Forms.ComboBox comboBoxCOMSpeed;
        public System.Windows.Forms.ComboBox comboBoxCOMPort;
        public System.Windows.Forms.Button buttonPortCheck;
    }
}