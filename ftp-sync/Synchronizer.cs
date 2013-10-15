using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ftp_sync
{
    class Synchronizer
    {
        readonly string DEFAULT_ID = Environment.MachineName;

        string id;

        public Synchronizer()
        {
            id = DEFAULT_ID;
        }

        public string Id { get { return id; } set { id = value; } }
    }
}
