// Service.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace FileInterchanger
{
    public class Service : System.ServiceProcess.ServiceBase
    {
        const string DEFAULT_CFG_FILE_NAME = "config.ini";
        const string DEFAULT_CREDENTIALS_CFG_FILE_NAME = "credentials.ini";
        const int DEFAULT_REFRESH = 5;
        const int MINUTE_TO_MILLISECONDS = 1000 * 60;

        Logger eventLog = Logger.Default;
        System.ComponentModel.Container components = null;
        int refresh = DEFAULT_REFRESH;
        Thread svcThread;
        Thread reloadThread;
        ManualResetEvent stopEvent;
        ManualResetEvent reloadEvent;

        public Service()
        {
            this.ServiceName = MainClass.PROGRAM_NAME;

            stopEvent = new ManualResetEvent(true);
            reloadEvent = new ManualResetEvent(false);
        }

        private void ConfigWatcher(object obj)
        {
            string cfgpath = ((string[])obj)[0];
            string credpath = ((string[])obj)[1];

            DateTime dtCfg = File.GetLastWriteTime(cfgpath);
            DateTime dtCred = File.GetLastWriteTime(credpath);
            DateTime dtCfg2, dtCred2;
            while (!stopEvent.WaitOne(0))
            {
                dtCfg2 = File.GetLastWriteTime(cfgpath);
                dtCred2 = File.GetLastWriteTime(credpath);
                if (dtCfg != dtCfg2 || dtCred != dtCred2)
                {
                    dtCfg = dtCfg2;
                    dtCred = dtCred2;
                    reloadEvent.Set();
                }

                if (stopEvent.WaitOne(30000))
                    break;
            }
        }

        private string GetConfigFileFullName(string dir, string fileName)
        {
            string cfgFile = null;
            if (dir == null)
            {
                string cfgDir = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData,
                    Environment.SpecialFolderOption.None), this.ServiceName);
                if (!Directory.Exists(cfgDir))
                    Directory.CreateDirectory(cfgDir);
                cfgFile = Path.Combine(cfgDir, fileName);
            }
            else
                cfgFile = Path.Combine(dir, fileName);
            if (!File.Exists(cfgFile))
            {
                string msg = string.Format("The configuration file \"{0}\" does not exist.", cfgFile);
                eventLog.WriteEntry(msg, EventLogEntryType.Error, EventId.ConfigFileNotFound);
                // http://msdn.microsoft.com/en-us/library/ms681384%28v=vs.85%29
                this.ExitCode = 15010;
                throw new FileNotFoundException(msg, cfgFile);
            }

            return cfgFile;
        }

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (components != null)
                {
                    components.Dispose();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Set things in motion so your service can do its work.
        /// </summary>
        protected override void OnStart(string[] args)
        {
            base.OnStart(args);

            string[] cmdArgs = Environment.GetCommandLineArgs();
            if (cmdArgs.Length > 1)
            {
                args = new string[cmdArgs.Length - 1];
                Array.Copy(cmdArgs, 1, args, 0, cmdArgs.Length - 1);
            }

            string dir = null;
            if (args.Length == 1)
                dir = args[0];

            string cfgFile = GetConfigFileFullName(dir, DEFAULT_CFG_FILE_NAME);
            string credentialFile = GetConfigFileFullName(dir, DEFAULT_CREDENTIALS_CFG_FILE_NAME);

            svcThread = new Thread(new ParameterizedThreadStart(StartThread));
            svcThread.Start(new string[] { cfgFile, credentialFile });
            eventLog.WriteEntry(string.Format("{0} service started", MainClass.PROGRAM_NAME),
                EventLogEntryType.Information, EventId.ServiceStateChanged);

            reloadThread = new Thread(new ParameterizedThreadStart(ConfigWatcher));
            reloadThread.Start(new string[] { cfgFile, credentialFile });
        }

        /// <summary>
        /// Stop this service.
        /// </summary>
        protected override void OnStop()
        {
            base.OnStop();

            if (svcThread != null && svcThread.IsAlive)
            {
                stopEvent.Set();
                svcThread.Join();
                reloadThread.Join();
            }

            eventLog.WriteEntry(string.Format("{0} service stopped", MainClass.PROGRAM_NAME),
                EventLogEntryType.Information, EventId.ServiceStateChanged);
        }

        /*protected override void OnContinue()
        {
            eventLog1.WriteEntry("my service is continuing in working");
        }*/

        private void StartThread(object obj)
        {
            stopEvent.Reset();
            string cfgpath = ((string[])obj)[0];
            string credpath = ((string[])obj)[1];

            IO.ConfigReader config = new IO.ConfigReader(cfgpath);
            IO.CredentialsReader credReader = new IO.CredentialsReader(credpath);
            InterchangerInfo[] infoArr = LoadConfiguration(config, credReader);
            if (infoArr == null)
            {
                eventLog.WriteEntry("None of configuration sections could be parsed",
                    EventLogEntryType.Error, EventId.ConfigFileAllSectionsInvalid);
                return;
            }

            Interchanger exchanger = new Interchanger();

            if (config.Refresh != -1)
                refresh = config.Refresh;

            DateTime before;
            TimeSpan elapsed;
            while (!stopEvent.WaitOne(0))
            {
                before = DateTime.Now;
                foreach (InterchangerInfo item in infoArr)
                {
                    exchanger.Exchange(item);

                    if (stopEvent.WaitOne(0))
                        break;
                }

                if (reloadEvent.WaitOne(0))
                {
                    InterchangerInfo[] infoArrNew = LoadConfiguration(config, credReader);
                    if (infoArrNew == null)
                    {
                        eventLog.WriteEntry("Configuration file was changed to invalid state",
                            EventLogEntryType.Error, EventId.ConfigFileReloadInvalid);
                    }
                    else
                    {
                        infoArr = infoArrNew;
                        if (config.Refresh != -1)
                            refresh = config.Refresh;

                        eventLog.WriteEntry("Configuration file reloaded",
                            EventLogEntryType.Information, EventId.ConfigFileReloaded);
                    }
                    reloadEvent.Reset();
                }

                elapsed = DateTime.Now - before;
                int elapsedMs = (int)Math.Ceiling(elapsed.TotalMilliseconds);
                int waitMs = refresh * MINUTE_TO_MILLISECONDS - elapsedMs;
                if (waitMs < 0)
                {
                    waitMs = 0;
                    eventLog.WriteEntry(string.Format(
                        "File exchange took {0} and refresh time is set to {1}",
                        elapsed, new TimeSpan(0, refresh, 0)),
                        EventLogEntryType.Warning, EventId.ServiceInsufficientWaitTime);
                }

                if (stopEvent.WaitOne(waitMs))
                    break;
            }
        }

        private InterchangerInfo[] LoadConfiguration(IO.ConfigReader config, IO.CredentialsReader credReader)
        {
            if (!ValidateConfiguration(config.FileName))
            {
                eventLog.WriteEntry(string.Format("Error loading configuration file {0}", config.FileName),
                    EventLogEntryType.Error, EventId.ConfigFileLoadError);
                return null;
            }
            if (!ValidateConfiguration(credReader.FileName))
            {
                eventLog.WriteEntry(string.Format("Error loading configuration file {0}", credReader.FileName),
                    EventLogEntryType.Error, EventId.ConfigFileLoadError);
                return null;
            }
            config.LoadFile();
            credReader.LoadFile();

            List<InterchangerInfo> tmp = new List<InterchangerInfo>();
            foreach (IO.ConfigReaderItem item in config)
            {
                InterchangerInfo? info;
                try { info = InterchangerInfo.Parse(item, credReader); }
                catch (Exception e)
                {
                    eventLog.WriteEntry(string.Format("Error parsing {0}\nMessage: {1}",
                        item.Section, e.Message), EventLogEntryType.Error, EventId.ConfigFileParseError);
                    info = null;
                }

                if (info.HasValue)
                    tmp.Add(info.Value);
            }

            if (tmp.Count == 0)
                return null;

            return tmp.ToArray();
        }

        internal void StartDebug(string[] args)
        {
            this.OnStart(args);
        }

        private bool ValidateConfiguration(string file)
        {
            try { SklLib.IO.ConfigFileReader reader = new SklLib.IO.ConfigFileReader(file); }
            catch (FileLoadException) { return false; }
            return true;
            // return reader.IsValidFile();
        }
    }
}
