using System;
using System.Text.RegularExpressions;
using WinSCP;
using stringb = System.Text.StringBuilder;

namespace ftp_exchange
{
    class Exchanger
    {
        const int DEFAULT_REFRESH = 5;
        const int EVENT_LOG_MAX_LENGHT = 32766;

        int refresh;
        System.Diagnostics.EventLog eventLog;

        public Exchanger()
        {
            refresh = DEFAULT_REFRESH;
        }

        public System.Diagnostics.EventLog EventLog { get { return eventLog; } set { eventLog = value; } }
        public int Refresh { get { return refresh; } set { refresh = value; } }

        private string GetDateNow()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public bool Exchange(ExchangeInfo info)
        {
            stringb log = new stringb();
            log.AppendLine(string.Format("[{0}] Initializing synchronization to {1}", GetDateNow(), info.Id));

            SessionOptions sessionOpt = new SessionOptions
            {
                Protocol = info.Protocol,
                FtpSecure = info.FtpSecure,
                HostName = info.HostName,
                UserName = info.UserName,
                Password = info.Password
            };
            if (sessionOpt.FtpSecure != FtpSecure.None)
                sessionOpt.SslHostCertificateFingerprint = info.Fingerprint;

            Session session = new Session();
            if (MainClass.DEBUG)
                session.SessionLogPath = @"ftp-session.log";
            session.Open(sessionOpt);

            TransferOptions transfOpt = new TransferOptions();
            transfOpt.TransferMode = TransferMode.Binary;
            //transfOpt.FileMask = config.Files;

            switch (info.SyncTarget)
            {
                case SynchronizationMode.Local:
                    RemoteDirectoryInfo dirInfo = session.ListDirectory(info.Remote);
                    foreach (RemoteFileInfo item in dirInfo.Files)
                    {
                        if (item.IsDirectory || !info.Files.IsMatch(item.Name))
                            continue;
                        if (info.TimeFilter.HasValue)
                        {
                            if (!TimeSpanExpression.Match(DateTime.Now - item.LastWriteTime, info.TimeFilter.Value))
                                continue;
                        }

                        string origFile = string.Format("{0}/{1}", info.Remote, item.Name);
                        if (info.Move && !string.IsNullOrWhiteSpace(info.BackupFolder))
                        {
                            string bkpFile = string.Format("{0}/{1}", info.BackupFolder, item.Name);
                            session.MoveFile(origFile, bkpFile);
                            origFile = bkpFile;
                            log.AppendLine(string.Format("[{0}] Done backup from file: {1}", GetDateNow(), item.Name));
                        }

                        TransferOperationResult result = session.GetFiles(origFile, info.Local, info.Move, transfOpt);
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
                default:
                    log.AppendLine(string.Format("[{0}] Invalid exchange mode: {1}", GetDateNow(), info.SyncTarget.ToString()));
                    return false;
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
