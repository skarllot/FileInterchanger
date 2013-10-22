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
            //session.SessionLogPath = @"C:\temp\ftp-session.log";
            session.Open(sessionOpt);

            TransferOptions transfOpt = new TransferOptions();
            transfOpt.TransferMode = TransferMode.Binary;
            transfOpt.FileMask = config.Files;

            //RemoteDirectoryInfo dir = session.ListDirectory(config.Remote);
            TransferOperationResult result = null;

            switch (syncmode)
            {
                case SynchronizationMode.Both:
                    throw new NotImplementedException();
                    //break;
                case SynchronizationMode.Local:
                    result = session.GetFiles(config.Remote, config.Local, false, transfOpt);
                    if (!result.IsSuccess)
                        log.AppendLine(string.Format("[{0}] Operation failed: downloaded files", GetDateNow()));
                    if (result.Transfers.Count > 0)
                    {
                        foreach (TransferEventArgs item in result.Transfers)
                            log.AppendLine(string.Format("[{0}] Downloaded: {1}", GetDateNow(), item.FileName));
                    }

                    string f = string.Format("{0}/{1}", config.Remote, config.DeleteAfter);
                    RemovalOperationResult remResult = session.RemoveFiles(f);
                    if (!remResult.IsSuccess)
                        log.AppendLine(string.Format("[{0}] Operation failed: remove old files", GetDateNow()));
                    if (remResult.Removals.Count > 0)
                    {
                        foreach (RemovalEventArgs item in remResult.Removals)
                            log.AppendLine(string.Format("[{0}] Removed: {1}", GetDateNow(), item.FileName));
                    }
                    break;
                case SynchronizationMode.Remote:
                    throw new NotImplementedException();
                    //break;
            }

            log.AppendLine(string.Format("[{0}] Synchronization finalized", GetDateNow()));
            if (log.Length > EVENT_LOG_MAX_LENGHT)
                log.Remove(EVENT_LOG_MAX_LENGHT - 1, log.Length - EVENT_LOG_MAX_LENGHT);
            eventLog.WriteEntry(log.ToString(), System.Diagnostics.EventLogEntryType.Information);
            return result.IsSuccess;
        }
    }
}
