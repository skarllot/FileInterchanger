using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace FileInterchanger
{
    public class Service : System.ServiceProcess.ServiceBase
    {
        const string DEFAULT_CFG_FILE_NAME = "ftp-exchange.ini";
        const string DEFAULT_CREDENTIALS_CFG_FILE_NAME = "credentials.ini";
        const int DEFAULT_REFRESH = 5;
        const string EVT_SOURCE = "FtpExchange";
        const string EVT_LOG = "FtpExchange";
        const int EVTID_SERVICE_STATE = 0;
        const int EVTID_CFGFILE_ALL_SECTIONS_INVALID = 1;
        const int EVTID_CFGFILE_RELOAD_INVALID = 2;
        const int EVTID_CFGFILE_RELOADED = 3;
        const int EVTID_REFRESH_TINY = 4;
        const int EVTID_CFGFILE_LOAD_ERROR = 5;
        const int EVTID_CFGFILE_PARSE_ERROR = 7;
        const int EVTID_EVENTLOG_CREATED = 8;
        const int EVTID_CFGFILE_NOTFOUND = 9;
        const int MINUTE_TO_MILLISECONDS = 1000 * 60;

        private System.Diagnostics.EventLog eventLog;
        private System.ComponentModel.Container components = null;
        int refresh = DEFAULT_REFRESH;
        Thread svcThread;
        Thread reloadThread;
        ManualResetEvent stopEvent;
        ManualResetEvent reloadEvent;

        public Service()
        {
            // PS> Remove-EventLog <logname>
            if (System.Diagnostics.EventLog.SourceExists(EVT_SOURCE))
            {
                eventLog = new EventLog { Source = EVT_SOURCE };
                if (eventLog.Log != EVT_LOG)
                    System.Diagnostics.EventLog.DeleteEventSource(EVT_SOURCE);
            }

            bool evtExists = false;
            try { evtExists = System.Diagnostics.EventLog.SourceExists(EVT_SOURCE); }
            catch { }
            if (!evtExists)
            {
                System.Diagnostics.EventLog.CreateEventSource(EVT_SOURCE, EVT_LOG);
                eventLog.WriteEntry("Event Log created", EventLogEntryType.Information, EVTID_EVENTLOG_CREATED);
            }

            this.eventLog = new System.Diagnostics.EventLog();
            eventLog.Source = "Service";
            eventLog.Log = EVT_LOG;

            this.ServiceName = "FtpExchange";

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
                eventLog.WriteEntry(msg, EventLogEntryType.Error, EVTID_CFGFILE_NOTFOUND);
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
            eventLog.WriteEntry("FtpSync service started",
                EventLogEntryType.Information, EVTID_SERVICE_STATE);

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

            eventLog.WriteEntry("FtpSync service stopped",
                EventLogEntryType.Information, EVTID_SERVICE_STATE);
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
            ExchangeInfo[] infoArr = LoadConfiguration(config, credReader);
            if (infoArr == null)
            {
                eventLog.WriteEntry("None of configuration sections could be parsed",
                    EventLogEntryType.Error, EVTID_CFGFILE_ALL_SECTIONS_INVALID);
                return;
            }

            Exchanger exchanger = new Exchanger();

            if (config.Refresh != -1)
                refresh = config.Refresh;

            DateTime before;
            TimeSpan elapsed;
            while (!stopEvent.WaitOne(0))
            {
                before = DateTime.Now;
                foreach (ExchangeInfo item in infoArr)
                {
                    exchanger.Exchange(item);

                    if (stopEvent.WaitOne(0))
                        break;
                }

                if (reloadEvent.WaitOne(0))
                {
                    ExchangeInfo[] infoArrNew = LoadConfiguration(config, credReader);
                    if (infoArrNew == null)
                    {
                        eventLog.WriteEntry("Configuration file was changed to invalid state",
                            EventLogEntryType.Error, EVTID_CFGFILE_RELOAD_INVALID);
                    }
                    else
                    {
                        infoArr = infoArrNew;
                        if (config.Refresh != -1)
                            refresh = config.Refresh;

                        eventLog.WriteEntry("Configuration file reloaded",
                            EventLogEntryType.Information, EVTID_CFGFILE_RELOADED);
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
                        EventLogEntryType.Warning, EVTID_REFRESH_TINY);
                }

                if (stopEvent.WaitOne(waitMs))
                    break;
            }
        }

        private ExchangeInfo[] LoadConfiguration(IO.ConfigReader config, IO.CredentialsReader credReader)
        {
            if (!ValidateConfiguration(config.FileName))
            {
                eventLog.WriteEntry(string.Format("Error loading configuration file {0}", config.FileName),
                    EventLogEntryType.Error, EVTID_CFGFILE_LOAD_ERROR);
                return null;
            }
            if (!ValidateConfiguration(credReader.FileName))
            {
                eventLog.WriteEntry(string.Format("Error loading configuration file {0}", credReader.FileName),
                    EventLogEntryType.Error, EVTID_CFGFILE_LOAD_ERROR);
                return null;
            }
            config.LoadFile();
            credReader.LoadFile();

            List<ExchangeInfo> tmp = new List<ExchangeInfo>();
            foreach (IO.ConfigReaderItem item in config)
            {
                ExchangeInfo? info;
                try { info = ExchangeInfo.Parse(item, credReader); }
                catch (Exception e)
                {
                    eventLog.WriteEntry(string.Format("Error parsing {0}\nMessage: {1}",
                        item.Section, e.Message), EventLogEntryType.Error, EVTID_CFGFILE_PARSE_ERROR);
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
