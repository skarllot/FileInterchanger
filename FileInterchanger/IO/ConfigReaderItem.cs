// ConfigReaderItem.cs
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
using System.Linq;
using System.Text;

namespace FileInterchanger.IO
{
    class ConfigReaderItem : ConfigReaderBase
    {
        public ConfigReaderItem(SklLib.IO.ConfigFileReader reader, string section)
        {
            this.cfgreader = reader;
            this.sections = new string[] { section };
        }

        public string BackupFolder { get { return GetString(sections[0], "BackupFolder"); } }
        public string Cleanup { get { return GetString(sections[0], "Cleanup"); } }
        public bool CleanupTarget { get { return GetBoolean(sections[0], "CleanupTarget"); } }
        public bool DisableSkipEmpty { get { return GetBoolean(sections[0], "DisableSkipEmpty"); } }
        public string Files { get { return GetString(sections[0], "Files"); } }
        public string Fingerprint { get { return GetString(sections[0], "Fingerprint"); } }
        public string FtpCredential { get { return GetString(sections[0], "FtpCredential"); } }
        public string FtpSecure { get { return GetString(sections[0], "FtpSecure"); } }
        public string HostName { get { return GetString(sections[0], "HostName"); } }
        public string Local { get { return GetString(sections[0], "Local"); } }
        public bool Move { get { return GetBoolean(sections[0], "Move"); } }
        public string NetworkCredential { get { return GetString(sections[0], "NetworkCredential"); } }
        public string Protocol { get { return GetString(sections[0], "Protocol"); } }
        public string Remote { get { return GetString(sections[0], "Remote"); } }
        public string Section { get { return sections[0]; } }
        public string SyncTarget { get { return GetString(sections[0], "SyncTarget"); } }
        public string TimeFilter { get { return GetString(sections[0], "TimeFilter"); } }
    }
}
