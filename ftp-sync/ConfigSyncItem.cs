using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ftp_sync
{
    class ConfigSyncItem
    {
        SklLib.IO.ConfigFileReader cfgreader;
        string section;

        public ConfigSyncItem(SklLib.IO.ConfigFileReader reader, string section)
        {
            this.cfgreader = reader;
            this.section = section;
        }

        public string[] Files { get { return GetCsvString("Files"); } }
        public bool KeepFiles { get { return GetBoolean("KeepFiles"); } }
        public string Local { get { return GetString("Local"); } }
        public string Remote { get { return GetString("Remote"); } }
        public string StoredSession { get { return GetString("StoredSession"); } }

        private bool GetBoolean(string key)
        {
            bool result;
            string val;
            if (!cfgreader.TryReadValue(section, key, out val))
                result = true;
            if (!bool.TryParse(val, out result))
                result = true;
            return result;
        }

        private string GetString(string key)
        {
            string val;
            cfgreader.TryReadValue(section, key, out val);
            return val;
        }

        private string[] GetCsvString(string key)
        {
            string val;
            cfgreader.TryReadValue(section, key, out val);
            string[] list = new string[0];
            if (!string.IsNullOrWhiteSpace(val))
                list = val.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            return list;
        }
    }
}
