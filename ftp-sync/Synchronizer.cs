using System;
using WinSCP;
using stringb = System.Text.StringBuilder;

namespace ftp_sync
{
    class Synchronizer
    {
        readonly string DEFAULT_ID = Environment.MachineName;
        const int DEFAULT_REFRESH = 60000;
        const int EVENT_LOG_MAX_LENGHT = 32766;
        const string REGEX_FILE_MASK = @".+[<>]?";

        string id;
        int refresh;
        System.Diagnostics.EventLog eventLog;

        public Synchronizer()
        {
            id = DEFAULT_ID;
            refresh = DEFAULT_REFRESH;
        }

        public System.Diagnostics.EventLog EventLog { get { return eventLog; } set { eventLog = value; } }
        public string Id { get { return id; } set { id = value; } }
        public int Refresh { get { return refresh; } set { refresh = value; } }

        private string GetDateNow()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public bool Synchronize(ConfigSyncItem config)
        {
            stringb log = new stringb();
            log.AppendLine(string.Format("[{0}] Initializing synchronization to {1}", GetDateNow(), config.Section));
            Protocol protocol;
            if (!Enum.TryParse<Protocol>(config.Protocol, true, out protocol))
            {
                log.AppendLine(string.Format("[{0}] Missing Protocol setting", GetDateNow()));
                eventLog.WriteEntry(log.ToString(), System.Diagnostics.EventLogEntryType.Error);
                return false;
            }
            FtpSecure ftpsecure;
            if (!Enum.TryParse<FtpSecure>(config.FtpSecure, true, out ftpsecure))
                ftpsecure = FtpSecure.None;
            SynchronizationMode syncmode;
            if (!Enum.TryParse<SynchronizationMode>(config.SyncTarget, true, out syncmode))
                syncmode = SynchronizationMode.Local;

            SessionOptions sessionOpt = new SessionOptions
            {
                Protocol = protocol,
                FtpSecure = ftpsecure,
                HostName = config.HostName,
                UserName = config.UserName,
                Password = config.Password,
                SslHostCertificateFingerprint = config.Fingerprint
            };

            Session session = new Session();
            if (MainClass.DEBUG)
                session.SessionLogPath = @"ftp-session.log";
            session.Open(sessionOpt);

            TransferOptions transfOpt = new TransferOptions();
            transfOpt.TransferMode = TransferMode.Binary;
            transfOpt.FileMask = config.Files;

            SynchronizationResult result = session.SynchronizeDirectories(
                syncmode, config.Local, config.Remote, false, false,
                SynchronizationCriteria.Time, transfOpt);
            if (!result.IsSuccess)
                log.AppendLine(string.Format("[{0}] Operation failed: synchronize files", GetDateNow()));
            if (result.Downloads.Count > 0)
            {
                foreach (TransferEventArgs item in result.Downloads)
                    log.AppendLine(string.Format("[{0}] Downloaded: {1}", GetDateNow(), item.FileName));
            }
            if (result.Uploads.Count > 0)
            {
                foreach (TransferEventArgs item in result.Uploads)
                    log.AppendLine(string.Format("[{0}] Uploaded: {1}", GetDateNow(), item.FileName));
            }

            Action cleanRemote = delegate()
            {
                string f = string.Format("{0}/{1}", config.Remote, config.DeleteAfter);
                RemovalOperationResult remResult = session.RemoveFiles(f);
                if (!remResult.IsSuccess)
                    log.AppendLine(string.Format("[{0}] Operation failed: remove old files", GetDateNow()));
                if (remResult.Removals.Count > 0)
                {
                    foreach (RemovalEventArgs item in remResult.Removals)
                        log.AppendLine(string.Format("[{0}] Removed: {1}", GetDateNow(), item.FileName));
                }
            };

            if (config.DeleteAfter != null)
            {
                switch (syncmode)
                {
                    case SynchronizationMode.Both:
                        CleanLocal(config.Local, config.DeleteAfter);
                        cleanRemote();
                        break;
                    case SynchronizationMode.Local:
                        cleanRemote();
                        break;
                    case SynchronizationMode.Remote:
                        CleanLocal(config.Local, config.DeleteAfter);
                        break;
                }
            }

            session.Dispose();
            log.AppendLine(string.Format("[{0}] Synchronization finalized", GetDateNow()));

            string[] logArr = SklLib.Strings.Split(log.ToString(), EVENT_LOG_MAX_LENGHT);
            foreach (string item in logArr)
                eventLog.WriteEntry(item, System.Diagnostics.EventLogEntryType.Information);

            return result.IsSuccess;
        }

        private void CleanLocal(string path, string mask)
        {
            throw new NotImplementedException();
        }
    }
}
