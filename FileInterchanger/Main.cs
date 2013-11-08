// Main.cs
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
using System.ServiceProcess;

namespace FileInterchanger
{
    class MainClass
    {
        public const string PROGRAM_NAME = "FileInterchanger";
        // Latest release: 
        // Major.Minor.Maintenance.Revision
        public const string PROGRAM_VERSION = "0.1.0.46";
        public const string PROGRAM_TITLE = PROGRAM_NAME + " 0.1";

        const int EVTID_EVENTLOG_CREATED = 8;
        const string EVT_LOG = MainClass.PROGRAM_NAME;
        const string EVT_SOURCE = MainClass.PROGRAM_NAME;
        public static readonly bool DEBUG = System.Diagnostics.Debugger.IsAttached;

        public static void Main(string[] args)
        {
            EventLog eventLog = CreateEventlog(EVT_SOURCE);
            eventLog.Dispose();
            eventLog = null;

            Service ftp = new Service();

            if (!DEBUG)
            {
                ServiceBase[] servicesToRun = new ServiceBase[] { ftp };
                System.ServiceProcess.ServiceBase.Run(servicesToRun);
            }
            else
            {
                ftp.StartDebug(args);
            }
        }

        public static EventLog CreateEventlog(string source)
        {
            EventLog result = null;

            try
            {
                // PS> Remove-EventLog <logname>
                if (EventLog.SourceExists(source))
                {
                    result = new EventLog { Source = source };
                    if (result.Log != EVT_LOG)
                    {
                        EventLog.DeleteEventSource(source);
                        result.Dispose();
                        result = null;
                    }
                }

                if (!EventLog.SourceExists(source))
                {
                    EventLog.CreateEventSource(source, EVT_LOG);
                    result = new EventLog { Source = source, Log = EVT_LOG };
                    result.WriteEntry("Event Log created",
                        EventLogEntryType.Information, EVTID_EVENTLOG_CREATED);
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format(
                    "Error creating EventLog (Source: {0} and Log: {1})", source, EVT_LOG), e);
            }

            return result;
        }
    }
}
