using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ftp_exchange.IO
{
    class CredentialsReader : ConfigReaderBase, IEnumerable<CredentialItemReader>
    {
        public CredentialsReader(string path)
        {
            cfgreader = new SklLib.IO.ConfigFileReader(path);
            sections = cfgreader.ReadSectionsName();
        }

        public CredentialItemReader this[int index]
        {
            get
            {
                return new CredentialItemReader(cfgreader, sections[index]);
            }
        }

        public CredentialItemReader this[string name]
        {
            get
            {
                int idx = Array.IndexOf<string>(sections, name);
                if (idx == -1)
                    return null;
                return this[idx];
            }
        }

        IEnumerator<CredentialItemReader> IEnumerable<CredentialItemReader>.GetEnumerator()
        {
            CredentialItemReader[] list = new CredentialItemReader[sections.Length - 1];
            for (int i = 0; i < list.Length; i++)
            {
                list[i] = this[i];
            }
            return ((IEnumerable<CredentialItemReader>)list).GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<CredentialItemReader>)this).GetEnumerator();
        }
    }
}
