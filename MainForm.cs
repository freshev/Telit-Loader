using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Ports;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Shell32;
using System.Diagnostics;
using System.Net;
using ICSharpCode.SharpZipLib.Zip.Compression.Streams;
using System.Xml;
using System.Runtime.InteropServices;

/*
at "C:\Program Files (x86)\Python\Pythonwin\pywin\framework\app.py" 
    def OnIdle(self, count):
        if not hasattr(self, 'opened'):
            fileName = win32ui.GetRecentFileList()[0]
            doc = win32ui.GetApp().OpenDocumentFile(fileName)
            self.opened = 1
        try: ...
*/

namespace TelitLoader {
    public partial class MainForm : Form {

        #region Vars
        const string scriptCommon = "https://asque.ru/firmware/Device/";
        const string scriptTelit = "https://asque.ru/firmware/Telit/";
        const string dbFileName = "Settings.db";
        string dbPath = "";
        List<Script> deviceScripts = new List<Script>();
        List<Script> dbSourceScripts = new List<Script>();
        List<Script> dbCompiledScripts = new List<Script>();
        List<Script> localScripts = new List<Script>();
        public byte[] key;
        public byte[] iv;
        public Device device = null;
        public DebugDevice debugDevice = null;
        string localDir = "";
        string comPort = "";
        string comSpeed = "115200";
        bool canUsePort = false;
        public bool deviceOpened = false;
        public bool debugStarted = false;
        DataGridView currentDGV = null;
        string currentFile = null;
        List<string> currentFiles = new List<string>();        
        bool inSelectionChange = false;
        bool inCodeTextBox = false;
        bool forceOverwrite = false;
        static Shell shell = new Shell();
        static Folder RecyclingBin = shell.NameSpace(10);
        SettingsForm settings = null;
        FileSystemWatcher fileSystemWatcher = new FileSystemWatcher();
        string richTextBoxError = "";
        bool syncMode = false;
        public int DeviceType = 1;
        #endregion

        #region Constructor
        public MainForm() {
            ServicePointManager.Expect100Continue = true;
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            InitializeComponent();
            codeRichTextBox.Font = new Font(FontFamily.GenericMonospace, codeRichTextBox.Font.SizeInPoints);
        }

        private void FormMain_Load(object sender, EventArgs e) {
            dbPath = Path.Combine(Environment.CurrentDirectory, dbFileName);
            device = new Device(this);
            Initialize();            
            debugDevice = new DebugDevice(this, settings);
            UpdateInterface();
        }
        #endregion

        #region Initialize
        private void Initialize() {
            Thread.CurrentThread.CurrentUICulture = CultureInfo.InvariantCulture;
            byte[] sl = new byte[] { 0xE5, 0xF1, 0xCC, 0x1F, 0x0A, 0x57, 0x38, 0x19 };
            Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes("let!@forgot#db", sl, 10000);
            key = pdb.GetBytes(32);
            iv = pdb.GetBytes(16);

            settings = new SettingsForm(this);
            if (settings.ScriptCommon == null) settings.ScriptCommon = scriptCommon;
            if (settings.ScriptTelit == null) settings.ScriptTelit = scriptTelit;
            if (!File.Exists(dbPath))
                if (!CreateScriptDB()) ShowStatus("Can not create new Script database"); ;
            if (!ReadScriptDB()) ShowStatus("Can not load Script database");            

            toolStripComboBoxPort.Items.AddRange(SerialPort.GetPortNames());
            if (toolStripComboBoxPort.Items.Contains(comPort)) toolStripComboBoxPort.SelectedItem = comPort;
            else if (toolStripComboBoxPort.Items.Count > 0) toolStripComboBoxPort.SelectedItem = toolStripComboBoxPort.Items[0];
            toolStripComboBoxSpeed.Items.AddRange(new string[] { "9600", "19200", "38400", "57600", "115200" });
            if (toolStripComboBoxSpeed.Items.Contains(comSpeed)) toolStripComboBoxSpeed.SelectedItem = comSpeed;

            groupBox1.Click += GroupBox1_Click;
            groupBox4.Click += localFolder_Click;
            codeRichTextBox.GotFocus += CodeRichTextBox_GotFocus;

            fileSystemWatcher.Path = localDir;
            fileSystemWatcher.IncludeSubdirectories = false;
            fileSystemWatcher.NotifyFilter = NotifyFilters.FileName | NotifyFilters.Attributes | NotifyFilters.Size | NotifyFilters.LastWrite;
            fileSystemWatcher.Changed += FileSystemWatcher_Changed;
            fileSystemWatcher.Created += FileSystemWatcher_Created;
            fileSystemWatcher.Deleted += FileSystemWatcher_Deleted;
            fileSystemWatcher.Renamed += FileSystemWatcher_Renamed;
            fileSystemWatcher.EnableRaisingEvents = true;

            toolTip.SetToolTip(groupBox1, "Files on the destination Telit device. Click to refresh");
            toolTip.SetToolTip(groupBox2, "Files in the Script database (source code)");
            toolTip.SetToolTip(groupBox3, "Files in the Script database (compiled code)");
            toolTip.SetToolTip(groupBox4, "Files in the local folder. Click to change.");
            toolTip.SetToolTip(b1to2up, "Copy device source file(s) to script database");
            toolTip.SetToolTip(b2to1up, "Copy script database source file(s) to device");
            toolTip.SetToolTip(b2to1down, "Copy script database compiled file(s) to device");
            toolTip.SetToolTip(b2to3up, "Copy script database source file(s) to local folder");
            toolTip.SetToolTip(b3to2up, "Copy local file(s) to script database source");
            toolTip.SetToolTip(b3to2down, "Compile and store local file(s) to script database (using Python 1.5.2)");

            foreach (ToolStripMenuItem item in toolStripDTSelect.DropDownItems) item.Click += DeviceTypeItem_Click;
        }

        private void DeviceTypeItem_Click(object sender, EventArgs e) {
            DeviceType = Convert.ToInt32((sender as ToolStripMenuItem).Tag);
            UpdateDTSelectMenu();
        }
        private void UpdateDTSelectMenu() {
            foreach (ToolStripMenuItem item in toolStripDTSelect.DropDownItems) item.Checked = Convert.ToInt32(item.Tag) == DeviceType;
            toolStripDT.Text = "DT:" + DeviceType.ToString();
        }


        private void FileSystemWatcher_Changed(object sender, FileSystemEventArgs e) { FillLocalFiles(); UpdateLocalFiles(); }
        private void FileSystemWatcher_Renamed(object sender, RenamedEventArgs e) { FillLocalFiles(); UpdateLocalFiles(); }
        private void FileSystemWatcher_Deleted(object sender, FileSystemEventArgs e) { FillLocalFiles(); UpdateLocalFiles(); }
        private void FileSystemWatcher_Created(object sender, FileSystemEventArgs e) { FillLocalFiles(); UpdateLocalFiles(); }
        #endregion

        #region Script DB

        #region CreateScriptDB
        public bool CreateScriptDB() {
            dbSourceScripts = new List<Script>();
            dbCompiledScripts = new List<Script>();
            return WriteScriptDB();
        }
        #endregion

        #region ReadScriptDB
        public bool ReadScriptDB() {
            bool res = false;
            string tempLocalDir = localDir;
            try {
                if (File.Exists(dbPath)) {
                    FileStream fs = new FileStream(dbPath, FileMode.Open, FileAccess.Read);
                    AesManaged aes = new AesManaged();
                    CryptoStream cryptoStream = new CryptoStream(fs, aes.CreateDecryptor(key, iv), CryptoStreamMode.Read);
                    BinaryFormatter bf = new BinaryFormatter();
                    DeviceType = (int)bf.Deserialize(cryptoStream);
                    comPort = (string)bf.Deserialize(cryptoStream);
                    comSpeed = (string)bf.Deserialize(cryptoStream);
                    tempLocalDir = (string)bf.Deserialize(cryptoStream);
                    device.Secured = settings.Secured = (bool)bf.Deserialize(cryptoStream);
                    settings.Remove = (bool)bf.Deserialize(cryptoStream);
                    settings.Auto = (bool)bf.Deserialize(cryptoStream);
                    settings.AutoSec = (int)bf.Deserialize(cryptoStream);
                    settings.Mode = (int)bf.Deserialize(cryptoStream);
                    settings.Runtime = (int)bf.Deserialize(cryptoStream);
                    settings.ScriptCommon = (string)bf.Deserialize(cryptoStream);
                    settings.ScriptTelit = (string)bf.Deserialize(cryptoStream);
                    settings.DebugCOMPort = (string)bf.Deserialize(cryptoStream);
                    settings.DebugCOMSpeed = (string)bf.Deserialize(cryptoStream);
                    dbSourceScripts = (List<Script>)bf.Deserialize(cryptoStream);
                    dbCompiledScripts = (List<Script>)bf.Deserialize(cryptoStream);
                    cryptoStream.Close();
                    fs.Close();
                    res = true;
                }
            } catch {
                try { File.Delete(dbPath); } catch { }
                CreateScriptDB();
            }
            if (Directory.Exists(tempLocalDir)) localDir = tempLocalDir;
            else {                
                string telitFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Telit");
                try {
                    if (!Directory.Exists(telitFolder)) Directory.CreateDirectory(telitFolder);
                    localDir = telitFolder;
                } catch {
                    localDir = Environment.CurrentDirectory;
                }
            }            

            FillLocalFiles();
            UpdateDTSelectMenu();
            UpdateDBSourceFiles();
            UpdateDBCompiledFiles();
            UpdateLocalFiles();
            return res;
        }
        #endregion

        #region WriteScriptDB
        public bool WriteScriptDB() {
            FileStream fs = null;
            bool res = false;
            try { DeviceType = Convert.ToInt32(toolStripDT.Text.Substring(3)); } catch { }
            try {
                if (File.Exists(dbPath)) fs = new FileStream(dbPath, FileMode.Truncate);
                else fs = new FileStream(dbPath, FileMode.CreateNew);
                AesManaged aes = new AesManaged();
                CryptoStream cryptoStream = new CryptoStream(fs, aes.CreateEncryptor(key, iv), CryptoStreamMode.Write);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(cryptoStream, DeviceType);
                bf.Serialize(cryptoStream, comPort);
                bf.Serialize(cryptoStream, comSpeed);
                bf.Serialize(cryptoStream, localDir);
                bf.Serialize(cryptoStream, settings.Secured);
                bf.Serialize(cryptoStream, settings.Remove);
                bf.Serialize(cryptoStream, settings.Auto);
                bf.Serialize(cryptoStream, settings.AutoSec);
                bf.Serialize(cryptoStream, settings.Mode);
                bf.Serialize(cryptoStream, settings.Runtime);
                bf.Serialize(cryptoStream, settings.ScriptCommon);
                bf.Serialize(cryptoStream, settings.ScriptTelit);
                bf.Serialize(cryptoStream, settings.DebugCOMPort);
                bf.Serialize(cryptoStream, settings.DebugCOMSpeed);
                bf.Serialize(cryptoStream, dbSourceScripts);
                bf.Serialize(cryptoStream, dbCompiledScripts);
                cryptoStream.Close();
                res = true;
            } catch { }

            if (fs != null) {
                fs.Close();
                fs.Dispose();
                fs = null;
            }
            return res;
        }
        #endregion

        #endregion

        #region ComboBoxPort
        private void toolStripComboBoxPort_SelectedIndexChanged(object sender, EventArgs e) {
            if (device != null && canUsePort == true) {
                device.ClosePort();
                device.OpenPort();
            }
            comPort = toolStripComboBoxPort.SelectedItem.ToString();
        }

        private void toolStripComboBoxSpeed_SelectedIndexChanged(object sender, EventArgs e) {
            bool speedChanged = false;
            if(deviceOpened) {
                string speed = toolStripComboBoxSpeed.SelectedItem.ToString();
                if (device != null && MessageBox.Show(this, "Change device COM port speed to " + speed + " ?", "Change device settings", MessageBoxButtons.YesNo) == DialogResult.Yes) {
                    if(device.SetSpeed(speed)) {                        
                        ShowStatus("Changed device speed to " + speed);
                        speedChanged = true;
                        //Thread.Sleep(500);
                    }                    
                }
            }

            if (device != null && !speedChanged) {
                device.ClosePort();
                device.OpenPort();
            }
            comSpeed = toolStripComboBoxSpeed.SelectedItem.ToString();
            canUsePort = true;
        }
        #endregion

        #region Close
        private void FormMain_FormClosed(object sender, FormClosedEventArgs e) {
            WriteScriptDB();
            if (device != null) device.ClosePort();
        }
        #endregion

        #region Show
        public void ShowStatus(string text) {
            MethodInvoker method = delegate { toolStripStatusLabel.Text = text; };
            if (InvokeRequired) BeginInvoke(method); else method.Invoke();
        }
        public void ShowContent(List<Script> scripts) {
            try {
                foreach (Script script in scripts) {
                    if (script.name == currentFile) {
                        if (script.type == ScriptType.Binary || script.type == ScriptType.PythonCompiled || script.type == ScriptType.PythonOptimized) {
                            ShowContent("Binary file");
                        } else {
                            string content = "";
                            if (script.content == null) {
                                try {
                                    string path = Path.Combine(localDir, script.name);
                                    if (File.Exists(path)) {
                                        StreamReader sr = new StreamReader(path);
                                        content = sr.ReadToEnd();
                                        sr.Close();
                                        if (content.Length > 100 * 1024) content = content.Substring(0, 100 * 1024) + "\r...";
                                    }
                                } catch { }
                            } else content = Encoding.UTF8.GetString(script.content);
                            ShowContent(content);
                        }
                    }
                }
            } catch { }
        }
        public void ShowContent(string content, int lineNum = -1, string errString = "") {
            MethodInvoker method = delegate {
                try {
                    codeRichTextBox.Hide();
                    codeRichTextBox.Text = content + "\r\n \r\n";
                    if (toolStripButtonLog.Checked) {
                        if (codeRichTextBox.Text.Length > 5) codeRichTextBox.Select(codeRichTextBox.Text.Length - 5, 0);
                        codeRichTextBox.ScrollToCaret();
                    }
                    if(lineNum != -1 && lineNum < codeRichTextBox.Lines.Length) {
                        if(codeRichTextBox.Text.Contains(codeRichTextBox.Lines[lineNum])) {
                            int startTextPos = 0;
                            int errStartTextPos = codeRichTextBox.Text.IndexOf(codeRichTextBox.Lines[lineNum]);
                            int errStopTextPos = errStartTextPos + 100;

                            if (lineNum >= 3) {
                                string partText = codeRichTextBox.Text.Substring(0, errStartTextPos - 1);
                                startTextPos = errStartTextPos;
                                for (int i = 0; i < 3 && startTextPos != -1; i++) { // move 3 lines upper
                                    startTextPos = partText.LastIndexOf('\n');
                                    partText = partText.Substring(0, startTextPos - 1);
                                }
                                if(startTextPos == -1) startTextPos = 0;
                            }
                            /*if(lineNum !=-1 && !"".Equals(errString) && errString.Contains("^")) {
                                string[] sa = errString.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                                if (sa.Length > 2 && sa[sa.Length - 2].Contains("^")) {
                                    string downString = sa[sa.Length - 2];
                                    errStartTextPos += downString.IndexOf('^');
                                    errStopTextPos = errStartTextPos + 1;
                                }
                            }*/
                            
                            codeRichTextBox.Select(startTextPos, 0);
                            codeRichTextBox.ScrollToCaret();
                            //codeRichTextBox.Select(errStartTextPos, errStopTextPos - errStartTextPos);                            
                        }
                    }
                    codeRichTextBox.Show();
                    //codeRichTextBox.Update();
                } catch { }
            };
            if (InvokeRequired) BeginInvoke(method); else method.Invoke();
        }
        #endregion

        #region UpdateInterface
        public void UpdateInterface() {
            if (currentDGV != dataGridViewDevice) dataGridViewDevice.ClearSelection();
            if (currentDGV != dataGridViewDBSource) dataGridViewDBSource.ClearSelection();
            if (currentDGV != dataGridViewDBCompiled) dataGridViewDBCompiled.ClearSelection();
            if (currentDGV != dataGridViewLocal) dataGridViewLocal.ClearSelection();
            if (currentDGV == dataGridViewDevice) {
                bool moveEnabled = false;
                if (currentFiles.Count > 0) {
                    foreach(string file in currentFiles) {
                        //if (file.EndsWith(".py")) 
                            moveEnabled = true;
                    }
                }
                b1to2up.Enabled = moveEnabled;
                b2to1up.Enabled = false;
                b2to3up.Enabled = false;
                b2to1down.Enabled = false;
                b3to2up.Enabled = false;
                b3to2down.Enabled = false;
                toolStripButtonSetActive.Enabled = true;
                toolStripButtonDel.Enabled = true;
                toolStripButtonPlay.Enabled = true;
                toolStripButtonLog.Enabled = true;
                Script activeScript = GetActiveScript(currentDGV);
                if(activeScript != null && dataGridViewDevice.Rows[dataGridViewDevice.CurrentCell.RowIndex].Cells[0].Value.ToString() == activeScript.name) {
                    toolStripButtonSetActive.Checked = true;
                    toolStripButtonSetActive.ToolTipText = "Deactivate script";
                    deviceContextMenu.Items[0].Enabled = false;
                    deviceContextMenu.Items[1].Enabled = true;
                } else {
                    toolStripButtonSetActive.Checked = false;
                    toolStripButtonSetActive.ToolTipText = "Set script as active";
                    deviceContextMenu.Items[0].Enabled = true;
                    deviceContextMenu.Items[1].Enabled = false;
                }                
            }
            if (currentDGV == dataGridViewDBSource) {
                b1to2up.Enabled = false;
                b2to1up.Enabled = true;
                b2to3up.Enabled = true;
                b2to1down.Enabled = false;
                b3to2up.Enabled = false;
                b3to2down.Enabled = false;
                toolStripButtonSetActive.Enabled = true;
                toolStripButtonDel.Enabled = true;
                toolStripButtonPlay.Enabled = false;
                toolStripButtonLog.Enabled = false;
            }
            if (currentDGV == dataGridViewDBCompiled) {
                b1to2up.Enabled = false;
                b2to1up.Enabled = false;
                b2to3up.Enabled = false;
                b2to1down.Enabled = true;
                b3to2up.Enabled = false;
                b3to2down.Enabled = false;
                toolStripButtonSetActive.Enabled = true;
                toolStripButtonDel.Enabled = true;
                toolStripButtonPlay.Enabled = false;
                toolStripButtonLog.Enabled = false;
            }
            if (currentDGV == dataGridViewLocal) {
                b1to2up.Enabled = false;
                b2to1up.Enabled = false;
                b2to3up.Enabled = false;
                b2to1down.Enabled = false;
                b3to2up.Enabled = true;
                b3to2down.Enabled = true;
                toolStripButtonSetActive.Enabled = false;
                toolStripButtonDel.Enabled = true;
                toolStripButtonPlay.Enabled = false;
                toolStripButtonLog.Enabled = false;
            }
        }
        #endregion

        #region UpdateDeviceFiles
        public void FillDeviceFiles(List<Script> files) {
            if (files != null) {
                deviceScripts.Clear();
                foreach (Script file in files) deviceScripts.Add(file);
            }
            UpdateDeviceFiles();
        }
        public void UpdateDeviceFiles() {            
            MethodInvoker updateFiles = delegate {
                toolStripButtonSetActive.Checked = false;
                groupBox1.Text = "Device files (" + (device.GetFreeBytes() / 1024) + " kB free)";
                string temp = currentFile;
                Script activeScript = GetActiveScript(dataGridViewDevice);
                if (activeScript != null) temp = activeScript.name;
                int tempRow = 0;
                dataGridViewDevice.Rows.Clear();
                foreach (Script file in deviceScripts) {
                    DataGridViewRow row = new DataGridViewRow();
                    row.Cells.AddRange(new DataGridViewTextBoxCell(), new DataGridViewTextBoxCell());
                    row.Cells[0].Value = file.name;
                    row.Cells[1].Value = file.size;
                    dataGridViewDevice.Rows.Add(row);
                    string ttText = Script.getScriptDescription(file.type);
                    foreach (DataGridViewCell cell in row.Cells) cell.ToolTipText = ttText;
                    if(file.active) {
                        row.DefaultCellStyle.ForeColor = Color.Red;
                        row.DefaultCellStyle.SelectionForeColor = Color.Red;
                        row.Cells[0].ToolTipText = "Active script";
                        tempRow = row.Index;
                    }
                }
                foreach (DataGridViewRow row in dataGridViewDevice.Rows) {
                    if (row.Cells[0].Value.ToString() == temp) {
                        dataGridViewDevice.ClearSelection();
                        row.Selected = true;
                        //dataGridViewDevice.CurrentCell = dataGridViewDevice.Rows[row.Index].Cells[1];
                        //dataGridViewDevice.CurrentCell = dataGridViewDevice.Rows[row.Index].Cells[0];
                        /*if (tempRow != 0) {
                            DataGridViewCellEventArgs arg = new DataGridViewCellEventArgs(1, tempRow);
                            dataGridViewDevice_CellContentClick(dataGridViewDevice, arg);
                        }*/
                        break;
                    }
                }
            };
            if (InvokeRequired) BeginInvoke(updateFiles); else updateFiles.Invoke();
        }
        public void UpdateActiveDeviceScript(string scriptName) {
            Script activeScript = GetActiveScript(dataGridViewDevice);
            if (activeScript != null) activeScript.active = false;
            foreach (Script script in deviceScripts) {
                if (script.name == scriptName) script.active = true;
            }
            UpdateDeviceFiles();
        }

        public void DeleteDeviceFile(List<string> files) {
            List<Script> delScripts = new List<Script>();
            foreach (string file in files) {
                foreach (Script script in deviceScripts) {
                    if (script.name == file) delScripts.Add(script);
                }
            }
            bool deleteAll = false;
            if (delScripts.Count == dataGridViewDevice.RowCount) deleteAll = true;

            Cursor = Cursors.WaitCursor;
            if (deleteAll) {
                device.DeleteAllFiles();
            } else {
                foreach (Script script in delScripts) {
                    ShowStatus("Delete " + script.name + " ...");
                    device.DeleteFile(script.name);
                }
            }
            Cursor = Cursors.Default;
            Thread readFilesThread = new Thread(new ThreadStart(device.ReadFiles));
            readFilesThread.Start();
        }
        private void GroupBox1_Click(object sender, EventArgs e) {
            Thread readFilesThread = new Thread(new ThreadStart(device.ReadFiles));
            readFilesThread.Start();
        }
        #endregion

        #region Update DB Files
        public void UpdateDBSourceFiles() {
            MethodInvoker updateFiles = delegate {
                string temp = currentFile;
                dataGridViewDBSource.Rows.Clear();
                foreach (Script file in dbSourceScripts) {
                    DataGridViewRow row = new DataGridViewRow();
                    row.Cells.AddRange(new DataGridViewTextBoxCell(), new DataGridViewTextBoxCell(), new DataGridViewTextBoxCell());
                    row.Cells[0].Value = file.name;
                    row.Cells[1].Value = file.size;
                    row.Cells[2].Value = file.date;
                    dataGridViewDBSource.Rows.Add(row);
                    string ttText = Script.getScriptDescription(file.type);
                    foreach (DataGridViewCell cell in row.Cells) cell.ToolTipText = ttText;
                    if (file.active) {
                        row.DefaultCellStyle.ForeColor = Color.Red;
                        row.DefaultCellStyle.SelectionForeColor = Color.Red;
                        row.Cells[0].ToolTipText = "Active script";                        
                    } 
                }
                foreach (DataGridViewRow row in dataGridViewDBSource.Rows) {
                    if (row.Cells[0].Value.ToString() == temp) {
                        dataGridViewDBSource.ClearSelection();
                        row.Selected = true;
                        break;
                    }
                }
            };
            if (InvokeRequired) BeginInvoke(updateFiles); else updateFiles.Invoke();
        }
        public void UpdateDBCompiledFiles() {
            MethodInvoker updateFiles = delegate {
                string temp = currentFile;
                dataGridViewDBCompiled.Rows.Clear();
                foreach (Script file in dbCompiledScripts) {
                    DataGridViewRow row = new DataGridViewRow();
                    row.Cells.AddRange(new DataGridViewTextBoxCell(), new DataGridViewTextBoxCell(), new DataGridViewTextBoxCell());
                    row.Cells[0].Value = file.name;
                    row.Cells[1].Value = file.size;
                    row.Cells[2].Value = file.date.ToString("dd.MM.yy HH:mm");
                    dataGridViewDBCompiled.Rows.Add(row);
                    string ttText = Script.getScriptDescription(file.type);
                    foreach (DataGridViewCell cell in row.Cells) cell.ToolTipText = ttText;
                    if (file.active) {
                        row.DefaultCellStyle.ForeColor = Color.Red;
                        row.DefaultCellStyle.SelectionForeColor = Color.Red;
                        row.Cells[0].ToolTipText = "Active script";
                    }

                }
                foreach (DataGridViewRow row in dataGridViewDBCompiled.Rows) {
                    if (row.Cells[0].Value.ToString() == temp) {
                        dataGridViewDBCompiled.ClearSelection();
                        row.Selected = true;
                        break;
                    }
                }
            };
            if (InvokeRequired) BeginInvoke(updateFiles); else updateFiles.Invoke();
        }
        public void DeleteDBSourceFile(List<string> files) {
            List<Script> delScripts = new List<Script>();
            foreach (string file in files) {
                foreach (Script script in dbSourceScripts) {
                    if (script.name == file) delScripts.Add(script);
                }
            }
            foreach (Script script in delScripts) dbSourceScripts.Remove(script);
            UpdateDBSourceFiles();
        }
        public void DeleteDBCompiledFile(List<string> files) {
            List<Script> delScripts = new List<Script>();
            foreach (string file in files) {
                foreach (Script script in dbCompiledScripts) {
                    if (script.name == file) delScripts.Add(script);
                }
            }
            foreach (Script script in delScripts) dbCompiledScripts.Remove(script);            
            UpdateDBCompiledFiles();
        }
        #endregion

        #region Update Local Files
        public void FillLocalFiles() {
            lock(localScripts) {
                localScripts.Clear();
                string[] files = Directory.GetFiles(localDir);
                foreach (string file in files) {
                    try {
                        FileInfo fi = new FileInfo(file);
                        string name = fi.Name;
                        DateTime date = fi.LastWriteTime;
                        long size = fi.Length;
                        Script script = new Script(ScriptStoreType.Local, name, size, date);
                        localScripts.Add(script);
                    } catch { }
                }
            }
        }
        public void UpdateLocalFiles() {
            MethodInvoker updateFiles = delegate {
                lock (localScripts) {
                    groupBox4.Text = "Local files (" + ((localDir.Length > 30) ? "..." + localDir.Substring(localDir.Length - 30) : localDir) + ")";
                    string temp = currentFile;
                    dataGridViewLocal.Rows.Clear();
                    foreach (Script file in localScripts) {
                        DataGridViewRow row = new DataGridViewRow();
                        row.Cells.AddRange(new DataGridViewTextBoxCell(), new DataGridViewTextBoxCell(), new DataGridViewTextBoxCell());
                        row.Cells[0].Value = file.name;
                        row.Cells[1].Value = file.size;
                        row.Cells[2].Value = file.date.ToString("dd.MM.yy HH:mm");
                        dataGridViewLocal.Rows.Add(row);
                        string ttText = Script.getScriptDescription(file.type);
                        foreach (DataGridViewCell cell in row.Cells) cell.ToolTipText = ttText;
                    }
                    foreach (DataGridViewRow row in dataGridViewLocal.Rows) {
                        if (row.Cells[0].Value.ToString() == temp) {
                            dataGridViewLocal.ClearSelection();
                            row.Selected = true;
                            break;
                        }
                    }
                }
            };
            if (InvokeRequired) BeginInvoke(updateFiles); else updateFiles.Invoke();
        }
        public void DeleteLocalFile(List<string> files) {
            foreach(string file in files) RecyclingBin.MoveHere(Path.Combine(localDir, file));
            FillLocalFiles();
            UpdateLocalFiles();
        }
        #endregion


        #region Download
        private string[] DownloadList(string url) {
            try {
                MemoryStream ms = new MemoryStream();
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.AllowAutoRedirect = false;
                request.KeepAlive = true;
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                    if (response.StatusCode == HttpStatusCode.OK) {
                        using (Stream stream = response.GetResponseStream()) {
                            stream.CopyTo(ms);
                        }
                    }
                }
                XmlDocument pdoc = new XmlDocument();
                string ppage = "";

                ppage = XmlProcessor.PreProcess(Encoding.UTF8.GetString(ms.ToArray()));
                pdoc.LoadXml(ppage);
                XmlNodeList nodes = pdoc.GetElementsByTagName("li");
                List<string> retList = new List<string>();
                foreach(XmlNode node in nodes) {
                    if (!node.InnerText.Trim().EndsWith("/") && !node.InnerText.Trim().Equals("Name") &&
                        !node.InnerText.Trim().Equals("Last modified") && !node.InnerText.Trim().Equals("Size") &&
                        !node.InnerText.Trim().Equals("Description") && !node.InnerText.Trim().Equals("Parent Directory")) {
                        retList.Add(node.InnerText.Trim());
                    }
                }
                XmlNodeList nodes2 = pdoc.GetElementsByTagName("a");                
                foreach (XmlNode node in nodes2) {
                    if (!node.InnerText.Trim().EndsWith("/") && !node.InnerText.Trim().Equals("Name") && 
                        !node.InnerText.Trim().Equals("Last modified") && !node.InnerText.Trim().Equals("Size") &&
                        !node.InnerText.Trim().Equals("Description") && !node.InnerText.Trim().Equals("Parent Directory")) {
                        retList.Add(node.InnerText.Trim());
                    }
                }
                if (retList.Count > 0) return retList.ToArray();
            } catch (Exception ex) { ShowStatus("Error: " + ex.Message); }
            return null;
        }

        private byte[] DownloadFile(string url) {
            try {
                MemoryStream ms = new MemoryStream();
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";
                request.AllowAutoRedirect = false;
                request.KeepAlive = true;
                //request.Headers.Add(HttpRequestHeader.AcceptEncoding, "deflate");
                using (HttpWebResponse response = (HttpWebResponse)request.GetResponse()) {
                    if (response.StatusCode == HttpStatusCode.OK) {
                        if (response.ContentLength < 1024 * 1024) {
                            using (Stream stream = response.GetResponseStream()) {
                                stream.CopyTo(ms);
                            }
                        } else {
                            return null;
                        }
                    }
                }
                byte[] content = ms.ToArray();
                if (content.Length > 2 && content[0] == 0x78 && content[1] == 0x9C) { // content compressed with ZLib
                    ms = new MemoryStream();
                    using (InflaterInputStream ds = new InflaterInputStream(new MemoryStream(content))) {
                        ds.CopyTo(ms);
                    }
                }
                return ms.ToArray();
            } catch { }
            return null;
        }

        private bool Download() {
            bool result = false;
            Cursor.Current = Cursors.WaitCursor;
            string[] scriptFolders = new string[] { settings.ScriptCommon, settings.ScriptTelit };
            foreach (string scriptFolder in scriptFolders) {
                if (!"".Equals(scriptFolder)) {
                    string[] files = DownloadList(scriptFolder);
                    if (files != null) {
                        string version = "";
                        bool hasVersion = false;
                        foreach (string file in files) {
                            if (file.ToLower().Equals("version")) {
                                hasVersion = true;
                                byte[] content = DownloadFile(scriptFolder + file);
                                version = Encoding.UTF8.GetString(content);
                            }
                        }
                        foreach (string file in files) {
                            if (hasVersion == false || (!file.ToLower().Equals("version") && !file.StartsWith(version))) {
                                try {
                                    byte[] content = DownloadFile(scriptFolder + file);
                                    if (content != null) {
                                        string localFileName = Path.Combine(localDir, file);
                                        if (File.Exists(localFileName)) try { File.Delete(localFileName); } catch { }
                                        File.WriteAllBytes(localFileName, content);
                                        toolStripStatusLabel.Text = "Load file success: " + file;
                                        result = true;
                                    }
                                } catch { }
                            }
                        }
                    } else result = true;
                }
            }
            Cursor.Current = Cursors.Default;
            return result;
        }
        #endregion

        #region Sync
        private List<Script> ComposeScriptsFromDestination(List<Script> source, List<Script> destination) {
            List<Script> scripts = new List<Script>();
            foreach (Script dstScript in destination) {
                foreach (Script srcScript in source) {
                    if (Path.GetFileNameWithoutExtension(srcScript.name) == Path.GetFileNameWithoutExtension(dstScript.name)) {
                        if (dstScript.date < srcScript.date) scripts.Add(srcScript); // Check datetime
                    }
                }
            }
            return scripts;
        }
        private List<Script> ComposeScriptsFromSource(List<Script> source, List<Script> destination) {
            List<Script> scripts = new List<Script>();
            foreach (Script srcScript in source) {
                bool found = false;
                foreach (Script dstScript in destination) {
                    if (srcScript.name == dstScript.name) {
                        found = true;
                        if (dstScript.date != DateTime.MinValue) {
                            if (dstScript.date < srcScript.date) scripts.Add(srcScript); // Check datetime
                        } else {
                            if (dstScript.size != srcScript.size || srcScript.force) scripts.Add(srcScript); // Check datetime
                        }
                    }
                }
                if (!found) scripts.Add(srcScript);
            }
            return scripts;
        }

        private void toolStripButtonSync_Click(object sender, EventArgs e) {
            if(device != null) device.debugDelegate = null;
            if (localScripts.Count == 0 && dbSourceScripts.Count == 0 && dbCompiledScripts.Count == 0) {
                if (!"".Equals(settings.ScriptCommon) || !"".Equals(settings.ScriptTelit)) {
                    string folder = !"".Equals(settings.ScriptCommon) ? settings.ScriptCommon : settings.ScriptTelit;
                    if (MessageBox.Show(this, "Download scripts from " + folder, "", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1) == DialogResult.Yes) {
                        if (Download() == false) {
                            toolStripStatusLabel.Text = "Load files failed.";
                            return;
                        }
                    }
                }
            }

            if (dbSourceScripts.Count == 0 && dbCompiledScripts.Count == 0) {
                ShowStatus("No DB files to synchronize");
                return;
            }
            device.SetStartMode(settings.Mode, settings.Runtime);
            device.SetDeviceType(DeviceType);
            List<Script> copyScripts, copyScripts3to2up, copyScripts3to2down;
            //List<Script> delScripts;
            List<string> storedCurrentFiles = new List<string>();
            storedCurrentFiles.AddRange(currentFiles);

            foreach (Script script in dbSourceScripts) script.force = false;
            foreach (Script script in dbCompiledScripts) script.force = false;

            syncMode = true;
            // Synchronize DB source scripts with local
            copyScripts3to2up = ComposeScriptsFromDestination(localScripts, dbSourceScripts);
            if (copyScripts3to2up.Count > 0) {
                storeToCurrent(copyScripts3to2up);
                b3to2up_Click(null, null);
            }

            // Synchronize DB Compiled scripts with local
            copyScripts3to2down = ComposeScriptsFromDestination(localScripts, dbCompiledScripts);
            if (copyScripts3to2down.Count > 0) {
                storeToCurrent(copyScripts3to2down);
                b3to2down_Click(null, null);
            }

            // Synchronize DB source scripts with device
            copyScripts = ComposeScriptsFromSource(dbSourceScripts, deviceScripts);
            if (copyScripts.Count > 0) {
                storeToCurrent(copyScripts);
                b2to1up_Click(null, null);
            }

            // Synchronize DB Compiled scripts with device
            copyScripts = ComposeScriptsFromSource(dbCompiledScripts, deviceScripts);
            if (copyScripts.Count > 0) {
                storeToCurrent(copyScripts);
                b2to1down_Click(null, null);
            }
            syncMode = false;

            //
            // Remove unnecessary files on the target device
            // delScripts list
            //

            UpdateDBSourceFiles();
            UpdateDBCompiledFiles();
            //Thread readFilesThread = new Thread(new ThreadStart(device.ReadFiles));
            //readFilesThread.Start();

            currentFiles.Clear();
            currentFiles.AddRange(storedCurrentFiles);
            forceOverwrite = false;

            device.ReadFiles();
            Script activeScript = GetActiveScript(dataGridViewDevice);
            if (activeScript != null) {
                foreach (DataGridViewRow row in dataGridViewDevice.Rows) {
                    if (row.Cells[0].Value.ToString() == activeScript.name) {
                        row.Selected = true;
                    }
                }
            }
        }
        private void toolStripButtonSettings_Click(object sender, EventArgs e) {
            settings.ShowDialog(this);
            ShowStatus("Ready");
            device.Secured = settings.Secured;
            if (debugStarted) debugDevice.OpenPort();
        }
        private void storeToCurrent(List<Script> sourceList) {
            currentFiles.Clear();
            foreach (Script scr in sourceList) currentFiles.Add(scr.name);
        }
        #endregion

        #region Change Local Folder
        private void localFolder_Click(object sender, EventArgs e) {
            if (e is MouseEventArgs) {
                MouseEventArgs em = e as MouseEventArgs;
                if (em.Button == MouseButtons.Left) {
                    runFolderDialog();
                } else if (em.Button == MouseButtons.Right) {
                    contextMenuStripLocalPath.Show(Cursor.Position);
                }
            } else runFolderDialog();
        }
        private void runFolderDialog() {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.SelectedPath = (localDir == null || localDir == "") ? Environment.CurrentDirectory : localDir;
            if (dlg.ShowDialog() == DialogResult.OK) {
                localDir = dlg.SelectedPath;
                fileSystemWatcher.Path = localDir;
                FillLocalFiles();
                UpdateLocalFiles();
            }
        }
        private void toolStripMenuItemCopy_Click(object sender, EventArgs e) {
            Clipboard.SetText(localDir);
        }
        #endregion

        #region Delete file handler
        private void toolStripButtonDel_Click(object sender, EventArgs e) {
            if (currentDGV != null && currentFile != null) { 
                if (currentDGV == dataGridViewDevice) DeleteDeviceFile(currentFiles);
                if (currentDGV == dataGridViewDBSource) DeleteDBSourceFile(currentFiles);
                if (currentDGV == dataGridViewDBCompiled) DeleteDBCompiledFile(currentFiles);
                if (currentDGV == dataGridViewLocal) DeleteLocalFile(currentFiles);
            }
        }
        private void dataGridViewDevice_KeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Delete) DeleteDeviceFile(currentFiles); }
        private void dataGridViewDBSource_KeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Delete) DeleteDBSourceFile(currentFiles); }
        private void dataGridViewDBCompiled_KeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Delete) DeleteDBCompiledFile(currentFiles); }
        private void dataGridViewLocal_KeyDown(object sender, KeyEventArgs e) { if (e.KeyCode == Keys.Delete) DeleteLocalFile(currentFiles); }
        #endregion

        #region DataGrid Cell Click 
        private void dataGridView_CellClickCommon(object sender, List<Script> scripts) {
            DataGridView dgv = sender as DataGridView;
            currentDGV = dgv;
            inCodeTextBox = false;
            toolStripButtonLog.Checked = false;
            UpdateInterface();
            if (dgv != null && dgv.Rows != null) {
                DataGridViewCell e = (sender as DataGridView).CurrentCell;
                if (e != null && e.RowIndex >= 0 && e.RowIndex < dgv.Rows.Count && e.RowIndex >=0) {
                    if (dgv.Rows[e.RowIndex] != null && dgv.Rows[e.RowIndex].Cells[0] != null && dgv.Rows[e.RowIndex].Cells[0].Value != null) {
                        currentFile = dgv.Rows[e.RowIndex].Cells[0].Value.ToString();
                        currentFiles.Clear();
                        bool newCurrent = false;
                        foreach (DataGridViewRow row in dgv.Rows) {
                            if (row.Selected) {
                                currentFiles.Add(row.Cells[0].Value.ToString());
                                if (newCurrent == false) {
                                    newCurrent = true;
                                    currentFile = row.Cells[0].Value.ToString();
                                }
                            }
                        }
                        if(scripts != null) ShowContent(scripts);
                    }
                }
            }
        }        

        private void dataGridViewDevice_CellClick(object sender, DataGridViewCellEventArgs e) { dataGridView_CellClickCommon(sender, null); }
        private void dataGridViewDBSource_CellClick(object sender, DataGridViewCellEventArgs e) { dataGridView_CellClickCommon(sender, dbSourceScripts); }
        private void dataGridViewDBCompiled_CellClick(object sender, DataGridViewCellEventArgs e) { dataGridView_CellClickCommon(sender, dbCompiledScripts); }
        private void dataGridViewLocal_CellClick(object sender, DataGridViewCellEventArgs e) { dataGridView_CellClickCommon(sender, localScripts); }

        private void dataGridViewDevice_SelectionChanged(object sender, EventArgs e) {
            if (!inSelectionChange) {
                inSelectionChange = true;
                dataGridView_CellClickCommon(sender, null);
                bool showed = false;
                foreach (Script script in deviceScripts) {
                    if (script.name == currentFile) {
                        if (script.type != ScriptType.Binary && script.type != ScriptType.PythonCompiled && script.type != ScriptType.PythonOptimized) {                            
                            device.ReadFile(currentFile);
                            showed = true;
                        }
                        break;
                    }
                }
                if(!showed) dataGridView_CellClickCommon(sender, deviceScripts);
                inSelectionChange = false;
            }
        }
        private void dataGridViewDBSource_SelectionChanged(object sender, EventArgs e) {
            if (!inSelectionChange) {
                inSelectionChange = true;
                dataGridView_CellClickCommon(sender, dbSourceScripts);
                inSelectionChange = false;
            }
        }
        private void dataGridViewDBCompiled_SelectionChanged(object sender, EventArgs e) {
            if (!inSelectionChange) {
                inSelectionChange = true;
                dataGridView_CellClickCommon(sender, dbCompiledScripts);
                inSelectionChange = false;
            }
        }
        private void dataGridViewLocal_SelectionChanged(object sender, EventArgs e) {
            if (!inSelectionChange) {
                inSelectionChange = true;
                dataGridView_CellClickCommon(sender, localScripts);
                inSelectionChange = false;
            }
        }
        #endregion

        #region codeRichTextBox_TextChanged
        private void CodeRichTextBox_GotFocus(object sender, EventArgs e) {
            if (!inCodeTextBox) {
                if (currentDGV == dataGridViewDevice) dataGridViewDevice.Focus();
                if (currentDGV == dataGridViewDBSource) dataGridViewDBSource.Focus();
                if (currentDGV == dataGridViewDBCompiled) dataGridViewDBCompiled.Focus();
                if (currentDGV == dataGridViewLocal) dataGridViewLocal.Focus();
            }
        }
        private void codeRichTextBox_MouseDown(object sender, MouseEventArgs e) {
            inCodeTextBox = true;
            b1to2up.Enabled = false;
            b2to1up.Enabled = false;
            b2to3up.Enabled = false;
            b2to1down.Enabled = false;
            b3to2up.Enabled = false;
            b3to2down.Enabled = false;
            toolStripButtonSetActive.Enabled = false;
            toolStripButtonDel.Enabled = false;
            codeRichTextBox.Focus();            
        }

        private void codeRichTextBox_TextChanged(object sender, EventArgs e) {
            // getting keywords/functions
            string keywords = @"\b(import|class|def|del|MOD|SER|SER2|RS232|RS485|ASC0|ASC1|UART1|UART2)\b";
            MatchCollection keywordMatches = Regex.Matches(codeRichTextBox.Text, keywords);

            // getting types/classes from the text 
            string types = @"\b(Console)\b";
            MatchCollection typeMatches = Regex.Matches(codeRichTextBox.Text, types);

            // getting comments (inline or multiline)
            string comments = @"(\/\/.+?$|\/\*.+?\*\/)";
            MatchCollection commentMatches = Regex.Matches(codeRichTextBox.Text, comments, RegexOptions.Multiline);
            
            // getting comments2 (# for python)
            string comments2 = "\\ #.*(\r\n|\r|\n)";
            MatchCollection comment2Matches = Regex.Matches(codeRichTextBox.Text, comments2, RegexOptions.Multiline);

            // getting strings
            string strings = "\".+?\"";
            MatchCollection stringMatches = Regex.Matches(codeRichTextBox.Text, strings);

            // getting strings
            string strings2 = "'.+?'";
            MatchCollection stringMatches2 = Regex.Matches(codeRichTextBox.Text, strings2);

            // getting results
            string resultSuccess = "\\ (S|s)uccess(\r\n|\r|\n)";
            MatchCollection resultSuccessMatches = Regex.Matches(codeRichTextBox.Text, resultSuccess);
            string resultSuccess2 = "\\ (O|o)k(\r\n|\r|\n)";
            MatchCollection resultSuccess2Matches = Regex.Matches(codeRichTextBox.Text, resultSuccess2);
            string resultFail = "\\ (F|f)ailed(\r\n|\r|\n)";
            MatchCollection resultFailMatches = Regex.Matches(codeRichTextBox.Text, resultFail);
            string resultFail2 = "\\ (N|n)ok(\r\n|\r|\n)";
            MatchCollection resultFail2Matches = Regex.Matches(codeRichTextBox.Text, resultFail2);

            // saving the original caret position + forecolor
            int originalIndex = codeRichTextBox.SelectionStart;
            int originalLength = codeRichTextBox.SelectionLength;
            Color originalColor = Color.Black;

            // MANDATORY - focuses a label before highlighting (avoids blinking)
            //titleLabel.Focus();

            // removes any previous highlighting (so modified words won't remain highlighted)
            codeRichTextBox.SelectionStart = 0;
            codeRichTextBox.SelectionLength = codeRichTextBox.Text.Length;
            codeRichTextBox.SelectionColor = originalColor;

            // scanning...
            foreach (Match m in keywordMatches) {
                codeRichTextBox.SelectionStart = m.Index;
                codeRichTextBox.SelectionLength = m.Length;
                codeRichTextBox.SelectionColor = Color.Blue;
            }

            foreach (Match m in typeMatches) {
                codeRichTextBox.SelectionStart = m.Index;
                codeRichTextBox.SelectionLength = m.Length;
                codeRichTextBox.SelectionColor = Color.DarkCyan;
            }

            foreach (Match m in stringMatches) {
                codeRichTextBox.SelectionStart = m.Index;
                codeRichTextBox.SelectionLength = m.Length;
                codeRichTextBox.SelectionColor = Color.Brown;
            }
            foreach (Match m in stringMatches2) {
                codeRichTextBox.SelectionStart = m.Index;
                codeRichTextBox.SelectionLength = m.Length;
                codeRichTextBox.SelectionColor = Color.Brown;
            }

            foreach (Match m in commentMatches) {
                codeRichTextBox.SelectionStart = m.Index;
                codeRichTextBox.SelectionLength = m.Length;
                codeRichTextBox.SelectionColor = Color.Gray;
            }
            foreach (Match m in comment2Matches) {
                codeRichTextBox.SelectionStart = m.Index;
                codeRichTextBox.SelectionLength = m.Length;
                codeRichTextBox.SelectionColor = Color.Gray;
            }


            if (!"".Equals(richTextBoxError)) {
                for (int i = 0; i < codeRichTextBox.Lines.Length; i++) {
                    if (codeRichTextBox.Lines[i].Contains(richTextBoxError)) {
                        string selectString = richTextBoxError;
                        int offset = 0;
                        if (selectString.StartsWith(" ")) offset = richTextBoxError.Length - selectString.TrimStart().Length;
                        codeRichTextBox.SelectionStart = codeRichTextBox.Text.IndexOf(codeRichTextBox.Lines[i]) + offset;
                        codeRichTextBox.SelectionLength = richTextBoxError.Length - offset;
                        codeRichTextBox.SelectionColor = Color.Red;
                        codeRichTextBox.SelectionBackColor = Color.Yellow;
                    }
                }
            }

            foreach (Match m in resultSuccessMatches) { codeRichTextBox.SelectionStart = m.Index; codeRichTextBox.SelectionLength = m.Length; codeRichTextBox.SelectionColor = Color.Green; }
            foreach (Match m in resultSuccess2Matches) { codeRichTextBox.SelectionStart = m.Index; codeRichTextBox.SelectionLength = m.Length; codeRichTextBox.SelectionColor = Color.Green; }
            foreach (Match m in resultFailMatches) { codeRichTextBox.SelectionStart = m.Index; codeRichTextBox.SelectionLength = m.Length; codeRichTextBox.SelectionColor = Color.Red; }
            foreach (Match m in resultFail2Matches) { codeRichTextBox.SelectionStart = m.Index; codeRichTextBox.SelectionLength = m.Length; codeRichTextBox.SelectionColor = Color.Red; }

            // restoring the original colors, for further writing
            codeRichTextBox.SelectionStart = originalIndex;
            codeRichTextBox.SelectionLength = originalLength;
            codeRichTextBox.SelectionColor = originalColor;

            // giving back the focus
            codeRichTextBox.Focus();
        }
        #endregion

        #region CopyFiles        
        private void CopyFiles(List<Script>source, List<Script>destination, ScriptStoreType sourceType, ScriptStoreType destinationType, bool force = false) {
            Dictionary<string, DialogResult> actions = new Dictionary<string, DialogResult>();
            bool toBreak = false;
            List<Script> delScripts = new List<Script>();

            foreach (Script src in source) {
                DialogResult res = DialogResult.Yes;
                foreach (Script dst in destination) {
                    if (dst.name == src.name) {
                        if (!forceOverwrite) {
                            string destName = "";
                            switch(destinationType) {
                                case ScriptStoreType.DBCompiled: destName = "DB compiled"; break;
                                case ScriptStoreType.DBSource: destName = "DB sources"; break;
                                case ScriptStoreType.Local: destName = "Local files"; break;
                                case ScriptStoreType.Device: destName = "Device files"; break;
                            }
                            CopyMessageBox dlg = new CopyMessageBox("File \"" + dst.name + "\" at \"" + destName + "\" already exists. Overwrite?"); 
                            res = dlg.ShowDialog();
                            if (res == DialogResult.Cancel) {
                                toBreak = true;
                                break;
                            }
                            if (res == DialogResult.Ignore) { forceOverwrite = true; res = DialogResult.Yes; }
                        }
                        if (res == DialogResult.Yes) delScripts.Add(dst);
                    }
                }
                if (toBreak) break;
                ShowStatus("Copy file " + src.name + " ...");
                foreach (Script del in delScripts) destination.Remove(del);

                Cursor.Current = Cursors.WaitCursor;
                try {
                    if (res == DialogResult.Yes) {
                        Script script = new Script(destinationType, src.name, src.size, src.date);
                        byte[] content = null;
                        script.type = src.type;
                        script.force = force;
                        switch (sourceType) {
                            case ScriptStoreType.Device: content = device.ReadFileSync(src.name); break;
                            case ScriptStoreType.DBSource: content = src.content; break;
                            case ScriptStoreType.DBCompiled: content = src.content; break;
                            case ScriptStoreType.Local: content = File.ReadAllBytes(Path.Combine(localDir, script.name)); break;
                        }
                        script.content = new byte[content.Length];
                        Array.Copy(content, script.content, content.Length);

                        if (script.date == DateTime.MinValue)
                            script.date = DateTime.Now;
                        if (script.content != null && script.content.Length == script.size) {
                            bool inserted = false;
                            for (int i = 0; i < destination.Count; i++) {
                                if (string.Compare(script.name, destination[i].name) < 0) {
                                    destination.Insert(i, script);
                                    inserted = true;
                                    break;
                                }
                            }
                            if (!inserted) destination.Add(script);

                            Script activeScript = null;
                            bool destActive = src.active;
                            Cursor.Current = Cursors.WaitCursor;
                            switch (destinationType) {
                                case ScriptStoreType.Device:
                                    if (device.WriteFileSync(script.name, script.content) == false) destination.Remove(script);
                                    if (destActive) {
                                        activeScript = GetActiveScript(dataGridViewDevice);
                                        if (activeScript != null) activeScript.active = false;
                                        device.SetActive(script.name);                                        
                                        script.active = destActive;
                                    }
                                    break;
                                case ScriptStoreType.DBSource:
                                    if (destActive) {
                                        activeScript = GetActiveScript(dataGridViewDBSource);
                                        if (activeScript != null) activeScript.active = false;
                                        script.active = destActive;
                                    }
                                    break;
                                case ScriptStoreType.DBCompiled:
                                    if (destActive) {
                                        activeScript = GetActiveScript(dataGridViewDBCompiled);
                                        if (activeScript != null) activeScript.active = false;
                                        script.active = destActive;
                                    }
                                    break;
                                case ScriptStoreType.Local:
                                    string path = Path.Combine(localDir, script.name);
                                    File.WriteAllBytes(path, script.content);
                                    File.SetCreationTime(path, script.date);
                                    File.SetLastAccessTime(path, script.date);
                                    File.SetLastWriteTime(path, script.date);
                                    script.active = false;
                                    break;
                            }
                        }
                    }
                } catch { }
                ShowStatus("Ready");
                Cursor.Current = Cursors.Default;
            }
        }
        #endregion

        #region Moving Scripts in Panels
        private void b1to2up_Click(object sender, EventArgs e) {
            List<Script> devScripts = new List<Script>();
            foreach (Script scr in deviceScripts) {
                if (currentFiles.Contains(scr.name)) devScripts.Add(scr);
            }
            CopyFiles(devScripts, dbSourceScripts, ScriptStoreType.Device, ScriptStoreType.DBSource);
            UpdateDBSourceFiles();
            if (!WriteScriptDB()) ShowStatus("Can not write Script database");
            if (!syncMode) forceOverwrite = false;
        }        

        private void b2to3up_Click(object sender, EventArgs e) {
            List<Script> dbScripts = new List<Script>();
            foreach (Script scr in dbSourceScripts) {
                if (currentFiles.Contains(scr.name)) dbScripts.Add(scr);
            }
            CopyFiles(dbScripts, localScripts, ScriptStoreType.DBSource, ScriptStoreType.Local);
            UpdateLocalFiles();
            if (!syncMode) forceOverwrite = false;
        }        

        private void b3to2down_Click(object sender, EventArgs e) {
            List<Script> locScripts = new List<Script>();
            List<Script> compScripts = new List<Script>();
            foreach (Script scr in localScripts) {
                if (currentFiles.Contains(scr.name)) locScripts.Add(scr);
            }
            List<Script> locCompiledScripts = new List<Script>();
            string tempDirectory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            string pythonName = Path.Combine(tempDirectory, "python.exe");
            string compileName = Path.Combine(tempDirectory, "__!compile!__.py");
            bool updated = false;
            string tempCurrent = null;
            try {                
                Directory.CreateDirectory(tempDirectory);
                File.WriteAllBytes(pythonName, Properties.Resources.python); // Write python to temp dir 
                File.WriteAllBytes(compileName, Properties.Resources.compile); // Write compile script to temp dir 
                

                foreach (Script script in locScripts) {
                    bool retry = true;
                    while (retry) {

                        string scriptName = Path.Combine(localDir, script.name);
                        if (File.Exists(scriptName)) {
                            script.content = File.ReadAllBytes(scriptName);
                        }
                        if (script.type == ScriptType.Python && script.content != null) {
                            string fileName = Path.Combine(tempDirectory, script.name);
                            File.WriteAllBytes(fileName, script.content);
                            string args = "-O __!compile!__.py \"" + script.name + "\"";
                            ProcessStartInfo psi = new ProcessStartInfo(pythonName, args);
                            psi.WorkingDirectory = tempDirectory;
                            psi.RedirectStandardOutput = true;
                            psi.RedirectStandardError = true;
                            psi.CreateNoWindow = true;
                            psi.UseShellExecute = false;
                            Process proc = Process.Start(psi);
                            proc.WaitForExit();
                            string procout = proc.StandardError.ReadToEnd();

                            if ("".Equals(procout)) {
                                retry = false;
                                string compFile = script.name + 'o';
                                string outFile = Path.Combine(tempDirectory, compFile);
                                if (tempCurrent == null) tempCurrent = compFile;
                                if (File.Exists(outFile)) {
                                    Script compScript = new Script(ScriptStoreType.DBCompiled, compFile, 0, DateTime.Now);
                                    compScript.content = File.ReadAllBytes(outFile);
                                    compScript.size = compScript.content.Length;
                                    compScript.type = ScriptType.PythonOptimized;
                                    compScripts.Add(compScript);
                                    updated = true;
                                }
                            } else {
                                string toFind = "Traceback (innermost last):";
                                string errString = (procout.Contains(toFind)) ? procout.Substring(0, procout.IndexOf(toFind)) : procout;
                                string toFindLine = "File \"" + script.name + "\", line ";
                                int lineNum = -1;
                                if (procout.Contains(toFindLine)) {
                                    try {
                                        string s = procout.Substring(procout.IndexOf(toFindLine) + toFindLine.Length);
                                        if (s.Contains("\r")) {
                                            s = s.Substring(0, s.IndexOf("\r"));
                                            lineNum = Convert.ToInt32(s);
                                        }
                                    } catch { }
                                }
                                codeRichTextBox.Text = Encoding.UTF8.GetString(script.content);
                                if (lineNum > 0) richTextBoxError = codeRichTextBox.Lines[lineNum - 1];
                                ShowContent(Encoding.UTF8.GetString(script.content), lineNum, errString);
                                ErrorMessageBox mbox = new ErrorMessageBox(errString, "Error in \"" + script.name + "\"");
                                if (mbox.ShowDialog() == DialogResult.Cancel) retry = false;
                            }
                        } else retry = false;
                    }
                }
                CopyFiles(compScripts, dbCompiledScripts, ScriptStoreType.DBCompiled, ScriptStoreType.DBCompiled, true);
            } catch {
            }            
            try { Directory.Delete(tempDirectory, true); } catch { }
            if (updated) {
                if (tempCurrent != null) currentFile = tempCurrent;
                UpdateDBCompiledFiles();
            }
            if (!syncMode) forceOverwrite = false;
        }

        private void b2to1down_Click(object sender, EventArgs e) {
            List<Script> dbScripts = new List<Script>();
            foreach (Script scr in dbCompiledScripts) {
                if (currentFiles.Contains(scr.name)) dbScripts.Add(scr);
            }
            CopyFiles(dbScripts, deviceScripts, ScriptStoreType.DBCompiled, ScriptStoreType.Device);
            UpdateDeviceFiles();
            Thread readFilesThread = new Thread(new ThreadStart(device.ReadFiles));
            readFilesThread.Start();
            if (!syncMode) forceOverwrite = false;
        }

        private void b2to1up_Click(object sender, EventArgs e) {
            List<Script> dbScripts = new List<Script>();
            foreach (Script src in dbSourceScripts) {
                if (currentFiles.Contains(src.name)) dbScripts.Add(src);
            }
            foreach(Script src in dbScripts) {
                src.content = Encoding.UTF8.GetBytes(Encoding.UTF8.GetString(src.content).Replace("\r\n", "\n"));
                src.size = src.content.Length;
            }
            CopyFiles(dbScripts, deviceScripts, ScriptStoreType.DBSource, ScriptStoreType.Device);
            UpdateDeviceFiles();
            Thread readFilesThread = new Thread(new ThreadStart(device.ReadFiles));
            readFilesThread.Start();
        }

        private void b3to2up_Click(object sender, EventArgs e) {
            List<Script> locScripts = new List<Script>();
            foreach (Script src in localScripts) {
                if (currentFiles.Contains(src.name)) locScripts.Add(src);
            }
            CopyFiles(locScripts, dbSourceScripts, ScriptStoreType.Local, ScriptStoreType.DBSource, true);
            UpdateDBSourceFiles();
            if (!syncMode) forceOverwrite = false;
        }
        #endregion

        #region Set Active Script
        private void setActiveScript(bool set) {
            if (currentFile != null && currentFile != "") {
                Script activeScript = GetActiveScript(currentDGV);
                if (activeScript != null) activeScript.active = false;
                Script script = GetCurrentScript();
                if (script != null && set) script.active = true;
                if (currentDGV == dataGridViewDevice) {
                    if (set) device.SetActive(currentFile);
                    else device.SetInactive();
                }
                Thread readFilesThread = new Thread(new ThreadStart(device.ReadFiles));
                readFilesThread.Start();

                if (currentDGV == dataGridViewDBSource) UpdateDBSourceFiles();
                if (currentDGV == dataGridViewDBCompiled) UpdateDBCompiledFiles();
            }
        }

        private void setAsActiveToolStripMenuItem_Click(object sender, EventArgs e) {
            toolStripButtonSetActive.Checked = true;
            setActiveScript(toolStripButtonSetActive.Checked);
        }

        private void deactivateToolStripMenuItem_Click(object sender, EventArgs e) {
            toolStripButtonSetActive.Checked = false;
            setActiveScript(toolStripButtonSetActive.Checked);
        }

        private void dataGridViewDevice_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e) {
            DataGridView dgv = sender as DataGridView;
            if (dgv != null && dgv.Rows != null & e.RowIndex < dgv.Rows.Count && e.RowIndex >=0) {
                DataGridViewRow row = dgv.Rows[e.RowIndex];
                if (row.Cells != null && e.ColumnIndex < row.Cells.Count) {
                    dgv.CurrentCell = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex];
                }
            }
        }

        private void toolStripButtonSetActive_Click(object sender, EventArgs e) {
            setActiveScript(toolStripButtonSetActive.Checked);
        }
        #endregion

        #region Getters
        private Script GetCurrentScript() {
            List<Script> scripts = new List<Script>();
            if (currentDGV == dataGridViewDevice) scripts = deviceScripts;
            if (currentDGV == dataGridViewDBSource) scripts = dbSourceScripts;
            if (currentDGV == dataGridViewDBCompiled) scripts = dbCompiledScripts;
            if (currentDGV == dataGridViewLocal) scripts = localScripts;
            foreach (Script script in scripts) {
                if (currentFile == script.name) return script;
            }
            return null;
        }
        private Script GetActiveScript(DataGridView dgv) {
            List<Script> scripts = new List<Script>();
            if (dgv == dataGridViewDevice) scripts = deviceScripts;
            if (dgv == dataGridViewDBSource) scripts = dbSourceScripts;
            if (dgv == dataGridViewDBCompiled) scripts = dbCompiledScripts;
            if (dgv == dataGridViewLocal) scripts = localScripts;
            foreach (Script script in scripts) {
                if (script.active) return script;
            }
            return null;
        }
        #endregion

        #region Hotkeys
        private void MainForm_KeyDown(object sender, KeyEventArgs e) {
            if(e.Control) {
                if (e.KeyCode == Keys.S) toolStripButtonSync_Click(null, null);
                if (e.KeyCode == Keys.P) toolStripButtonSettings_Click(null, null);
                if (e.KeyCode == Keys.L) localFolder_Click(null, null);
            }
        }
        #endregion

        #region Run Script
        private void toolStripButtonPlay_Click(object sender, EventArgs e) {
            if(device != null && currentFile != null && !"".Equals(currentFile)) {
                //toolStripButtonPlay.Enabled = false;                
                if (debugDevice != null) {
                    debugDevice.ClearLog();
                    if (debugDevice.getPort() == null) {
                        if (settings.DebugCOMPort != comPort) debugDevice.OpenPort();
                        else debugDevice.OpenMainPort();
                    } else {
                        if (debugDevice.getPort().PortName != comPort) {
                            debugDevice.ClosePort();
                            debugDevice.OpenPort();
                        } else {
                            debugDevice.CloseMainPort();
                            debugDevice.OpenMainPort();
                        }
                    }
                
                    debugDevice.AppendLog("Debug started...\n");
                    toolStripButtonLog.Checked = true;

                    device.SetActive(currentFile);
                    debugStarted = true;
                    device.RunFile();
                }
                //toolStripButtonStop.Enabled = true;
            }
        }
        
        private void toolStripButtonStop_Click(object sender, EventArgs e) {
            if (device != null) {                
                toolStripButtonStop.Enabled = false;
                device.Reboot();
                Thread.Sleep(1000);
                device.SetInactive();
                toolStripButtonPlay.Enabled = true;                
                toolStripButtonLog.Checked = true;
            }
        }

        private void toolStripButtonLog_Click(object sender, EventArgs e) {
            toolStripButtonLog.Checked = !toolStripButtonLog.Checked;
        }

        private void toolStripButtonLog_CheckedChanged(object sender, EventArgs e) {
            if (toolStripButtonLog.Checked) {
                /*if(!debugStarted) {
                    debugDevice.ClearLog();
                    debugDevice.ClosePort();
                    debugDevice.OpenPort();
                    debugDevice.AppendLog("Debug started...\n");
                    device.RunFile();
                    debugStarted = true;
                }*/
                ShowContent(debugDevice.GetLog());
            } else {
                if (currentDGV == dataGridViewDevice) dataGridViewDevice_SelectionChanged(currentDGV, null);
                if (currentDGV == dataGridViewDBSource) dataGridViewDBSource_SelectionChanged(currentDGV, null);
                if (currentDGV == dataGridViewDBCompiled) dataGridViewDBCompiled_SelectionChanged(currentDGV, null);
                if (currentDGV == dataGridViewLocal) dataGridViewLocal_SelectionChanged(currentDGV, null);
                //debugDevice.ClosePort();
                //debugStarted = false;                
            }
        }
        #endregion

        #region Check port
        private void toolStripButtonCheck_Click(object sender, EventArgs e) {
            if (device != null && comPort != null && !"".Equals(comPort) && comSpeed != null) {                
                ShowStatus("Reopening port " + comPort);
                device.ClosePort();
                device.OpenPort();
                if (deviceOpened) {
                    canUsePort = true;
                }
            }
        }

        #endregion
    }    
}
