using System;

namespace ftp_sync
{
    class Config
    {
        IniFile ini;

        public Config(string path)
        {
            ini = new IniFile(path);
        }

        public string Id
        {
            get
            {
                return ini.ReadValue("MAIN", "ID");
            }
        }

        public int Refresh
        {
            get
            {
                string val = ini.ReadValue("MAIN", "RefreshTime");
                int result;
                if (!int.TryParse(val, out result))
                    result = -1;
                return result;
            }
        }

        public string[] GetSections()
        {
            string[] result = ini.GetSectionNames();
            string[] ret = new string[result.Length - 1];
            Array.Copy(result, 1, ret, 0, ret.Length);
            return ret;
        }

        public string GetStoredSession(string section)
        {
            return ini.ReadValue(section, "StoredSession");
        }

        public bool GetKeepFiles(string section)
        {
            string val = ini.ReadValue(section, "KeepFiles");
            bool result;
            if (!bool.TryParse(val, out result))
                result = true;
            return result;
        }

        public string GetLocal(string section)
        {
            return ini.ReadValue(section, "Local");
        }

        public string GetRemote(string section)
        {
            return ini.ReadValue(section, "Remote");
        }

        public string[] GetFiles(string section)
        {
            string val = ini.ReadValue(section, "Files");
            string[] ret = new string[0];
            if (!string.IsNullOrEmpty(val))
                ret = val.Split(';');
            return ret;
        }
    }
}
