using System;
using System.Text.RegularExpressions;
using WinSCP;
using stringb = System.Text.StringBuilder;

namespace ftp_exchange
{
    class Synchronizer
    {
        readonly string DEFAULT_ID = Environment.MachineName;
        const int DEFAULT_REFRESH = 5;
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
            Regex r = new Regex(config.Files);
            TimeSpan temp;
            TimeSpan? filter = null;
            bool filterGT = true;
            if (config.TimeFilter != null)
            {
                if (config.TimeFilter[0] == '<')
                    filterGT = false;
                if (TimeSpan.TryParse(config.TimeFilter.Substring(1), out temp))
                    filter = temp;
            }
            TimeSpan? cleanup = null;
            bool clenupGT = true;
            if (config.Cleanup != null)
            {
                if (config.Cleanup[0] == '<')
                    clenupGT = false;
                if (TimeSpan.TryParse(config.Cleanup.Substring(1), out temp))
                    cleanup = temp;
            }
            bool move = config.Move;
            string backupFolder = config.BackupFolder;

            SessionOptions sessionOpt = new SessionOptions
            {
                Protocol = protocol,
                FtpSecure = ftpsecure,
                HostName = config.HostName,
                UserName = config.UserName,
                Password = config.Password
            };
            if (sessionOpt.FtpSecure != FtpSecure.None)
                sessionOpt.SslHostCertificateFingerprint = config.Fingerprint;

            Session session = new Session();
            if (MainClass.DEBUG)
                session.SessionLogPath = @"ftp-session.log";
            session.Open(sessionOpt);

            TransferOptions transfOpt = new TransferOptions();
            transfOpt.TransferMode = TransferMode.Binary;
            //transfOpt.FileMask = config.Files;

            switch (syncmode)
            {
                case SynchronizationMode.Both:
                    throw new NotImplementedException();
                    break;
                case SynchronizationMode.Local:
                    RemoteDirectoryInfo dirInfo = session.ListDirectory(config.Remote);
                    foreach (RemoteFileInfo item in dirInfo.Files)
                    {
                        if (item.IsDirectory || !r.IsMatch(item.Name))
                            continue;
                        if (filter.HasValue)
                        {
                            if (filterGT)
                            {
                                if (!((DateTime.Now - item.LastWriteTime) > filter))
                                    continue;
                            }
                            else
                            {
                                if (!((DateTime.Now - item.LastWriteTime) < filter))
                                    continue;
                            }
                        }

                        string origFile = string.Format("{0}/{1}", config.Remote, item.Name);
                        if (move && !string.IsNullOrWhiteSpace(backupFolder))
                        {
                            string bkpFile = string.Format("{0}/{1}", backupFolder, item.Name);
                            session.MoveFile(origFile, bkpFile);
                            origFile = bkpFile;
                            log.AppendLine(string.Format("[{0}] Done backup from file: {1}", GetDateNow(), item.Name));
                        }

                        TransferOperationResult result = session.GetFiles(origFile, config.Local, move, transfOpt);
                        if (!result.IsSuccess)
                            log.AppendLine(string.Format("[{0}] Operation failed: download files", GetDateNow()));
                        if (result.Transfers.Count > 0)
                        {
                            foreach (TransferEventArgs t in result.Transfers)
                                log.AppendLine(string.Format("[{0}] Downloaded: {1}", GetDateNow(), t.FileName));
                        }
                    }
                    break;
                case SynchronizationMode.Remote:
                    throw new NotImplementedException();
                    break;
            }
            /*
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
            }*/

            session.Dispose();
            log.AppendLine(string.Format("[{0}] Synchronization finalized", GetDateNow()));

            string[] logArr = SklLib.Strings.Split(log.ToString(), EVENT_LOG_MAX_LENGHT);
            foreach (string item in logArr)
                eventLog.WriteEntry(item, System.Diagnostics.EventLogEntryType.Information);

            //return result.IsSuccess;
            return true;
        }

        private void CleanLocal(string path, string mask)
        {
            throw new NotImplementedException();
        }
    }
}
