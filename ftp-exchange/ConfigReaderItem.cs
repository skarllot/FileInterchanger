using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ftp_exchange
{
    class ConfigReaderItem
    {
        SklLib.IO.ConfigFileReader cfgreader;
        string section;

        public ConfigReaderItem(SklLib.IO.ConfigFileReader reader, string section)
        {
            this.cfgreader = reader;
            this.section = section;
        }

        public string BackupFolder { get { return GetString("BackupFolder"); } }
        public string Cleanup { get { return GetString("Cleanup"); } }
        public string Files { get { return GetString("Files"); } }
        public string Fingerprint { get { return GetString("Fingerprint"); } }
        public string FtpSecure { get { return GetString("FtpSecure"); } }
        public string HostName { get { return GetString("HostName"); } }
        public string Local { get { return GetString("Local"); } }
        public bool Move { get { return GetBoolean("Move"); } }
        public string Password { get { return GetString("Password"); } }
        public string Protocol { get { return GetString("Protocol"); } }
        public string Remote { get { return GetString("Remote"); } }
        public string Section { get { return section; } }
        public string SyncTarget { get { return GetString("SyncTarget"); } }
        public string TimeFilter { get { return GetString("TimeFilter"); } }
        public string UserName { get { return GetString("UserName"); } }

        private bool GetBoolean(string key)
        {
            bool result;
            string val;
            if (!cfgreader.TryReadValue(section, key, out val))
                return false;
            if (!bool.TryParse(val, out result))
                return false;
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
