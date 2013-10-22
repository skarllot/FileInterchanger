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

        public string DeleteAfter { get { return GetString("DeleteAfter"); } }
        public string Files { get { return GetString("Files"); } }
        public string Fingerprint { get { return GetString("Fingerprint"); } }
        public string FtpSecure { get { return GetString("FtpSecure"); } }
        public string HostName { get { return GetString("HostName"); } }
        public string Local { get { return GetString("Local"); } }
        public string Password { get { return GetString("Password"); } }
        public string Protocol { get { return GetString("Protocol"); } }
        public string Remote { get { return GetString("Remote"); } }
        public string Section { get { return section; } }
        public string StoredSession { get { return GetString("StoredSession"); } }
        public string SyncTarget { get { return GetString("SyncTarget"); } }
        public string UserName { get { return GetString("UserName"); } }

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
