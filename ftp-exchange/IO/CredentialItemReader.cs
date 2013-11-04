using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ftp_exchange.IO
{
    class CredentialItemReader : ConfigReaderBase
    {
        public CredentialItemReader(SklLib.IO.ConfigFileReader reader, string section)
        {
            this.cfgreader = reader;
            this.sections = new string[] { section };
        }

        public string Domain { get { return GetString(sections[0], "Domain"); } }
        public string UserName { get { return GetString(sections[0], "UserName"); } }
        public string Password { get { return GetString(sections[0], "Password"); } }
    }
}
