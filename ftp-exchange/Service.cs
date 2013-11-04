using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace ftp_exchange
{
    public class Service : System.ServiceProcess.ServiceBase
    {
        const string DEFAULT_CFG_FILE_NAME = "ftp-exchange.ini";
        const string DEFAULT_CREDENTIALS_CFG_FILE_NAME = "credentials.ini";
        const int DEFAULT_REFRESH = 5;
        const string EVT_SOURCE = "FtpExchange";
        const string EVT_LOG = "FtpExchange";
        const int MINUTE_TO_MILLISECONDS = 1000 * 60;

        private System.Diagnostics.EventLog eventLog;
        private System.ComponentModel.Container components = null;
        int refresh = DEFAULT_REFRESH;
        Thread svcThread;
        ManualResetEvent stopEvent;

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
                eventLog.WriteEntry("Event Log created", EventLogEntryType.Information);
            }

            this.eventLog = new System.Diagnostics.EventLog();
            eventLog.Source = "Service";
            eventLog.Log = EVT_LOG;
            this.ServiceName = "FtpExchange";

            stopEvent = new ManualResetEvent(true);
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
                eventLog.WriteEntry(msg, EventLogEntryType.Error);
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

            string dir = null;
            if (args.Length == 1)
                dir = args[0];

            string cfgFile = GetConfigFileFullName(dir, DEFAULT_CFG_FILE_NAME);
            string credentialFile = GetConfigFileFullName(dir, DEFAULT_CREDENTIALS_CFG_FILE_NAME);

            svcThread = new Thread(new ParameterizedThreadStart(StartThread));
            svcThread.Start(new string[] { cfgFile, credentialFile });
            eventLog.WriteEntry("FtpSync service started");
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
            }

            eventLog.WriteEntry("FtpSync service stopped");
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

            System.Diagnostics.EventLog evlogTransf = new System.Diagnostics.EventLog();
            evlogTransf.Source = "Transfer";
            evlogTransf.Log = EVT_LOG;

            IO.ConfigReader config = new IO.ConfigReader(cfgpath);
            IO.CredentialsReader credReader = new IO.CredentialsReader(credpath);
            Exchanger exchanger = new Exchanger();
            exchanger.EventLog = evlogTransf;

            if (config.Refresh != -1)
                refresh = config.Refresh;

            DateTime before;
            while (!stopEvent.WaitOne(0))
            {
                before = DateTime.Now;
                foreach (IO.ConfigReaderItem item in config)
                {
                    ExchangeInfo? info;
                    try { info = ExchangeInfo.Parse(item, credReader); }
                    catch (Exception e)
                    {
                        evlogTransf.WriteEntry(e.Message, EventLogEntryType.Error);
                        info = null;
                    }

                    if (info.HasValue)
                        exchanger.Exchange(info.Value);

                    if (stopEvent.WaitOne(0))
                        break;
                }

                int elapsedMs = (int)Math.Ceiling((DateTime.Now - before).TotalMilliseconds);
                if (stopEvent.WaitOne(refresh * MINUTE_TO_MILLISECONDS - elapsedMs))
                    break;
            }
        }

        internal void Start()
        {
            this.OnStart(new string[0]);
        }
    }
}
