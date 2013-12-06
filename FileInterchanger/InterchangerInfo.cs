// ExchangeInfo.cs
//
// Copyright (C) 2013 Fabrício Godoy
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <http://www.gnu.org/licenses/>.
//

using System;
using System.Net;
using System.Text.RegularExpressions;
using WinSCP;

namespace FileInterchanger
{
    struct InterchangerInfo
    {
        string backupFolder;
        TimeSpanExpression? cleanup;
        bool cleanupTarget;
        bool disableSkipEmpty;
        Regex files;
        string fingerprint;
        NetworkCredential ftpCredential;
        FtpSecure ftpSecure;
        string hostname;
        string id;
        string local;
        bool move;
        NetworkCredential netCredential;
        Protocol protocol;
        string remote;
        SynchronizationMode syncTarget;
        TimeSpanExpression? timeFilter;

        public string BackupFolder { get { return backupFolder; } }
        public TimeSpanExpression? Cleanup { get { return cleanup; } }
        public bool CleanupTarget { get { return cleanupTarget; } }
        public bool DisableSkipEmpty { get { return disableSkipEmpty; } }
        public Regex Files { get { return files; } }
        public string Fingerprint { get { return fingerprint; } }
        public NetworkCredential FtpCredential { get { return ftpCredential; } }
        public FtpSecure FtpSecure { get { return ftpSecure; } }
        public string HostName { get { return hostname; } }
        public string Id { get { return id; } }
        public string Local { get { return local; } }
        public bool Move { get { return move; } }
        public NetworkCredential NetworkCredential { get { return netCredential; } }
        public Protocol Protocol { get { return protocol; } }
        public string Remote { get { return remote; } }
        public SynchronizationMode SyncTarget { get { return syncTarget; } }
        public TimeSpanExpression? TimeFilter { get { return timeFilter; } }

        public static InterchangerInfo Parse(IO.ConfigReaderItem cfg, IO.CredentialsReader cred)
        {
            InterchangerInfo result = new InterchangerInfo();
            result.backupFolder = cfg.BackupFolder;
            result.cleanupTarget = cfg.CleanupTarget;
            result.disableSkipEmpty = cfg.DisableSkipEmpty;
            result.fingerprint = cfg.Fingerprint;
            result.hostname = cfg.HostName;
            result.id = cfg.Section;
            result.local = cfg.Local;
            result.move = cfg.Move;
            result.remote = cfg.Remote;

            try { result.cleanup = TimeSpanExpression.Parse(cfg.Cleanup); }
            catch { result.cleanup = null; }

            try { result.files = new Regex(cfg.Files, RegexOptions.IgnoreCase); }
            catch { throw new Exception("Invalid regular expression for Files"); }

            if (cfg.FtpCredential != null)
            {
                IO.CredentialItemReader p0 = cred[cfg.FtpCredential];
                if (p0 == null)
                    throw new Exception(string.Format("Invalid credential name: {0}", cfg.FtpCredential));
                result.ftpCredential = new NetworkCredential(p0.UserName, p0.Password);
            }

            FtpSecure p1;
            if (!Enum.TryParse<FtpSecure>(cfg.FtpSecure, true, out p1))
                p1 = FtpSecure.None;
            result.ftpSecure = p1;

            if (cfg.NetworkCredential != null)
            {
                IO.CredentialItemReader p4 = cred[cfg.NetworkCredential];
                if (p4 == null)
                    throw new Exception(string.Format("Invalid credential name: {0}", cfg.NetworkCredential));
                result.netCredential = new NetworkCredential(p4.UserName, p4.Password, p4.Domain);
            }

            Protocol p2;
            if (!Enum.TryParse<Protocol>(cfg.Protocol, true, out p2))
                throw new Exception("Protocol is not properly defined");
            result.protocol = p2;

            SynchronizationMode p3;
            if (!Enum.TryParse<SynchronizationMode>(cfg.SyncTarget, true, out p3))
                throw new Exception("SyncTarget is not properly defined");
            result.syncTarget = p3;

            try { result.timeFilter = TimeSpanExpression.Parse(cfg.TimeFilter); }
            catch { result.timeFilter = null; }

            return result;
        }
    }
}
