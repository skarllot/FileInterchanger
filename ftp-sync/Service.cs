using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ftp_sync
{
    public class Service : System.ServiceProcess.ServiceBase
    {
        const string DEFAULT_CFG_FILE_NAME = "ftpsync.ini";
        const string EVT_SOURCE = "FtpSync";
        const string EVT_LOG = "FtpSync";

        private System.Diagnostics.EventLog eventLog;
        private System.ComponentModel.Container components = null;
        Thread svcThread;
        bool started = false;
        bool stopping = false;

        public Service()
        {
            // PS> Remove-EventLog <logname>
            /*if (System.Diagnostics.EventLog.SourceExists(EVT_SOURCE))
            {
                eventLog = new EventLog { Source = EVT_SOURCE };
                if (eventLog.Log != EVT_LOG)
                    System.Diagnostics.EventLog.DeleteEventSource(EVT_SOURCE);
            }*/

            bool evtExists = false;
            try { evtExists = System.Diagnostics.EventLog.SourceExists(EVT_SOURCE); }
            catch { }
            if (!evtExists)
            {
                System.Diagnostics.EventLog.CreateEventSource(EVT_SOURCE, EVT_LOG);
                eventLog.WriteEntry("Event Log created", EventLogEntryType.Information);
            }

            this.eventLog = new System.Diagnostics.EventLog();
            eventLog.Source = EVT_SOURCE;
            eventLog.Log = EVT_LOG;
            this.ServiceName = "FtpSync";


            //eventLog.Source = "FtpSynchronizer";
            //eventLog.Log = "ftp-sync";
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

            string cfgFile = null;
            if (args.Length == 1)
            {
                cfgFile = args[0];
            }
            if (cfgFile == null)
            {
                string cfgDir = Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.CommonApplicationData,
                    Environment.SpecialFolderOption.None), EVT_SOURCE);
                if (!Directory.Exists(cfgDir))
                    Directory.CreateDirectory(cfgDir);
                cfgFile = Path.Combine(cfgDir, DEFAULT_CFG_FILE_NAME);
            }
            if (!File.Exists(cfgFile))
            {
                string msg = string.Format("The configuration file \"{0}\" does not exist.", cfgFile);
                eventLog.WriteEntry(msg, EventLogEntryType.Error);
                // http://msdn.microsoft.com/en-us/library/ms681384%28v=vs.85%29
                this.ExitCode = 15010;
                throw new FileNotFoundException(msg, cfgFile);
            }

            svcThread = new Thread(new ParameterizedThreadStart(StartThread));
            svcThread.Start(cfgFile);
            eventLog.WriteEntry("FtpSync service started");
        }

        /// <summary>
        /// Stop this service.
        /// </summary>
        protected override void OnStop()
        {
            base.OnStop();

            if (started && svcThread != null && svcThread.IsAlive)
            {
                stopping = true;
                svcThread.Join();
            }

            eventLog.WriteEntry("FtpSync service stoped");
        }

        /*protected override void OnContinue()
        {
            eventLog1.WriteEntry("my service is continuing in working");
        }*/

        private void StartThread(object obj)
        {
            started = true;
            string cfgpath = (string)obj;

            Config config = new Config(cfgpath);
            Synchronizer syncer = new Synchronizer();

            if (!string.IsNullOrEmpty(config.Id))
                syncer.Id = config.Id;
            if (config.Refresh != -1)
                syncer.Refresh = config.Refresh;

            while (!stopping)
            {
                foreach (ConfigSyncItem item in config)
                {
                    string storedsession = item.StoredSession;
                    bool keepfiles = item.KeepFiles;
                    string local = item.Local;
                    string remote = item.Remote;
                    string[] files = item.Files;

                    if (stopping)
                        break;
                }
                if (stopping)
                    break;

                Thread.Sleep(syncer.Refresh);
            }
        }

        internal void Start()
        {
            this.OnStart(new string[0]);
        }
    }
}
