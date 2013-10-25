using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ftp_exchange
{
    class MainClass
    {
        public static readonly bool DEBUG = System.Diagnostics.Debugger.IsAttached;

        public static void Main(string[] args)
        {
            Service ftp = new Service();

            if (!DEBUG)
            {
                System.ServiceProcess.ServiceBase[] ServicesToRun;

                // More than one user Service may run within the same process. To add
                // another service to this process, change the following line to
                // create a second service object. For example,
                //
                //   ServicesToRun = New System.ServiceProcess.ServiceBase[] {new Service1(), new MySecondUserService()};
                //
                ServicesToRun = new System.ServiceProcess.ServiceBase[] { ftp };

                System.ServiceProcess.ServiceBase.Run(ServicesToRun);
            }
            else
            {
                ftp.Start();
            }
        }
    }
}
