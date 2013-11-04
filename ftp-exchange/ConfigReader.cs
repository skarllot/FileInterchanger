using System;
using System.Collections.Generic;

namespace ftp_exchange
{
    class ConfigReader : IEnumerable<ConfigReaderItem>
    {
        const string CFG_MAIN = "MAIN";
        SklLib.IO.ConfigFileReader cfgreader;
        string[] sections;
        int idxMain;

        public ConfigReader(string path)
        {
            cfgreader = new SklLib.IO.ConfigFileReader(path);
            sections = cfgreader.ReadSectionsName();
            idxMain = Array.IndexOf<string>(sections, CFG_MAIN);
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
    }
}
