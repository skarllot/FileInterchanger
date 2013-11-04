using System;
using System.IO;
using WinSCP;
using stringb = System.Text.StringBuilder;

namespace ftp_exchange
{
    class Exchanger
    {
        const int DEFAULT_REFRESH = 5;
        static readonly TransferOptions DEFAULT_TRANSFER_OPTIONS = new TransferOptions
        {
            TransferMode = TransferMode.Binary
        };
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

            switch (info.SyncTarget)
            {
                case SynchronizationMode.Local:
                    if (!ExchangeToLocal(info, session, log))
                        return false;
                    if (info.Cleanup.HasValue)
                    {
                        CleanupRemote(info, session, log);
                        CleanupLocal(info, session, log);
                    }
                    break;
                case SynchronizationMode.Remote:
                    if (!ExchangeToRemote(info, session, log))
                        return false;
                    if (info.Cleanup.HasValue)
                    {
                        CleanupLocal(info, session, log);
                        CleanupRemote(info, session, log);
                    }
                    break;
                default:
                    log.AppendLine(string.Format("[{0}] Invalid exchange mode: {1}", GetDateNow(), info.SyncTarget.ToString()));
                    return false;
            }

            session.Dispose();
            log.AppendLine(string.Format("[{0}] Synchronization finalized", GetDateNow()));

            string[] logArr = SklLib.Strings.Split(log.ToString(), EVENT_LOG_MAX_LENGHT);
            foreach (string item in logArr)
                eventLog.WriteEntry(item, System.Diagnostics.EventLogEntryType.Information);

            //return result.IsSuccess;
            return true;
        }

        private bool ExchangeToLocal(ExchangeInfo info, Session session, stringb log)
        {
            RemoteDirectoryInfo dirInfo;
            try { dirInfo = session.ListDirectory(info.Remote); }
            catch (SessionRemoteException)
            {
                log.AppendLine(string.Format("[{0}] Remote directory could not be read: {1}", GetDateNow(), info.Remote));
                return false;
            }
            try { session.ListDirectory(info.BackupFolder); }
            catch (SessionRemoteException)
            {
                log.AppendLine(string.Format("[{0}] Remote backup directory could not be read: {1}", GetDateNow(), info.Remote));
                return false;
            }
            if (!Directory.Exists(info.Local))
            {
                log.AppendLine(string.Format("[{0}] Local directory does not exist: {1}", GetDateNow(), info.Local));
                return false;
            }

            foreach (RemoteFileInfo item in dirInfo.Files)
            {
                string localFile = Path.Combine(info.Local, item.Name);
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

                if (File.Exists(localFile))
                {
                    File.Delete(localFile);
                    log.AppendLine(string.Format("[{0}] Local file deleted: {1}", GetDateNow(), localFile));
                }

                TransferOperationResult result = session.GetFiles(origFile, info.Local, info.Move, DEFAULT_TRANSFER_OPTIONS);
                if (!result.IsSuccess)
                {
                    log.AppendLine(string.Format("[{0}] Operation failed: download files", GetDateNow()));
                    return false;
                }
                if (result.Transfers.Count > 0)
                {
                    foreach (TransferEventArgs t in result.Transfers)
                        log.AppendLine(string.Format("[{0}] Downloaded: {1}", GetDateNow(), t.FileName));
                }
            }

            return true;
        }

        private bool ExchangeToRemote(ExchangeInfo info, Session session, stringb log)
        {
            try { session.ListDirectory(info.Remote); }
            catch (SessionRemoteException)
            {
                log.AppendLine(string.Format("[{0}] Remote directory could not be read: {1}", GetDateNow(), info.Remote));
                return false;
            }
            DirectoryInfo lDir = new DirectoryInfo(info.Local);
            if (!lDir.Exists)
            {
                log.AppendLine(string.Format("[{0}] Local directory does not exist: {1}", GetDateNow(), info.Local));
                return false;
            }
            if (!Directory.Exists(info.BackupFolder))
            {
                log.AppendLine(string.Format("[{0}] Remote backup directory does not exist: {1}", GetDateNow(), info.BackupFolder));
                return false;
            }

            foreach (FileInfo item in lDir.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                string remoteFile = string.Format("{0}/{1}", info.Remote, item.Name);
                if (!info.Files.IsMatch(item.Name))
                    continue;
                if (info.TimeFilter.HasValue)
                {
                    if (!TimeSpanExpression.Match(DateTime.Now - item.LastWriteTime, info.TimeFilter.Value))
                        continue;
                }

                string origFile = item.FullName;
                if (info.Move && !string.IsNullOrWhiteSpace(info.BackupFolder))
                {
                    string bkpFile = Path.Combine(info.BackupFolder, item.Name);
                    File.Move(origFile, bkpFile);
                    origFile = bkpFile;
                    log.AppendLine(string.Format("[{0}] Done backup from file: {1}", GetDateNow(), item.Name));
                }

                if (session.FileExists(remoteFile))
                {
                    RemovalOperationResult remResult = session.RemoveFiles(remoteFile);
                    if (!remResult.IsSuccess)
                    {
                        log.AppendLine(string.Format("[{0}] Delete remote file failed: {1}", GetDateNow(), remoteFile));
                        return false;
                    }
                    log.AppendLine(string.Format("[{0}] Remote file deleted: {1}", GetDateNow(), remoteFile));
                }

                TransferOperationResult result = session.PutFiles(origFile, info.Remote, info.Move, DEFAULT_TRANSFER_OPTIONS);
                if (!result.IsSuccess)
                {
                    log.AppendLine(string.Format("[{0}] Operation failed: upload files", GetDateNow()));
                    return false;
                }
                if (result.Transfers.Count > 0)
                {
                    foreach (TransferEventArgs t in result.Transfers)
                        log.AppendLine(string.Format("[{0}] Uploaded: {1}", GetDateNow(), t.FileName));
                }
            }

            return true;
        }

        private bool CleanupRemote(ExchangeInfo info, Session session, stringb log)
        {
            string rDir = info.Remote;
            if (info.SyncTarget == SynchronizationMode.Local
                && info.Move && !string.IsNullOrWhiteSpace(info.BackupFolder))
                rDir = info.BackupFolder;

            RemoteDirectoryInfo dirInfo;
            try { dirInfo = session.ListDirectory(rDir); }
            catch (SessionRemoteException)
            {
                log.AppendLine(string.Format("[{0}] Remote directory could not be read: {1}", GetDateNow(), rDir));
                return false;
            }

            foreach (RemoteFileInfo item in dirInfo.Files)
            {
                if (item.IsDirectory || !info.Files.IsMatch(item.Name))
                    continue;

                string rFile = string.Format("{0}/{1}", rDir, item.Name);
                if (!TimeSpanExpression.Match(DateTime.Now - item.LastWriteTime, info.Cleanup.Value))
                    continue;

                RemovalOperationResult result = session.RemoveFiles(rFile);
                if (!result.IsSuccess)
                {
                    log.AppendLine(string.Format("[{0}] Operation failed: old files cleanup", GetDateNow()));
                    return false;
                }
                if (result.Removals.Count > 0)
                {
                    foreach (RemovalEventArgs r in result.Removals)
                        log.AppendLine(string.Format("[{0}] Removed: {1}", GetDateNow(), r.FileName));
                }
            }

            return true;
        }

        private bool CleanupLocal(ExchangeInfo info, Session session, stringb log)
        {
            string lDir = info.Local;
            if (info.SyncTarget == SynchronizationMode.Remote
                && info.Move && !string.IsNullOrWhiteSpace(info.BackupFolder))
                lDir = info.BackupFolder;

            DirectoryInfo dirInfo = new DirectoryInfo(lDir);
            if (!dirInfo.Exists)
            {
                log.AppendLine(string.Format("[{0}] Local directory does not exist: {1}", GetDateNow(), lDir));
                return false;
            }

            foreach (FileInfo item in dirInfo.GetFiles("*", SearchOption.TopDirectoryOnly))
            {
                if (!info.Files.IsMatch(item.Name))
                    continue;

                if (!TimeSpanExpression.Match(DateTime.Now - item.LastWriteTime, info.Cleanup.Value))
                    continue;

                File.Delete(item.FullName);
                log.AppendLine(string.Format("[{0}] Removed: {1}", GetDateNow(), item.Name));
            }

            return true;
        }
    }
}
