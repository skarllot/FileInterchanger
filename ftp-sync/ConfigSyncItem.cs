using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ftp_sync
{
    class ConfigSyncItem
    {
        SklLib.IO.ConfigFileReader cfgreader;

        public ConfigSyncItem(SklLib.IO.ConfigFileReader reader)
        {
            this.cfgreader = reader;
        }
    }
}
