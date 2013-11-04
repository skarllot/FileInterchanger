using System;
using System.Text.RegularExpressions;
using WinSCP;

namespace ftp_exchange
{
    struct ExchangeInfo
    {
        string backupFolder;
        TimeSpanExpression? cleanup;
        bool cleanupTarget;
        Regex files;
        string fingerprint;
        FtpSecure ftpSecure;
        string hostname;
        string id;
        string local;
        bool move;
        string password;
        Protocol protocol;
        string remote;
        SynchronizationMode syncTarget;
        TimeSpanExpression? timeFilter;
        string username;

        public string BackupFolder { get { return backupFolder; } }
        public TimeSpanExpression? Cleanup { get { return cleanup; } }
        public bool CleanupTarget { get { return cleanupTarget; } }
        public Regex Files { get { return files; } }
        public string Fingerprint { get { return fingerprint; } }
        public FtpSecure FtpSecure { get { return ftpSecure; } }
        public string HostName { get { return hostname; } }
        public string Id { get { return id; } }
        public string Local { get { return local; } }
        public bool Move { get { return move; } }
        public string Password { get { return password; } }
        public Protocol Protocol { get { return protocol; } }
        public string Remote { get { return remote; } }
        public SynchronizationMode SyncTarget { get { return syncTarget; } }
        public TimeSpanExpression? TimeFilter { get { return timeFilter; } }
        public string UserName { get { return username; } }

        public static ExchangeInfo Parse(ConfigReaderItem item)
        {
            ExchangeInfo result = new ExchangeInfo();
            result.backupFolder = item.BackupFolder;
            result.cleanupTarget = item.CleanupTarget;
            result.fingerprint = item.Fingerprint;
            result.hostname = item.HostName;
            result.id = item.Section;
            result.local = item.Local;
            result.move = item.Move;
            result.password = item.Password;
            result.remote = item.Remote;
            result.username = item.UserName;

            try { result.cleanup = TimeSpanExpression.Parse(item.Cleanup); }
            catch { result.cleanup = null; }

            try { result.files = new Regex(item.Files); }
            catch { throw new Exception("Invalid regular expression for Files"); }

            FtpSecure p1;
            if (!Enum.TryParse<FtpSecure>(item.FtpSecure, true, out p1))
                p1 = FtpSecure.None;
            result.ftpSecure = p1;

            Protocol p2;
            if (!Enum.TryParse<Protocol>(item.Protocol, true, out p2))
                throw new Exception("Protocol is not properly defined");
            result.protocol = p2;

            SynchronizationMode p3;
            if (!Enum.TryParse<SynchronizationMode>(item.SyncTarget, true, out p3))
                throw new Exception("SyncTarget is not properly defined");
            result.syncTarget = p3;

            try { result.timeFilter = TimeSpanExpression.Parse(item.TimeFilter); }
            catch { result.timeFilter = null; }

            return result;
        }
    }
}
