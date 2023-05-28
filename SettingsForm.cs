using System;

using System.Windows.Forms;

namespace TelitLoader {
    public partial class SettingsForm : Form {
        public int Mode { get; set; }
        public int Runtime { get; set; }
        public bool Secured { get; set; }
        public bool Remove { get; set; }
        public bool Auto { get; set;  }
        public int AutoSec { get; set; }

        public SettingsForm() {
            InitializeComponent();           
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
            Mode = 1;
            Runtime = 10;
            Secured = true;
            Remove = true;
            Auto = false;
            AutoSec = 10;
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
            Close();
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
    }
}
