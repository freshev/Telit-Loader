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
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scriptStartTo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownAutoSec)).BeginInit();
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
            this.groupBox1.Size = new System.Drawing.Size(136, 118);
            this.groupBox1.TabIndex = 1;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "AT#startmodescr";
            // 
            // scriptStartTo
            // 
            this.scriptStartTo.Location = new System.Drawing.Point(259, 162);
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
            this.label1.Size = new System.Drawing.Size(98, 17);
            this.label1.TabIndex = 3;
            this.label1.Text = "script_start_to";
            // 
            // buttonOK
            // 
            this.buttonOK.Location = new System.Drawing.Point(13, 227);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(358, 25);
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
            this.numericUpDownAutoSec.Location = new System.Drawing.Point(169, 75);
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
            this.label2.Location = new System.Drawing.Point(236, 77);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(61, 17);
            this.label2.TabIndex = 3;
            this.label2.Text = "seconds";
            // 
            // SettingsForm
            // 
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(382, 261);
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
            this.Text = "Sync settings";
            this.Load += new System.EventHandler(this.SettingsForm_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.scriptStartTo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numericUpDownAutoSec)).EndInit();
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
    }
}