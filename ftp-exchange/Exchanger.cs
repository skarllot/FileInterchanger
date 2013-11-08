using System;
using System.Diagnostics;
using System.IO;
using WinSCP;
using stringb = System.Text.StringBuilder;

namespace ftp_exchange
{
    class Exchanger
    {
        const string EVT_LOG = "FtpExchange";
        const int EVTID_CRED_CONFLICT = 1;
        const int EVTID_CRED_ERROR = 2;
        const int EVTID_EXCHANGE_COMPLETED = 3;
        const int EVTID_HOSTNAME_INVALID = 4;
        const int EVTID_SESSION_OPEN_ERROR = 5;
        const int EVTID_UNEXPECTED_ERROR = 99;

        static readonly TransferOptions DEFAULT_TRANSFER_OPTIONS = new TransferOptions
        {
            TransferMode = TransferMode.Binary
        };
        const int EVENT_LOG_MAX_LENGTH = 15000;

        EventLog eventLog;

        public Exchanger()
        {
            eventLog = new EventLog();
            eventLog.Source = "Transfer";
            eventLog.Log = EVT_LOG;
        }

        private string GetDateNow()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public bool Exchange(ExchangeInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.HostName))
            {
                eventLog.WriteEntry(string.Format("Error on {0}\nFTP hostname was not provided",
                    info.Id), EventLogEntryType.Error, EVTID_HOSTNAME_INVALID);
                return false;
            }

            stringb log = new stringb();
            log.AppendLine(string.Format("[{0}] Initializing synchronization to {1}", GetDateNow(), info.Id));

            SessionOptions sessionOpt = new SessionOptions
            {
                Protocol = info.Protocol,
                FtpSecure = info.FtpSecure,
                HostName = info.HostName
            };
            if (info.FtpCredential != null)
            {
                sessionOpt.UserName = info.FtpCredential.UserName;
                sessionOpt.Password = info.FtpCredential.Password;
            }
            if (sessionOpt.FtpSecure != FtpSecure.None)
                sessionOpt.SslHostCertificateFingerprint = info.Fingerprint;

            Session session = null;
            NetworkConnection netConn = null;

            if (info.NetworkCredential != null)
            {
                try { netConn = new NetworkConnection(info.Local, info.NetworkCredential); }
                catch (System.ComponentModel.Win32Exception e)
                {
                    if (e.NativeErrorCode == NetworkConnection.ERROR_SESSION_CREDENTIAL_CONFLICT)
                    {
                        eventLog.WriteEntry(string.Format(
                            "Error on {0}\nA connection to a shared resource using another credential already exists",
                            info.Id), EventLogEntryType.Error, EVTID_CRED_CONFLICT);
                    }
                    else
                    {
                        eventLog.WriteEntry(string.Format(
                            "Error on {0}\nError connecting to shared resource using provided credentials: {1}",
                            info.Id, e.Message), EventLogEntryType.Error, EVTID_CRED_ERROR);
                    }
                    return false;
                }
            }

            try
            {
                session = new Session();
                if (MainClass.DEBUG)
                    session.SessionLogPath = @"ftp-session.log";
                session.Open(sessionOpt);
                log.AppendLine(string.Format("[{0}] Connected to {1}@{2}", GetDateNow(),
                    sessionOpt.UserName ?? string.Empty, info.HostName));

                switch (info.SyncTarget)
                {
                    case SynchronizationMode.Local:
                        if (!ExchangeToLocal(info, session, log))
                            return false;
                        if (info.Cleanup.HasValue)
                        {
                            CleanupRemote(info, session, log);
                            if (info.CleanupTarget)
                                CleanupLocal(info, session, log);
                        }
                        break;
                    case SynchronizationMode.Remote:
                        if (!ExchangeToRemote(info, session, log))
                            return false;
                        if (info.Cleanup.HasValue)
                        {
                            CleanupLocal(info, session, log);
                            if (info.CleanupTarget)
                                CleanupRemote(info, session, log);
                        }
                        break;
                    default:
                        log.AppendLine(string.Format("[{0}] Invalid exchange mode: {1}", GetDateNow(), info.SyncTarget.ToString()));
                        return false;
                }
            }
            catch (SessionRemoteException)
            {
                eventLog.WriteEntry(string.Format(
                    "Error on {0}\nFailed to authenticate or connect to server",
                    info.Id), EventLogEntryType.Error, EVTID_SESSION_OPEN_ERROR);
                log.Clear();
                return false;
            }
            catch (Exception e)
            {
                string msg = string.Format("Error on {0}\nMessage: {1}\nSource: {2}\nStack Trace: {3}",
                    info.Id, e.Message, e.Source, e.StackTrace);
                string[] logArr = SklLib.Strings.Split(msg, EVENT_LOG_MAX_LENGTH);
                foreach (string item in logArr)
                    eventLog.WriteEntry(item, EventLogEntryType.Error, EVTID_UNEXPECTED_ERROR);
                Environment.Exit(EVTID_UNEXPECTED_ERROR);
            }
            finally
            {
                if (netConn != null)
                    netConn.Dispose();
                if (session != null)
                    session.Dispose();
                log.AppendLine(string.Format("[{0}] Synchronization finalized", GetDateNow()));

                string[] logArr = SklLib.Strings.Split(log.ToString(), EVENT_LOG_MAX_LENGTH);
                foreach (string item in logArr)
                    eventLog.WriteEntry(item, EventLogEntryType.Information, EVTID_EXCHANGE_COMPLETED);
            }

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
            if (!string.IsNullOrWhiteSpace(info.BackupFolder))
            {
                try { session.ListDirectory(info.BackupFolder); }
                catch (SessionRemoteException)
                {
                    log.AppendLine(string.Format("[{0}] Remote backup directory could not be read: {1}", GetDateNow(), info.Remote));
                    return false;
                }
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

                bool move = info.Move;
                string origFile = string.Format("{0}/{1}", info.Remote, item.Name);
                if (info.Move && !string.IsNullOrWhiteSpace(info.BackupFolder))
                {
                    string bkpFile = string.Format("{0}/{1}", info.BackupFolder, item.Name);
                    session.MoveFile(origFile, bkpFile);
                    origFile = bkpFile;
                    move = false;
                    log.AppendLine(string.Format("[{0}] Done backup from file: {1}", GetDateNow(), item.Name));
                }

                if (File.Exists(localFile))
                {
                    if (!info.Move)
                    {
                        log.AppendLine(string.Format("[{0}] Local file '{1}' already exists", GetDateNow(), item.Name));
                        continue;
                    }
                    int counter = 1;
                    string ext = Path.GetExtension(item.Name);
                    string fname = Path.GetFileNameWithoutExtension(item.Name);
                    do
                    {
                        localFile = string.Format(@"{0}\{1}-{2}{3}", info.Local, fname, counter, ext);
                        counter++;
                    } while (File.Exists(localFile));
                    log.AppendLine(string.Format("[{0}] Local file '{1}' already exists. New name: {2}",
                        GetDateNow(), item.Name, Path.GetFileName(localFile)));
                }

                TransferOperationResult result = session.GetFiles(origFile, localFile, move, DEFAULT_TRANSFER_OPTIONS);
                if (!result.IsSuccess)
                {
                    log.AppendLine(string.Format("[{0}] Operation failed: download files", GetDateNow()));
                    return false;
                }
                if (result.Transfers.Count > 0)
                {
                    foreach (TransferEventArgs t in result.Transfers)
                        log.AppendLine(string.Format("[{0}] Downloaded: {1} to {2}", GetDateNow(), t.FileName, t.Destination));
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
            if (!string.IsNullOrWhiteSpace(info.BackupFolder))
            {
                if (!Directory.Exists(info.BackupFolder))
                {
                    log.AppendLine(string.Format("[{0}] Remote backup directory does not exist: {1}", GetDateNow(), info.BackupFolder));
                    return false;
                }
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

                bool move = info.Move;
                string origFile = item.FullName;
                if (info.Move && !string.IsNullOrWhiteSpace(info.BackupFolder))
                {
                    string bkpFile = Path.Combine(info.BackupFolder, item.Name);
                    File.Move(origFile, bkpFile);
                    origFile = bkpFile;
                    move = false;
                    log.AppendLine(string.Format("[{0}] Done backup from file: {1}", GetDateNow(), item.Name));
                }

                if (session.FileExists(remoteFile))
                {
                    if (!info.Move)
                    {
                        log.AppendLine(string.Format("[{0}] Remote file '{1}' already exists", GetDateNow(), item.Name));
                        continue;
                    }
                    int counter = 1;
                    string ext = Path.GetExtension(item.Name);
                    string fname = Path.GetFileNameWithoutExtension(item.Name);
                    do
                    {
                        remoteFile = string.Format(@"{0}/{1}-{2}{3}", info.Remote, fname, counter, ext);
                        counter++;
                    } while (session.FileExists(remoteFile));
                    log.AppendLine(string.Format("[{0}] Remote file '{1}' already exists. New name: {2}",
                        GetDateNow(), item.Name, Path.GetFileName(remoteFile)));
                }

                TransferOperationResult result = session.PutFiles(origFile, remoteFile, move, DEFAULT_TRANSFER_OPTIONS);
                if (!result.IsSuccess)
                {
                    log.AppendLine(string.Format("[{0}] Operation failed: upload files", GetDateNow()));
                    return false;
                }
                if (result.Transfers.Count > 0)
                {
                    foreach (TransferEventArgs t in result.Transfers)
                        log.AppendLine(string.Format("[{0}] Uploaded: {1} to {2}", GetDateNow(), t.FileName, t.Destination));
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
