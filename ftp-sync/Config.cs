using System;

namespace ftp_sync
{
    class Config
    {
        const string CFG_MAIN = "MAIN";
        SklLib.IO.ConfigFileReader cfgreader;
        string[] sections;

        public Config(string path)
        {
            cfgreader = new SklLib.IO.ConfigFileReader(path);
            sections = cfgreader.ReadSectionsName();
            if (sections.Length < 1 || sections[0] != CFG_MAIN)
                throw new Exception(SklLib.resExceptions.InvalidFile.Replace("%var", path));
        }

        public string Id
        {
            get
            {
                string res = null;
                try { res = cfgreader.ReadValue(CFG_MAIN, "ID"); }
                catch (Exception ex)
                {
                    if (!(ex is SklLib.IO.KeyNotFoundException) &&
                        !(ex is SklLib.IO.SectionNotFoundException))
                        throw;
                }
                return res;
            }
        }

        public int Refresh
        {
            get
            {
                string val = cfgreader.ReadValue(CFG_MAIN, "RefreshTime");
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
