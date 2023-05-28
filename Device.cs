using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text;
using System.Threading;

namespace TelitLoader {
    
    #region CommandParams 
    class CommandParams {
        public string com;
        public int timeout;
        public string wret;
        public CommandParams(string _com, int _timeout, string _wret) {
            com = _com;
            timeout = _timeout;
            wret = _wret;
        }
    }
    #endregion

    class Device {
        public string name = "Unknown";
        MainForm mainForm;
        SerialPort serialPort;
        public bool Secured { get; set; }
        const int bufferSize = 65536;
        byte[] data = new byte[bufferSize];
        string command = "";
        bool responded = false;
        string response = "";
        List<byte> bresponse = new List<byte>();
        string waitret = "OK";
        List<Script> files = new List<Script>();
        string activeScript = "";
        bool threadStarted = false;
        int freeBytes = -1;

        public Device(MainForm mainForm) {
            this.mainForm = mainForm;
        }

        public void Init() {
            if(SendCommandSync("E0")) {
                mainForm.ShowStatus("Device ready");
            }
        }

        public int GetFreeBytes() { return freeBytes; }

        public void ReadFiles() {
            files.Clear();
            SendCommandSync("#LSCRIPT", 20);
            SendCommandSync("#ESCRIPT?");
        }
        public void ReadFile(string name) { SendCommand("#RSCRIPT=\"" + name + "\""); }
        public byte[] ReadFileSync(string name) { SendCommandSync("#RSCRIPT=\"" + name + "\""); return bresponse.ToArray();  }
        public bool WriteFileSync(string name, byte [] content) {
            SendCommandSync("#WSCRIPT=\"" + name + "\"," + content.Length + "," + (Secured ? "1" : "0"), 3, ">>>");
            if(response.EndsWith(">>>")) {
                responded = false;
                response = "";
                waitret = "OK";
                bresponse = new List<byte>();
                int len = content.Length;
                int size = Math.Min(200, len);
                int c = 0;
                while (len > 0) {
                    serialPort.Write(content, c, size);
                    len -= size;
                    size = Math.Min(200, len);
                    Thread.Sleep(70);
                }
                Thread.Sleep(500);
                int count = 0;
                while (!responded && count < 10 * 10) { Thread.Sleep(100); count++; } // 10 seconds
                if (responded) {
                    if (response != "") {
                        string[] retar = response.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        if (retar.Length > 0 && retar[0] == waitret) {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        public void DeleteFile(string name) {
            SendCommandSync("#DSCRIPT=\"" + name + "\"");
            Thread.Sleep(2000);
        }

        public void SetActive(string name) {
            SendCommandSync("#ESCRIPT=\"" + name + "\"");
        }

        public void SetStartMode(int mode, int script_start_to) {
            SendCommandSync("#STARTMODESCR=" + mode + (mode == 1 ? "," + script_start_to : ""));
        }

        #region SendCommands
        private bool SendCommandSync(string com, int timeout = 3, string wret = "OK") {
            Thread commandThread = new Thread(new ParameterizedThreadStart(SendCommandParam));
            commandThread.Start(new CommandParams(com, timeout, wret));
            commandThread.Join();
            return true;
        }

        private bool SendCommand(string com, int timeout = 3, string wret = "OK") {
            Thread commandThread = new Thread(new ParameterizedThreadStart(SendCommandParam));
            commandThread.Start(new CommandParams(com, timeout, wret));
            return true;
        }

        private void SendCommandParam(object param) {
            if (!threadStarted) {
                threadStarted = true;
                CommandParams cparams = param as CommandParams;
                if (cparams != null) SendCommandInternal(cparams.com, cparams.timeout, cparams.wret);
                threadStarted = false;
            }
        }

        private bool SendCommandInternal(string com, int timeout = 3, string wret = "OK") {
            bool res = false;
            command = com;
            responded = false;
            response = "";
            bresponse = new List<byte>();
            waitret = wret;
            if (serialPort.IsOpen) {
                serialPort.Write("AT" + com + "\r");
                string baseCom = com;
                string baseParam = "";                
                if (com.Contains("=")) {
                    baseCom = com.Substring(0, com.IndexOf("="));
                    baseParam = com.Substring(com.IndexOf("=") + 1);
                }
                int count = 0;
                while (!responded && count < timeout * 10) { Thread.Sleep(100); count++; }

                if (responded) {
                    if (response != "") {
                        string[] retar = response.Split(new char[] { '\n' });

                        #region preprocess
                        for (int i = 0; i < retar.Length; i++) if (retar[i].EndsWith("\r")) retar[i] = retar[i].Substring(0, retar[i].Length - 1);
                        while (retar.Length > 0 && retar[0] == "") { // ignore first empty string
                            List<string> retlist = (new List<string>(retar));
                            retlist.RemoveAt(0);
                            retar = retlist.ToArray();
                        }
                        while (retar.Length > 0 && retar[0] == "") { // ignore first empty string
                            List<string> retlist = (new List<string>(retar));
                            retlist.RemoveAt(0);
                            retar = retlist.ToArray();
                        }
                        while (retar.Length > 0 && retar[retar.Length - 1] == "") { // ignore last empty string
                            List<string> retlist = (new List<string>(retar));
                            retlist.RemoveAt(retar.Length - 1);
                            retar = retlist.ToArray();
                        }
                        if (retar.Length > 0 && retar[0] == "AT" + com) { // ignore Echo
                            List<string> retlist = (new List<string>(retar));
                            retlist.RemoveAt(0);
                            retar = retlist.ToArray();
                        }
                        #endregion

                        if (retar.Length > 0 && retar[retar.Length - 1] == wret) {
                            if (retar[retar.Length - 1] == "OK") { // ignore last OK
                                List<string> retlist = (new List<string>(retar));
                                retlist.RemoveAt(retar.Length - 1);
                                retar = retlist.ToArray();
                            }
                            ProcessResponse(baseCom, baseParam, retar);
                            responded = true;
                        } else mainForm.ShowStatus("Device reponse with error (AT" + baseCom + ")...");
                    } else mainForm.ShowStatus("Device response missing...");
                } else mainForm.ShowStatus("Device not responded...");
            }
            return res;
        }
        #endregion

        #region ProcessResponse
        private void ProcessResponse(string com, string param, string [] retarr) {
            string status = "Ready";
            switch(com) {
                case "E0": status = "Echo off"; break;
                case "E1": status = "Echo on"; break;
                case "#LSCRIPT":
                    foreach(string s in retarr) {
                        if(!s.StartsWith("#LSCRIPT: free bytes:") && s.Length > 10) {
                            string[] sa = s.Substring(10).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                            if (sa.Length == 2) {
                                long size = 0;
                                if (long.TryParse(sa[1], out size)) files.Add(new Script(ScriptStoreType.Device, sa[0].Replace("\"", ""), size));
                            }
                        } else if (s.Length > 22) {
                            string fb = s.Substring(22);
                            int.TryParse(fb, out freeBytes);
                        }
                    }
                    status = "List scripts";                    
                    Dictionary<string, Script> newFiles = new Dictionary<string, Script>();
                    List<string> names = new List<string>();
                    foreach (Script s in files) {
                        if (!newFiles.ContainsKey(s.name)) {
                            newFiles.Add(s.name, s);
                            names.Add(s.name);
                        }
                    }
                    names.Sort();
                    files.Clear();
                    foreach (string name in names) files.Add(newFiles[name]);
                    mainForm.FillDeviceFiles(files);
                    break;
                case "#ESCRIPT?":
                    if(retarr.Length > 0 && retarr[0].Length > 10) activeScript = retarr[0].Substring(10).Replace("\"","");
                    mainForm.UpdateActiveDeviceScript(activeScript);
                    break;

                case "#RSCRIPT":
                    try {
                        string content = "";
                        if (bresponse.Count > 11) {
                            bresponse.RemoveRange(bresponse.Count - 6, 6);
                            bresponse.RemoveRange(0, 5);
                        }
                        string tparam = param.Replace("\"", "").ToLower();
                        if (param != "" && (tparam.EndsWith("pyo") || tparam.EndsWith("pyc"))) {
                            content = "Binary file";
                        } else {
                            if (retarr.Length > 0 && retarr[0].Length > 3) retarr[0] = retarr[0].Substring(3);
                            foreach (string s in retarr) content += s + "\r";
                        }
                        status = "Read script";
                        mainForm.ShowContent(content);
                    } catch { }
                    break;

                default:
                    mainForm.ShowStatus("Unknown command sent");
                    break;
            }
            mainForm.ShowStatus(status);
        }
        #endregion

        #region OpenPort
        public void OpenPort() {
            string portName = mainForm.toolStripComboBoxPort.SelectedItem.ToString();
            mainForm.ShowStatus("Opening " + portName + "...");            
            serialPort = new SerialPort(portName);
            try {
                serialPort.BaudRate = 115200;
                serialPort.Parity = Parity.None;
                serialPort.StopBits = StopBits.One;
                serialPort.DataBits = 8;
                serialPort.Handshake = Handshake.None;
                serialPort.DataReceived += Port_DataReceived;
                serialPort.Open();
                mainForm.ShowStatus("Port " + portName + " opened");
                Init();
                Thread readFilesThread = new Thread(new ThreadStart(ReadFiles));
                readFilesThread.Start();
            } catch {
                mainForm.ShowStatus("Can not open port");
            }
        }
        #endregion

        #region ClosePort
        public void ClosePort() {
            string portName = mainForm.toolStripComboBoxPort.SelectedItem.ToString();
            mainForm.ShowStatus("Closing " + portName + "...");
            Thread closePortThread = new Thread(new ThreadStart(ClosePortInternal));
            closePortThread.Start();
            closePortThread.Join();
        }

        private void ClosePortInternal() {
            if (serialPort != null && serialPort.IsOpen) {
                serialPort.DataReceived -= Port_DataReceived;
                serialPort.DiscardInBuffer();
                serialPort.DiscardOutBuffer();
                serialPort.Close(); 
                int counter = 0;
                while (serialPort.IsOpen && counter < 30) {
                    Thread.Sleep(100);
                    counter++;
                }
            }
        }
        #endregion

        #region Port_DataReceived
        private void Port_DataReceived(object sender, SerialDataReceivedEventArgs e) {
            if (serialPort != null && serialPort.IsOpen) {
                int b2r = serialPort.BytesToRead;

                if (b2r > 0 && b2r < data.Length) {
                    for (int i = b2r; i < data.Length; i++) data[i] = 0x00;

                    int bytesCount = serialPort.Read(data, 0, b2r);
                    if (bytesCount == b2r) {
                        response = response + Encoding.UTF8.GetString(data, 0, bytesCount);
                        byte[] b = new byte[bytesCount];
                        Array.Copy(data, b, bytesCount);
                        bresponse.AddRange(b);
                        string[] retar = response.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                        if (retar.Length > 0 && (retar[retar.Length - 1] == waitret || retar[retar.Length - 1] == "ERROR")) {
                            responded = true;
                        }
                    }
                } else if (b2r > 0) {
                    serialPort.DiscardInBuffer();
                    serialPort.DiscardOutBuffer();
                }
            }
        }
        #endregion


    }
}
