using System;
using System.IO.Ports;
using System.Threading;
using System.Windows.Forms;

namespace TelitLoader {
    public partial class SettingsForm : Form {
        public int Mode { get; set; }
        public int Runtime { get; set; }
        public bool Secured { get; set; }
        public bool Remove { get; set; }
        public bool Auto { get; set;  }
        public int AutoSec { get; set; }
        public string ScriptCommon { get; set;  }
        public string ScriptTelit { get; set; }
        public string DebugCOMPort { get; set; }
        public string DebugCOMSpeed { get; set; }
        private MainForm mainForm;
        private bool canUsePort = false;
        private bool deviceOpened = false;        

        public SettingsForm(MainForm mainForm) {            
            InitializeComponent();

            this.mainForm = mainForm;
            string secondTT = "current script will be executed at startup only if the user does not send\n" +
                                             "any AT command on the serial port for the time interval specified in\n" +
                                             "<script_start_to> parameter, otherwise the Easy Script® interpreter will\n" +
                                             "not execute and the MODULE will behave normally answering only to\n" +
                                             "AT commands on the serial port.The DTR line is not tested";
            toolTip.SetToolTip(radioButton0, "current script will be executed at startup only if the DTR line is found\n" + 
                                             "Low(that is: COM is not open on a PC), otherwise the Easy Script®\n" +
                                             "interpreter will not execute and the MODULE will behave normally\n" +
                                             "answering only to AT commands on the serial port(factory default).");
            toolTip.SetToolTip(radioButton1, secondTT);
            toolTip.SetToolTip(radioButton2, "current script will be executed at startup in any case. DTR line and if\n" +
                                             "the user does not send any AT command on the serial port have no\n" +
                                             "influence on script execution.But AT command interface will be\n" +
                                             "available on serial port ASC0 and connected to third AT parser instance.\n" +
                                             "See ”Easy Script in Python” document for further details on this\n" +
                                             "execution start mode.");
            toolTip.SetToolTip(scriptStartTo, secondTT);
            toolTip.SetToolTip(label1, secondTT);
            toolTip.SetToolTip(checkBoxSecure, "Hide script content on the device.");
            toolTip.SetToolTip(checkBoxRemove, "Remove unused scripts from device during Sync");
            toolTip.SetToolTip(checkBoxAuto, "Periodically synchronize device and DB files with local files");
            toolTip.SetToolTip(numericUpDownAutoSec, "Synchronize every n-th second");
            toolTip.SetToolTip(textBoxScriptCommon, "URL with files to download");
            toolTip.SetToolTip(textBoxScriptTelit, "URL with files to download.\n" +
                               "File names that match file names from \"Common\" folder will be overwritten.");
            Mode = 1;
            Runtime = 10;
            Secured = true;
            Remove = true;
            Auto = false;
            AutoSec = 10;
            buttonPortCheck.BackgroundImage = Properties.Resources.Disconnected;
            DebugCOMPort = "";
            DebugCOMSpeed = "";
        }

        private void buttonOK_Click(object sender, EventArgs e) {            
            if (radioButton0.Checked) Mode = 0;
            if (radioButton1.Checked) Mode = 1;
            if (radioButton2.Checked) Mode = 2;
            Runtime = (int)scriptStartTo.Value;
            Secured = checkBoxSecure.Checked;
            Remove = checkBoxRemove.Checked;
            Auto = checkBoxAuto.Checked;
            AutoSec = (int)numericUpDownAutoSec.Value;
            ScriptCommon = textBoxScriptCommon.Text + (!textBoxScriptCommon.Text.EndsWith("/") ? "/" : "");
            ScriptTelit = textBoxScriptTelit.Text + (!textBoxScriptTelit.Text.EndsWith("/") ? "/" : "");
            Close();
        }
        private void SettingsForm_FormClosed(object sender, FormClosedEventArgs e) {
            if (mainForm != null && mainForm.debugDevice != null) mainForm.debugDevice.ClosePort();
        }

        private void SettingsForm_Load(object sender, EventArgs e) {
            if (Mode == 0) radioButton0.Checked = true;
            if (Mode == 1) radioButton1.Checked = true;
            if (Mode == 2) radioButton2.Checked = true;
            if (Runtime >= 10 && Runtime <= 60) scriptStartTo.Value = Runtime;
            checkBoxSecure.Checked = Secured;
            checkBoxRemove.Checked = Remove;
            checkBoxAuto.Checked = Auto;
            if (AutoSec >= 10 && AutoSec <= 60) numericUpDownAutoSec.Value = AutoSec;

            textBoxScriptCommon.Text = ScriptCommon;
            textBoxScriptTelit.Text = ScriptTelit; 

            comboBoxCOMPort.Items.Clear();
            comboBoxCOMPort.Items.AddRange(SerialPort.GetPortNames());
            if (DebugCOMPort != null && comboBoxCOMPort.Items.Contains(DebugCOMPort)) comboBoxCOMPort.SelectedItem = DebugCOMPort;
            else if (comboBoxCOMPort.Items.Count > 0) comboBoxCOMPort.SelectedItem = comboBoxCOMPort.Items[0];

            comboBoxCOMSpeed.Items.AddRange(new string[] { "9600", "19200", "38400", "57600", "115200" });
            if (DebugCOMSpeed != null && comboBoxCOMSpeed.Items.Contains(DebugCOMSpeed)) comboBoxCOMSpeed.SelectedItem = DebugCOMSpeed;
            else if (comboBoxCOMSpeed.Items.Count > 0) comboBoxCOMSpeed.SelectedItem = comboBoxCOMSpeed.Items[0];
        }

        private void radioButton0_CheckedChanged(object sender, EventArgs e) {
            scriptStartTo.Enabled = false;
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e) {
            scriptStartTo.Enabled = true;
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e) {
            scriptStartTo.Enabled = false;
        }

        private void checkBoxAuto_CheckedChanged(object sender, EventArgs e) {
            numericUpDownAutoSec.Enabled = checkBoxAuto.Checked;
        }

        private void comboBoxCOMPort_SelectedIndexChanged(object sender, EventArgs e) {
            if (mainForm !=null && mainForm.debugDevice != null && canUsePort == true) {
                if (mainForm.debugDevice.getPort() != null) {
                    if (mainForm.debugDevice.getPort().PortName != comboBoxCOMPort.Text) mainForm.debugDevice.ClosePort();
                    else mainForm.debugDevice.CloseMainPort();
                }
                if(mainForm.toolStripComboBoxPort.SelectedItem.ToString() == comboBoxCOMPort.Text) deviceOpened = mainForm.debugDevice.OpenMainPort();
                else deviceOpened = mainForm.debugDevice.OpenPort();
            }
            DebugCOMPort = comboBoxCOMPort.SelectedItem.ToString();
        }

        private void comboBoxCOMSpeed_SelectedIndexChanged(object sender, EventArgs e) {
            bool speedChanged = false;
            if (deviceOpened) {
                string speed = comboBoxCOMSpeed.SelectedItem.ToString();
                if (mainForm != null && mainForm.device != null && mainForm.debugDevice != null && MessageBox.Show(this, "Change DebugPort port speed to " + speed + " ?", "Change device settings", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                    if (mainForm.device.SetDebugSpeed(speed)) {
                        mainForm.ShowStatus("Changed DebugPort speed to " + speed);
                        speedChanged = true;
                        //Thread.Sleep(500);
                    }
                }
            }

            if (mainForm != null && mainForm.debugDevice != null && !speedChanged) {
                if (mainForm.debugDevice.getPort() != null) {
                    if (mainForm.debugDevice.getPort().PortName != comboBoxCOMPort.Text) mainForm.debugDevice.ClosePort();
                    else mainForm.debugDevice.CloseMainPort();
                }
                if (mainForm.toolStripComboBoxPort.SelectedItem.ToString() == comboBoxCOMPort.Text) deviceOpened = mainForm.debugDevice.OpenMainPort();
                else deviceOpened = mainForm.debugDevice.OpenPort();
            }
            DebugCOMSpeed = comboBoxCOMSpeed.SelectedItem.ToString();
            
            canUsePort = true;
        }
        
    }
}
