using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ftp_exchange.IO
{
    class ConfigReaderItem : ConfigReaderBase
    {
        public ConfigReaderItem(SklLib.IO.ConfigFileReader reader, string section)
        {
            this.cfgreader = reader;
            this.sections = new string[] { section };
        }

        public string BackupFolder { get { return GetString(sections[0], "BackupFolder"); } }
        public string Cleanup { get { return GetString(sections[0], "Cleanup"); } }
        public bool CleanupTarget { get { return GetBoolean(sections[0], "CleanupTarget"); } }
        public string Files { get { return GetString(sections[0], "Files"); } }
        public string Fingerprint { get { return GetString(sections[0], "Fingerprint"); } }
        public string FtpCredential { get { return GetString(sections[0], "FtpCredential"); } }
        public string FtpSecure { get { return GetString(sections[0], "FtpSecure"); } }
        public string HostName { get { return GetString(sections[0], "HostName"); } }
        public string Local { get { return GetString(sections[0], "Local"); } }
        public bool Move { get { return GetBoolean(sections[0], "Move"); } }
        public string NetworkCredential { get { return GetString(sections[0], "NetworkCredential"); } }
        public string Protocol { get { return GetString(sections[0], "Protocol"); } }
        public string Remote { get { return GetString(sections[0], "Remote"); } }
        public string Section { get { return sections[0]; } }
        public string SyncTarget { get { return GetString(sections[0], "SyncTarget"); } }
        public string TimeFilter { get { return GetString(sections[0], "TimeFilter"); } }
    }
}
