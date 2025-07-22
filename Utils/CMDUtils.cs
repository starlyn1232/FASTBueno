using System;
using System.Diagnostics;
using System.Text;

namespace FASTBueno.Utilities
{
    public static class CMDUtils
    {
        // Create Process object
        public static Process InitProcess(string exe,
            string args = "",
            bool redirectSTD = true,
            bool redirectERR = true,
            bool redirectINPUT = false)
        {
            Process newProcess = new Process();

            newProcess.StartInfo.FileName = exe;
            newProcess.StartInfo.Arguments = args;
            newProcess.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            newProcess.StartInfo.CreateNoWindow = true;
            newProcess.StartInfo.UseShellExecute = false;

            newProcess.StartInfo.RedirectStandardOutput = redirectSTD;
            newProcess.StartInfo.RedirectStandardError = redirectERR;
            newProcess.StartInfo.RedirectStandardInput = redirectINPUT;

            return newProcess;
        }
    }
}
