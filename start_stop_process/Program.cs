using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace start_stop_process
{
    class Program
    {
        public static bool isprocess = false;
        private static readonly string ProcessName = ConfigurationManager.AppSettings["Process"].ToString();
        private static readonly string StartProcess = ConfigurationManager.AppSettings["Path"].ToString();
        static void Main(string[] args)
        {

            Process[] processlist = Process.GetProcesses();
            foreach (Process theprocess in processlist)
            {
                if (theprocess.ProcessName.Contains(ProcessName))
                {
                    isprocess = true;
                }
            }

            if (isprocess == false)
            {
                using (Process myProcess = new Process())
                {
                    myProcess.StartInfo.UseShellExecute = false;
                    // You can start any process, HelloWorld is a do-nothing example.
                    myProcess.StartInfo.FileName = StartProcess;
                    myProcess.StartInfo.CreateNoWindow = true;
                    myProcess.Start();
                }
            }

        }
    }
}
