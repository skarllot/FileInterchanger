// Exchanger.cs
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
using System.Diagnostics;
using System.IO;
using WinSCP;
using stringb = System.Text.StringBuilder;

namespace FileInterchanger
{
    class Interchanger
    {
        static readonly TransferOptions DEFAULT_TRANSFER_OPTIONS = new TransferOptions
        {
            TransferMode = TransferMode.Binary
        };

        Logger eventLog = Logger.Default;

        private string GetDateNow()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public bool Exchange(InterchangerInfo info)
        {
            if (string.IsNullOrWhiteSpace(info.HostName))
            {
                eventLog.WriteEntry(string.Format("Error on {0}\nFTP hostname was not provided",
                    info.Id), EventLogEntryType.Error, EventId.HostnameInvalid);
                return false;
            }

            stringb log = new stringb();
            log.AppendLine(string.Format("[{0}] Initializing interchange to {1}", GetDateNow(), info.Id));

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

            System.Net.NetworkCredential netCred =
                info.NetworkCredential ?? new System.Net.NetworkCredential();

            bool result = true;
            int transferCount = 0;
            using (NetworkConnection netConn =
                new NetworkConnection(info.Local, netCred))
            using (Session session = new Session())
            {
                if (MainClass.DEBUG)
                    session.SessionLogPath = @"ftp-session.log";

                if (info.NetworkCredential != null)
                {
                    try { netConn.Connect(); }
                    catch (System.ComponentModel.Win32Exception e)
                    {
                        if (e.NativeErrorCode == NetworkConnection.ERROR_SESSION_CREDENTIAL_CONFLICT)
                        {
                            eventLog.WriteEntry(string.Format(
                                "Error on {0}\nA connection to a shared resource using another credential already exists",
                                info.Id), EventLogEntryType.Error, EventId.CredentialConflict);
                        }
                        else
                        {
                            eventLog.WriteEntry(string.Format(
                                "Error on {0}\nError connecting to shared resource using provided credentials: {1}",
                                info.Id, e.Message), EventLogEntryType.Error, EventId.CredentialError);
                        }
                        return false;
                    }
                }

                try { session.Open(sessionOpt); }
                catch (SessionRemoteException)
                {
                    eventLog.WriteEntry(string.Format(
                        "Error on {0}\nFailed to authenticate or connect to server",
                        info.Id), EventLogEntryType.Error, EventId.SessionOpenError);
                    log.Clear();
                    return false;
                }
                catch (Exception e)
                {
                    LogUnexpectedException(e, info.Id);
                    return false;
                }

                log.AppendLine(string.Format("[{0}] Connected to {1}@{2}",
                    GetDateNow(), sessionOpt.UserName ?? string.Empty, info.HostName));
                log.AppendLine(string.Format("[{0}] Local path set to {1}",
                    GetDateNow(), info.Local));

                try
                {
                    switch (info.SyncTarget)
                    {
                        case SynchronizationMode.Local:
                            result = WinscpToLocal(info, session, log, out transferCount);
                            break;
                        case SynchronizationMode.Remote:
                            result = WinscpToRemote(info, session, log, out transferCount);
                            break;
                        default:
                            string evmsg = string.Format("Invalid exchange mode: {1}", info.SyncTarget.ToString());
                            log.AppendLine(string.Format("[{0}] " + evmsg, GetDateNow()));
                            eventLog.WriteEntry(evmsg, EventLogEntryType.Error, EventId.SyncTargetInvalid);
                            result = false;
                            return false;
                    }
                }
                catch (Exception e)
                {
                    LogUnexpectedException(e, info.Id);
                    return false;
                }
                finally
                {
                    if (log.Length > 0)
                    {
                        log.AppendLine(string.Format("[{0}] Interchange finalized", GetDateNow()));
                        EventLogEntryType evType = EventLogEntryType.Information;
                        if (!result)
                            evType = EventLogEntryType.Warning;
                        EventId evId;
                        if (transferCount > 0)
                        {
                            if (result)
                                evId = EventId.InterchangeCompleted;
                            else
                                evId = EventId.InterchangeCompletedWithErrors;
                        }
                        else
                        {
                            if (result)
                                evId = EventId.InterchangeCompletedEmpty;
                            else
                                evId = EventId.InterchangeCompletedEmptyWithErrors;
                        }
                        eventLog.WriteEntry(log.ToString(), evType, evId);
                    }
                }

                if (!result)
                    return false;

                if (info.Cleanup.HasValue)
                {
                    log.Clear();
                    result = true;
                    try
                    {
                        switch (info.SyncTarget)
                        {
                            case SynchronizationMode.Local:
                                result = CleanupRemote(info, session, log, out transferCount);
                                break;
                            case SynchronizationMode.Remote:
                                result = CleanupLocal(info, session, log, out transferCount);
                                break;
                        }
                    }
                    catch (Exception e)
                    {
                        LogUnexpectedException(e, info.Id);
                        return false;
                    }
                    finally
                    {
                        if (log.Length > 0)
                        {

                            log.AppendLine(string.Format("[{0}] Origin cleanup finalized", GetDateNow()));
                            EventLogEntryType evType = EventLogEntryType.Information;
                            if (!result)
                                evType = EventLogEntryType.Warning;
                            EventId evId;
                            if (transferCount > 0)
                            {
                                if (result)
                                    evId = EventId.CleanupOriginCompleted;
                                else
                                    evId = EventId.CleanupOriginCompletedWithErrors;
                            }
                            else
                            {
                                if (result)
                                    evId = EventId.CleanupOriginCompletedEmpty;
                                else
                                    evId = EventId.CleanupOriginCompletedEmptyWithErrors;
                            }
                            eventLog.WriteEntry(log.ToString(), evType, evId);
                        }
                    }

                    if (!result)
                        return false;

                    if (info.CleanupTarget)
                    {
                        log.Clear();
                        result = true;
                        try
                        {
                            switch (info.SyncTarget)
                            {
                                case SynchronizationMode.Local:
                                    result = CleanupLocal(info, session, log, out transferCount);
                                    break;
                                case SynchronizationMode.Remote:
                                    result = CleanupRemote(info, session, log, out transferCount);
                                    break;
                            }
                        }
                        catch (Exception e)
                        {
                            LogUnexpectedException(e, info.Id);
                            return false;
                        }
                        finally
                        {
                            if (log.Length > 0)
                            {

                                log.AppendLine(string.Format("[{0}] Target cleanup finalized", GetDateNow()));
                                EventLogEntryType evType = EventLogEntryType.Information;
                                if (!result)
                                    evType = EventLogEntryType.Warning;
                                EventId evId;
                                if (transferCount > 0)
                                {
                                    if (result)
                                        evId = EventId.CleanupTargetCompleted;
                                    else
                                        evId = EventId.CleanupTargetCompletedWithErrors;
                                }
                                else
                                {
                                    if (result)
                                        evId = EventId.CleanupTargetCompletedEmpty;
                                    else
                                        evId = EventId.CleanupTargetCompletedEmptyWithErrors;
                                }
                                eventLog.WriteEntry(log.ToString(), evType, evId);
                            }
                        }

                        return result;
                    }
                }
            }

            return true;
        }

        private void LogUnexpectedException(Exception e, string exchangeId)
        {
            string msg = string.Format("Error on {0}\nMessage: {1}\nSource: {2}\nStack Trace: {3}",
                        exchangeId, e.Message, e.Source, e.StackTrace);
            eventLog.WriteEntry(msg, EventLogEntryType.Error, EventId.UnexpectedError);
            Environment.Exit((int)EventId.UnexpectedError);
        }

        private bool WinscpToLocal(InterchangerInfo info, Session session, stringb log, out int transferCount)
        {
            transferCount = 0;
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
                if (!info.DisableSkipEmpty && item.Length == 0)
                {
                    log.AppendLine(string.Format("[{0}] Skipped empty file: {1}", GetDateNow(), item.Name));
                    continue;
                }
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

                TransferOperationResult result = session.GetFiles(origFile, localFile, false, DEFAULT_TRANSFER_OPTIONS);
                if (!result.IsSuccess)
                {
                    string evmsg = "Error downloading files";
                    log.AppendLine(string.Format("[{0}] " + evmsg, GetDateNow()));
                    eventLog.WriteEntry(evmsg, EventLogEntryType.Error, EventId.TransfLocalDownloadError);
                    return false;
                }

                if (result.Transfers.Count == 1)
                {
                    if (File.Exists(localFile))
                    {
                        long localLength = new FileInfo(localFile).Length;
                        if (item.Length == localLength)
                        {
                            if (move) session.RemoveFiles(origFile);
                            log.AppendLine(string.Format("[{0}] Downloaded: {1}", GetDateNow(), origFile));
                            transferCount++;
                        }
                        else
                        {
                            File.Delete(localFile);
                            string evmsg = string.Format("Size from downloaded file '{0}' do not match its remote counterpart", localFile);
                            log.AppendLine(string.Format("[{0}] " + evmsg, GetDateNow()));
                            eventLog.WriteEntry(evmsg, EventLogEntryType.Error, EventId.TransfLocalFilesNotMatch);
                            return false;
                        }
                    }
                    else
                    {
                        string evmsg = string.Format("Downloaded file '{0}' cannot be found", localFile);
                        log.AppendLine(string.Format("[{0}] " + evmsg, GetDateNow()));
                        eventLog.WriteEntry(evmsg, EventLogEntryType.Error, EventId.TransfLocalFileNotExists);
                        return false;
                    }
                }
                else if (result.Transfers.Count == 0)
                {
                    string evmsg = string.Format("Remote file '{0}' cannot be found", origFile);
                    log.AppendLine(string.Format("[{0}] " + evmsg, GetDateNow()));
                    eventLog.WriteEntry(evmsg, EventLogEntryType.Error, EventId.TransfLocalEmptyDownload);
                    return false;
                }
                else    // Count > 1?
                {
                    string evmsg = "Multiple files was downloaded instead of one";
                    log.AppendLine(string.Format("[{0}] " + evmsg, GetDateNow()));
                    foreach (TransferEventArgs t in result.Transfers)
                    {
                        log.Append(string.Format("[{0}] Downloaded: {1}", GetDateNow(), t.FileName));
                        File.Delete(Path.Combine(info.Local, Path.GetFileName(t.FileName)));
                        log.AppendLine(" ... deleted");
                    }
                    eventLog.WriteEntry(evmsg, EventLogEntryType.Error, EventId.TransfLocalMultFiles);
                    return false;
                }
            }

            return true;
        }

        private bool WinscpToRemote(InterchangerInfo info, Session session, stringb log, out int transferCount)
        {
            transferCount = 0;
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
                if (!info.DisableSkipEmpty && item.Length == 0)
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

                TransferOperationResult result = session.PutFiles(origFile, remoteFile, false, DEFAULT_TRANSFER_OPTIONS);
                if (!result.IsSuccess)
                {
                    string evmsg = "Error uploading files";
                    log.AppendLine(string.Format("[{0}] " + evmsg, GetDateNow()));
                    eventLog.WriteEntry(evmsg, EventLogEntryType.Error, EventId.TransfRemoteUploadError);
                    return false;
                }

                if (result.Transfers.Count == 1)
                {
                    if (session.FileExists(remoteFile))
                    {
                        long remoteLength = session.GetFileInfo(remoteFile).Length;
                        if (item.Length == remoteLength)
                        {
                            if (move) item.Delete();
                            log.AppendLine(string.Format("[{0}] Uploaded: {1}",
                                GetDateNow(), result.Transfers[0].FileName));
                            transferCount++;
                        }
                        else
                        {
                            session.RemoveFiles(remoteFile);
                            string evmsg = string.Format("Uploaded file '{0}' size do not match local file", remoteFile);
                            log.AppendLine(string.Format("[{0}] " + evmsg, GetDateNow()));
                            eventLog.WriteEntry(evmsg, EventLogEntryType.Error, EventId.TransfRemoteFilesNotMatch);
                            return false;
                        }
                    }
                    else
                    {
                        string evmsg = string.Format("Uploaded file '{0}' cannot be found", remoteFile);
                        log.AppendLine(string.Format("[{0}] " + evmsg, GetDateNow()));
                        eventLog.WriteEntry(evmsg, EventLogEntryType.Error, EventId.TransfRemoteFileNotExists);
                        return false;
                    }
                }
                else if (result.Transfers.Count == 0)
                {
                    string evmsg = string.Format("Local file '{0}' cannot be found", origFile);
                    log.AppendLine(string.Format("[{0}] " + evmsg, GetDateNow()));
                    eventLog.WriteEntry(evmsg, EventLogEntryType.Error, EventId.TransfRemoteEmptyUpload);
                    return false;
                }
                else    // Count > 1?
                {
                    string evmsg = "Multiple files was uploaded instead of one";
                    log.AppendLine(string.Format("[{0}] " + evmsg, GetDateNow()));
                    foreach (TransferEventArgs t in result.Transfers)
                    {
                        log.Append(string.Format("[{0}] Uploaded: {1}", GetDateNow(), t.FileName));
                        session.RemoveFiles(string.Format("{0}/{1}", info.Remote, Path.GetFileName(t.FileName)));
                        log.AppendLine(" ... deleted");
                    }
                    eventLog.WriteEntry(evmsg, EventLogEntryType.Error, EventId.TransfRemoteMultFiles);
                    return false;
                }
            }

            return true;
        }

        private bool CleanupRemote(InterchangerInfo info, Session session, stringb log, out int removeCount)
        {
            removeCount = 0;
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
                    string evmsg = "Operation failed: old files cleanup";
                    log.AppendLine(string.Format("[{0}] " + evmsg, GetDateNow()));
                    eventLog.WriteEntry(evmsg, EventLogEntryType.Error, EventId.CleanupRemoteRemoveError);
                    return false;
                }

                if (result.Removals.Count == 1)
                {
                    log.AppendLine(string.Format("[{0}] Removed: {1}", GetDateNow(), result.Removals[0].FileName));
                    removeCount++;
                }
                else if (result.Removals.Count == 0)
                {
                    string evmsg = "Remote file '{0}' could not be removed";
                    log.AppendLine(string.Format("[{0}] " + evmsg, GetDateNow()));
                    eventLog.WriteEntry(evmsg, EventLogEntryType.Error, EventId.CleanupRemoteRemoveEmpty);
                    return false;
                }
                else    // Count > 1?
                {
                    string evmsg = "Multiple files was removed instead of one";
                    log.AppendLine(string.Format("[{0}] " + evmsg, GetDateNow()));
                    foreach (RemovalEventArgs r in result.Removals)
                    {
                        log.AppendLine(string.Format("[{0}] Removed: {1}", GetDateNow(), r.FileName));
                        removeCount++;
                    }
                    eventLog.WriteEntry(evmsg, EventLogEntryType.Error, EventId.CleanupRemoteRemoveMultFiles);
                    return false;
                }
            }

            return true;
        }

        private bool CleanupLocal(InterchangerInfo info, Session session, stringb log, out int removeCount)
        {
            removeCount = 0;
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

                try
                {
                    File.Delete(item.FullName);
                    log.AppendLine(string.Format("[{0}] Removed: {1}", GetDateNow(), item.Name));
                    removeCount++;
                }
                catch (IOException)
                {
                    string evmsg = string.Format("Could not remove the file '{0}' because it is in use", item.Name);
                    log.AppendLine(string.Format("[{0}] " + evmsg, GetDateNow()));
                    eventLog.WriteEntry(evmsg, EventLogEntryType.Error, EventId.CleanupLocalRemoveIoError);
                    return false;
                }
                catch (UnauthorizedAccessException)
                {
                    string evmsg = string.Format("Unauthorized to remove the file '{0}'", item.Name);
                    log.AppendLine(string.Format("[{0}] " + evmsg, GetDateNow()));
                    eventLog.WriteEntry(evmsg, EventLogEntryType.Error, EventId.CleanupLocalRemoveUnauthorized);
                    return false;
                }
                catch (Exception e)
                {
                    log.AppendLine(string.Format("[{0}] Unexpected error removing the file '{1}'", GetDateNow(), item.Name));
                    string msg = string.Format("Error removing file '{0}'\nMessage: {1}\nSource: {2}\nStack Trace: {3}",
                        item.Name, e.Message, e.Source, e.StackTrace);
                    eventLog.WriteEntry(msg, EventLogEntryType.Error, EventId.CleanupLocalRemoveUnexpectedError);
                    return false;
                }
            }

            return true;
        }
    }
}
