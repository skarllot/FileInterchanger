using System;
using System.Collections.Generic;

namespace ftp_sync
{
    class Config : IEnumerable<ConfigSyncItem>
    {
        const string CFG_MAIN = "MAIN";
        SklLib.IO.ConfigFileReader cfgreader;
        string[] sections;
        int idxMain;

        public Config(string path)
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

        public ConfigSyncItem this[int index]
        {
            get
            {
                if (index >= idxMain)
                    index++;
                return new ConfigSyncItem(cfgreader, sections[index]);
            }
        }

        IEnumerator<ConfigSyncItem> IEnumerable<ConfigSyncItem>.GetEnumerator()
        {
            ConfigSyncItem[] list = new ConfigSyncItem[sections.Length - 1];
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = this[i];
            }
            return ((IEnumerable<ConfigSyncItem>)list).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<ConfigSyncItem>)this).GetEnumerator();
        }
    }
}
