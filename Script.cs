using System;

namespace TelitLoader {

    public enum ScriptType {
        Binary,
        DeviceType,
        Python,
        PythonCompiled,
        PythonOptimized,
        Text,
        Json,
        Version,
        Xml,
        Ini,
        Bat, 
        Cmd,
        Ps1
    }
    public enum ScriptStoreType {
        Device,
        DBSource,
        DBCompiled,
        Local,
    }

    [Serializable]
    public class Script {
        public const string DeviceTypeFileName = "Device.type";
        public string name;
        public long size;
        public DateTime date;        
        public byte[] content;
        public ScriptStoreType storeType;
        public ScriptType type;
        public bool active;
        public bool force = false;

        public Script(ScriptStoreType type, string name, long size) {
            this.storeType = type;
            this.name = name;
            this.size = size;
            active = false;
            UpdateType();
        }
        public Script(ScriptStoreType type, string name, long size, DateTime date) {
            this.storeType = type;
            this.name = name;
            this.size = size;
            this.date = date;
            active = false;
            UpdateType();
        }
        private void UpdateType() {
            type = ScriptType.Binary;
            if (name.Equals(DeviceTypeFileName)) type = ScriptType.DeviceType;
            if (name.EndsWith("py")) type = ScriptType.Python;
            if (name.EndsWith("pyc")) type = ScriptType.PythonCompiled;
            if (name.EndsWith("pyo")) type = ScriptType.PythonOptimized;
            if (name.EndsWith("txt")) type = ScriptType.Text;
            if (name.EndsWith("json")) type = ScriptType.Json;
            if (name.EndsWith("version")) type = ScriptType.Version;
            if (name.EndsWith("xml")) type = ScriptType.Xml;
            if (name.EndsWith("ini")) type = ScriptType.Ini;
            if (name.EndsWith("bat")) type = ScriptType.Bat;
            if (name.EndsWith("cmd")) type = ScriptType.Cmd;
            if (name.EndsWith("ps1")) type = ScriptType.Ps1;
        }

        public static string getScriptDescription(ScriptType type) {
            string ttText = "Binary file";
            if (type == ScriptType.DeviceType) ttText = "Device type settings file";
            if (type == ScriptType.Python) ttText = "Python script";
            if (type == ScriptType.PythonCompiled) ttText = "Compiled python script";
            if (type == ScriptType.PythonOptimized) ttText = "Compiled and optimized python script";
            if (type == ScriptType.Text) ttText = "Text file";
            if (type == ScriptType.Json) ttText = "JSON file";
            if (type == ScriptType.Version) ttText = "Version file";
            if (type == ScriptType.Xml) ttText = "XML file";
            if (type == ScriptType.Ini) ttText = "Ini file";
            if (type == ScriptType.Bat) ttText = "Command interpretier file";
            if (type == ScriptType.Cmd) ttText = "Powershell command file";
            if (type == ScriptType.Ps1) ttText = "Powershell command file";
            return ttText;
        }
    }    
}
