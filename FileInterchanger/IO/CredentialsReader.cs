using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FileInterchanger.IO
{
    class CredentialsReader : ConfigReaderBase, IEnumerable<CredentialItemReader>
    {
        public CredentialsReader(string path)
        {
            base.filename = path;
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
                name = name.ToLower();
                int idx = -1;
                for (int i = 0; i < sections.Length; i++)
                {
                    if (sections[i].ToLower() == name)
                    {
                        idx = i;
                        continue;
                    }
                }
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

        new public void LoadFile()
        {
            base.LoadFile();
        }
    }
}
