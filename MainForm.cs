using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.IO.Ports;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using Shell32;
using System.Diagnostics;

namespace TelitLoader {
    public partial class MainForm : Form {

        #region Vars
        const string dbFileName = "Scripts.db";
        string dbPath = "";
        List<Script> deviceScripts = new List<Script>();
        List<Script> dbSourceScripts = new List<Script>();
        List<Script> dbCompiledScripts = new List<Script>();
        List<Script> localScripts = new List<Script>();
        public byte[] key;
        public byte[] iv;
        Device device = null;
        string localDir = "";
        string comPort = "";
        DataGridView currentDGV = null;
        string currentFile = null;
        List<string> currentFiles = new List<string>();        
        bool inSelectionChange = false;
        bool inCodeTextBox = false;
        static Shell shell = new Shell();
        static Folder RecyclingBin = shell.NameSpace(10);
        SettingsForm settings = null;
        FileSystemWatcher fileSystemWatcher = new FileSystemWatcher();
        #endregion

        #region Constructor
        public MainForm() {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e) {
            dbPath = Path.Combine(Environment.CurrentDirectory, dbFileName);
            device = new Device(this);
            Initialize();
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

            settings = new SettingsForm();
            if (!File.Exists(dbPath))
                if (!CreateScriptDB()) ShowStatus("Can not create new Script database"); ;
            if (!ReadScriptDB()) ShowStatus("Can not load Script database");            

            toolStripComboBoxPort.Items.AddRange(SerialPort.GetPortNames());
            if (toolStripComboBoxPort.Items.Contains(comPort)) toolStripComboBoxPort.SelectedItem = comPort;
            else if (toolStripComboBoxPort.Items.Count > 0) toolStripComboBoxPort.SelectedItem = toolStripComboBoxPort.Items[0];

            groupBox1.Click += GroupBox1_Click;
            groupBox4.Click += toolStripButtonAdd_Click;
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
                    comPort = (string)bf.Deserialize(cryptoStream);
                    tempLocalDir = (string)bf.Deserialize(cryptoStream);
                    device.Secured = settings.Secured = (bool)bf.Deserialize(cryptoStream);
                    settings.Remove = (bool)bf.Deserialize(cryptoStream);
                    settings.Auto = (bool)bf.Deserialize(cryptoStream);
                    settings.AutoSec = (int)bf.Deserialize(cryptoStream);
                    settings.Mode = (int)bf.Deserialize(cryptoStream);
                    settings.Runtime = (int)bf.Deserialize(cryptoStream);
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
            else localDir = Environment.CurrentDirectory;            

            FillLocalFiles();
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
            try {                
                if (File.Exists(dbPath)) fs = new FileStream(dbPath, FileMode.Truncate);
                else fs = new FileStream(dbPath, FileMode.CreateNew);
                AesManaged aes = new AesManaged();
                CryptoStream cryptoStream = new CryptoStream(fs, aes.CreateEncryptor(key, iv), CryptoStreamMode.Write);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(cryptoStream, comPort);
                bf.Serialize(cryptoStream, localDir);
                bf.Serialize(cryptoStream, settings.Secured);
                bf.Serialize(cryptoStream, settings.Remove);
                bf.Serialize(cryptoStream, settings.Auto);
                bf.Serialize(cryptoStream, settings.AutoSec);
                bf.Serialize(cryptoStream, settings.Mode);
                bf.Serialize(cryptoStream, settings.Runtime);
                bf.Serialize(cryptoStream, dbSourceScripts);
                bf.Serialize(cryptoStream, dbCompiledScripts);
                cryptoStream.Close();
                res = true;
            } catch {}

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
            if (device != null) {
                device.ClosePort();
                device.OpenPort();
                comPort = toolStripComboBoxPort.SelectedItem.ToString();
            }
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
                                        if (content.Length > 10 * 1024) content = content.Substring(0, 10 * 1024) + "\r...";
                                    }
                                } catch { }
                            } else content = Encoding.UTF8.GetString(script.content);
                            ShowContent(content);
                        }
                    }
                }
            } catch { }
        }
        public void ShowContent(string content) {
            MethodInvoker method = delegate { codeRichTextBox.Text = content; };
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
                b1to2up.Enabled = true;
                b2to1up.Enabled = false;
                b2to3up.Enabled = false;
                b2to1down.Enabled = false;
                b3to2up.Enabled = false;
                b3to2down.Enabled = false;
                toolStripButtonSetActive.Enabled = true;
                toolStripButtonDel.Enabled = true;
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
                groupBox1.Text = "Device files (" + (device.GetFreeBytes() / 1024) + " kB free)";
                string temp = currentFile;
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
                    }
                }
                foreach (DataGridViewRow row in dataGridViewDevice.Rows) {
                    if (row.Cells[0].Value.ToString() == temp) {
                        dataGridViewDevice.ClearSelection();
                        row.Selected = true;
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
            Cursor = Cursors.WaitCursor;
            foreach (Script script in delScripts) device.DeleteFile(script.name);
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

        #region Sync
        private List<Script> ComposeScriptsFromDestination(List<Script> source, List<Script> destination) {
            List<Script> scripts = new List<Script>();
            foreach (Script dstScript in destination) {
                foreach (Script srcScript in source) {
                    if (srcScript.name == dstScript.name) {                        
                        if (dstScript.date < srcScript.date) scripts.Add(srcScript); // Check datetime
                    }
                }
            }
            return scripts;
        }
        private List<Script> ComposeScriptsFromSource(List<Script> source, List<Script> destination) {
            List<Script> scripts = new List<Script>();
            foreach (Script srcScript in source) {
                foreach (Script dstScript in destination) {                
                    if (srcScript.name == dstScript.name) {
                        if (dstScript.date < srcScript.date) scripts.Add(srcScript); // Check datetime
                    }
                }
            }
            return scripts;
        }
        private void toolStripButtonSync_Click(object sender, EventArgs e) {
            if(dbSourceScripts.Count == 0 && dbCompiledScripts.Count == 0) {
                ShowStatus("No DB files to synchronize");
                return;
            }
            device.SetStartMode(settings.Mode, settings.Runtime);
            List<Script> copyScripts;
            List<Script> delScripts;
            // Synscronize DB source scripts with local
            copyScripts = ComposeScriptsFromDestination(localScripts, dbSourceScripts);
            CopyFiles(copyScripts, dbSourceScripts, ScriptStoreType.Local, ScriptStoreType.DBSource, true);
            // Synscronize DB Compiled scripts with local
            copyScripts = ComposeScriptsFromDestination(localScripts, dbCompiledScripts);
            CopyFiles(copyScripts, dbCompiledScripts, ScriptStoreType.Local, ScriptStoreType.DBCompiled, true);
            // Synscronize DB source scripts with device
            copyScripts = ComposeScriptsFromSource(dbSourceScripts, deviceScripts);

            // Synscronize DB Compiled scripts with device
            copyScripts = ComposeScriptsFromSource(dbCompiledScripts, deviceScripts);

            // Remove unnecessary files on the target device


            UpdateDBSourceFiles();
            UpdateDBCompiledFiles();
            //Thread readFilesThread = new Thread(new ThreadStart(device.ReadFiles));
            //readFilesThread.Start();
        }
        private void toolStripButtonSettings_Click(object sender, EventArgs e) {
            settings.ShowDialog(this);
            device.Secured = settings.Secured;
        }
        #endregion

        #region Change Local Folder
        private void toolStripButtonAdd_Click(object sender, EventArgs e) {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.SelectedPath = Environment.CurrentDirectory;
            if (dlg.ShowDialog() == DialogResult.OK) {
                localDir = dlg.SelectedPath;
                fileSystemWatcher.Path = localDir;
                FillLocalFiles();
                UpdateLocalFiles();
            }
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
            UpdateInterface();
            if (dgv != null && dgv.Rows != null) {
                DataGridViewCell e = (sender as DataGridView).CurrentCell;
                if (e != null && e.RowIndex >= 0 && e.RowIndex < dgv.Rows.Count) {
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
            string keywords = @"\b(import|class|def)\b";
            MatchCollection keywordMatches = Regex.Matches(codeRichTextBox.Text, keywords);

            // getting types/classes from the text 
            string types = @"\b(Console)\b";
            MatchCollection typeMatches = Regex.Matches(codeRichTextBox.Text, types);

            // getting comments (inline or multiline)
            string comments = @"(\/\/.+?$|\/\*.+?\*\/)";
            MatchCollection commentMatches = Regex.Matches(codeRichTextBox.Text, comments, RegexOptions.Multiline);

            // getting strings
            string strings = "\".+?\"";
            MatchCollection stringMatches = Regex.Matches(codeRichTextBox.Text, strings);

            // getting strings
            string strings2 = "'.+?'";
            MatchCollection stringMatches2 = Regex.Matches(codeRichTextBox.Text, strings2);

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

            foreach (Match m in commentMatches) {
                codeRichTextBox.SelectionStart = m.Index;
                codeRichTextBox.SelectionLength = m.Length;
                codeRichTextBox.SelectionColor = Color.Green;
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

            // restoring the original colors, for further writing
            codeRichTextBox.SelectionStart = originalIndex;
            codeRichTextBox.SelectionLength = originalLength;
            codeRichTextBox.SelectionColor = originalColor;

            // giving back the focus
            codeRichTextBox.Focus();
        }
        #endregion

        #region CopyFiles        
        private void CopyFiles(List<Script>source, List<Script>destination, ScriptStoreType sourceType, ScriptStoreType destinationType, bool ignoreExisting = false) {
            Dictionary<string, DialogResult> actions = new Dictionary<string, DialogResult>();
            bool toBreak = false;
            List<Script> delScripts = new List<Script>();

            foreach (Script src in source) {
                DialogResult res = DialogResult.Yes;
                foreach (Script dst in destination) {
                    if (dst.name == src.name) {
                        if (!ignoreExisting) {
                            res = MessageBox.Show("File \"" + dst.name + "\" already exists. Overwrite?", "", MessageBoxButtons.YesNoCancel);
                            if (res == DialogResult.Cancel) {
                                toBreak = true;
                                break;
                            }
                        }
                        if (res == DialogResult.Yes) delScripts.Add(dst);
                    }
                }
                if (toBreak) break;
                foreach (Script del in delScripts) destination.Remove(del);

                Cursor.Current = Cursors.WaitCursor;
                try {
                    if (res == DialogResult.Yes) {
                        Script script = new Script(destinationType, src.name, src.size, src.date);
                        byte[] content = null;
                        script.type = src.type;
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
        }        

        private void b2to3up_Click(object sender, EventArgs e) {
            List<Script> dbScripts = new List<Script>();
            foreach (Script scr in dbSourceScripts) {
                if (currentFiles.Contains(scr.name)) dbScripts.Add(scr);
            }
            CopyFiles(dbScripts, localScripts, ScriptStoreType.DBSource, ScriptStoreType.Local);
            UpdateLocalFiles();
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
                File.WriteAllBytes(compileName, Properties.Resources.compile); // Write compilke script to temp dir 

                foreach (Script script in locScripts) {                    
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
                        psi.CreateNoWindow = true;
                        psi.UseShellExecute = false;
                        Process proc = Process.Start(psi);
                        proc.WaitForExit();

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
                    }
                }
                CopyFiles(compScripts, dbCompiledScripts, ScriptStoreType.DBCompiled, ScriptStoreType.DBCompiled);
            } catch {
            }            
            try { Directory.Delete(tempDirectory, true); } catch { }
            if (updated) {
                if (tempCurrent != null) currentFile = tempCurrent;
                UpdateDBCompiledFiles();
            }
        }

        private void b2to1down_Click(object sender, EventArgs e) {
            List<Script> dbScripts = new List<Script>();
            foreach (Script scr in dbCompiledScripts) {
                if (currentFiles.Contains(scr.name)) dbScripts.Add(scr);
            }
            CopyFiles(dbScripts, deviceScripts, ScriptStoreType.DBCompiled, ScriptStoreType.Device);
            UpdateDeviceFiles();
        }

        private void b2to1up_Click(object sender, EventArgs e) {
            List<Script> dbScripts = new List<Script>();
            foreach (Script scr in dbSourceScripts) {
                if (currentFiles.Contains(scr.name)) dbScripts.Add(scr);
            }
            CopyFiles(dbScripts, deviceScripts, ScriptStoreType.DBSource, ScriptStoreType.Device);
            UpdateDeviceFiles();
        }

        private void b3to2up_Click(object sender, EventArgs e) {
            List<Script> locScripts = new List<Script>();
            foreach (Script scr in localScripts) {
                if (currentFiles.Contains(scr.name)) locScripts.Add(scr);
            }
            CopyFiles(locScripts, dbSourceScripts, ScriptStoreType.Local, ScriptStoreType.DBSource);
            UpdateDBSourceFiles();
        }

        #endregion

        #region Set Active Script
        private void setAsActiveToolStripMenuItem_Click(object sender, EventArgs e) {
            if (currentFile != null && currentFile != "") {
                Script activeScript = GetActiveScript(currentDGV);
                if (activeScript != null) activeScript.active = false;
                Script script = GetCurrentScript();
                if (script != null) script.active = true;
                if (currentDGV == dataGridViewDevice) {
                    device.SetActive(currentFile);
                    Thread readFilesThread = new Thread(new ThreadStart(device.ReadFiles));
                    readFilesThread.Start();
                }
                if (currentDGV == dataGridViewDBSource) UpdateDBSourceFiles();
                if (currentDGV == dataGridViewDBCompiled) UpdateDBCompiledFiles();
            }
        }

        private void dataGridViewDevice_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e) {
            DataGridView dgv = sender as DataGridView;
            if (dgv != null && dgv.Rows != null & e.RowIndex < dgv.Rows.Count) {
                DataGridViewRow row = dgv.Rows[e.RowIndex];
                if (row.Cells != null && e.ColumnIndex < row.Cells.Count) {
                    dgv.CurrentCell = dgv.Rows[e.RowIndex].Cells[e.ColumnIndex];
                }
            }
        }

        private void toolStripButtonSetActive_Click(object sender, EventArgs e) {
            setAsActiveToolStripMenuItem_Click(null, null);
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
        
    }
}
