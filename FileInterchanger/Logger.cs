// Logger.cs
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
using System.Linq;
using System.Text;

namespace FileInterchanger
{
    class Logger
    {
        const string DEFAULT_LOG = MainClass.PROGRAM_NAME;
        const string DEFAULT_SOURCE = MainClass.PROGRAM_NAME;

        static Logger _default;
        EventLog eventLog;

        static Logger()
        {
            _default = new Logger(DEFAULT_SOURCE, DEFAULT_LOG);
        }

        public Logger(string source, string log)
        {
            eventLog = CreateEventlog(source, log);
        }

        public static Logger Default { get { return _default; } }

        public void WriteEntry(string message, EventLogEntryType type, EventId eventId)
        {
            eventLog.WriteEntry(message, type, (int)eventId);
        }

        private static EventLog CreateEventlog(string source, string log)
        {
            EventLog result = null;

            try
            {
                // PS> Remove-EventLog <logname>
                if (EventLog.SourceExists(source))
                {
                    result = new EventLog { Source = source };
                    if (result.Log != log)
                    {
                        EventLog.DeleteEventSource(source);
                        result.Dispose();
                        result = null;
                    }
                }

                if (!EventLog.SourceExists(source))
                {
                    EventLog.CreateEventSource(source, log);
                    result = new EventLog { Source = source, Log = log };
                    result.WriteEntry("Event Log created",
                        EventLogEntryType.Information, (int)EventId.EventLogCreated);
                }
            }
            catch (Exception e)
            {
                throw new Exception(string.Format(
                    "Error creating EventLog (Source: {0} and Log: {1})", source, log), e);
            }

            return result;
        }
    }
}
