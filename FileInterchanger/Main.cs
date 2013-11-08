using System.ServiceProcess;

namespace FileInterchanger
{
    class MainClass
    {
        public static readonly bool DEBUG = System.Diagnostics.Debugger.IsAttached;

        public const string PROGRAM_NAME = "FileInterchanger";
        // Latest release: 
        // Major.Minor.Maintenance.Revision
        public const string PROGRAM_VERSION = "0.1.0.43";
        public const string PROGRAM_TITLE = PROGRAM_NAME + " 0.1.0";

        public static void Main(string[] args)
        {
            Service ftp = new Service();

            if (!DEBUG)
            {
                ServiceBase[] servicesToRun = new ServiceBase[] { ftp };
                System.ServiceProcess.ServiceBase.Run(servicesToRun);
            }
            else
            {
                ftp.StartDebug(args);
            }
        }
    }
}
