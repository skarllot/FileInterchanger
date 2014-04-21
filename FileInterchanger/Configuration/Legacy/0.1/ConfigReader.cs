// ConfigReader.cs
//
// Copyright (C) 2014 Fabrício Godoy
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

namespace FileInterchanger.Legacy.v0_1.Configuration
{
    class ConfigReader : ConfigReaderBase, IEnumerable<ConfigReaderItem>
    {
        const string CFG_MAIN = "MAIN";
        public const string DEFAULT_FILENAME = "config.ini";
        int idxMain;

        public ConfigReader(string path)
        {
            base.filename = path;
        }

        public int Refresh
        {
            get
            {
                string val;
                int result;
                if (!cfgreader.TryReadValue(CFG_MAIN, "RefreshTime", out val))
                    return -1;
                if (!int.TryParse(val, out result))
                    return -1;
                return result;
            }
        }

        public ConfigReaderItem this[int index]
        {
            get
            {
                if (index >= idxMain)
                    index++;
                return new ConfigReaderItem(cfgreader, sections[index]);
            }
        }

        IEnumerator<ConfigReaderItem> IEnumerable<ConfigReaderItem>.GetEnumerator()
        {
            ConfigReaderItem[] list = new ConfigReaderItem[sections.Length - 1];
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = this[i];
            }
            return ((IEnumerable<ConfigReaderItem>)list).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<ConfigReaderItem>)this).GetEnumerator();
        }

        new public void LoadFile()
        {
            base.LoadFile();
            idxMain = Array.IndexOf<string>(sections, CFG_MAIN);
        }
    }
}
