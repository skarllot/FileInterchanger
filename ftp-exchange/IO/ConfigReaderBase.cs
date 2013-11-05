using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ftp_exchange.IO
{
    abstract class ConfigReaderBase
    {
        protected SklLib.IO.ConfigFileReader cfgreader;
        protected string filename;
        protected string[] sections;

        public string FileName { get { return filename; } }

        protected bool GetBoolean(string section, string key)
        {
            bool result;
            string val;
            if (!cfgreader.TryReadValue(section, key, out val))
                return false;
            if (!bool.TryParse(val, out result))
                return false;
            return result;
        }

        protected string GetString(string section, string key)
        {
            string val;
            cfgreader.TryReadValue(section, key, out val);
            return val;
        }

        protected string[] GetCsvString(string section, string key)
        {
            string val;
            cfgreader.TryReadValue(section, key, out val);
            string[] list = new string[0];
            if (!string.IsNullOrWhiteSpace(val))
                list = val.Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            return list;
        }

        protected void LoadFile()
        {
            if (cfgreader == null)
                cfgreader = new SklLib.IO.ConfigFileReader(filename);

            cfgreader.ReloadFile();
            sections = cfgreader.ReadSectionsName();
        }
    }
}
