using System;
using WinSCP;

namespace ftp_sync
{
    class Synchronizer
    {
        readonly string DEFAULT_ID = Environment.MachineName;
        const int DEFAULT_REFRESH = 60000;

        string id;
        int refresh;

        public Synchronizer()
        {
            id = DEFAULT_ID;
            refresh = DEFAULT_REFRESH;
        }

        public string Id { get { return id; } set { id = value; } }
        public int Refresh { get { return refresh; } set { refresh = value; } }

        public void Transfer()
        {
            SessionOptions sessionOpt = new SessionOptions();

            Session session = new Session();
            session.Open(sessionOpt);

            TransferOptions transfOpt = new TransferOptions();
            transfOpt.TransferMode = TransferMode.Binary;

            TransferOperationResult transfRes;
            transfRes = session.PutFiles("local", "remote", true, transfOpt);

            transfRes.Check();
        }
    }
}
