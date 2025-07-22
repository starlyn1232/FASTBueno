using FASTBueno.Classes;
using FASTBueno.Utilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using static FASTBueno.Utilities.CMDUtils;
using static FASTBueno.Utilities.Constants;
using static FASTBueno.Utilities.Debugging;

namespace FASTBueno
{
    public class Fastboot
    {
        public enum FastbootMode
        {
            Normal,
            Fastboot
        }

        // Attributes
        public string SN { get; set; } = string.Empty;
        public List<Property> Properties { get; set; } = new List<Property>();

        // Constructor
        public Fastboot(string sn = "")
        {
            SN = sn;

            if (!string.IsNullOrEmpty(SN))
            {
                Aux($"Fastboot initialized with serial number: {SN}");
            }
            else
            {
                Aux("Fastboot initialized without serial number.");
            }
        }

        // Methods
        public string Command(string arg, int timer = 10000)
        {
            return ParsedCMD(arg, timer);
        }

        public string OEMCommand(string arg, int timer = 10000)
        {
            return ParsedCMD($"oem {arg}", timer);
        }

        private string ParsedCMD(string arg, int timer = 10000)
        {
            // Specify the serial number
            if (!string.IsNullOrEmpty(SN))
            {
                arg = $"-s {SN} {arg}";
            }

            var result = FastCMD(arg, SN, timer);

            if (result.Contains(FAST_ERR_UNKNOWN_COMMAND))
                throw new FastException("Unknown command detected.");
            else if (result.Contains(FAST_ERR))
                throw new FastException("Fastboot command failed: " + result.Substring(result.IndexOf(FAST_ERR) + FAST_ERR.Length).TrimEnd(')'));

            if (result.Contains(FAST_COMPLETED))
                result = result.Substring(0, result.IndexOf(FAST_COMPLETED));

            return result;
        }

        public bool ReadProperties()
        {
            // Run fastboot getvar all command
            string output = ParsedCMD("getvar all");
            var sr = new StringReader(output);
            var line = string.Empty;
            var cnt = 0;
            Properties.Clear();
            while ((line = sr.ReadLine()) != null)
            {
                if (!line.Contains(FAST_GETVAR_HEADER))
                    continue;

                // Remove the header from the line
                line = line.Substring(FAST_GETVAR_HEADER.Length);

                // Use regex to splie value and key from, for example, "partition-size:apdp: 0x40000"
                var parts = line.Split(new[] { ':' }, 2);
                if (parts.Length < 2)
                    continue;
                // Trim spaces and check for empty keys or values
                var key = parts[0].Trim();
                var value = parts[1].Trim();
                // Skip empty keys or values
                if (string.IsNullOrEmpty(key) || string.IsNullOrEmpty(value))
                    continue;
                // Create a new property and add it to the list
                Properties.Add(new Property(key, value));
                cnt++;
            }
            return cnt > 0;
        }

        public string GetProperty(string name)
        {
            if (Properties.Count == 0)
            {
                if (!ReadProperties())
                    return string.Empty;
            }

            // Find the property with the specified name
            var prop = Properties.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            // Return the value if found, otherwise return null
            return prop?.Value;
        }

        private int OkayCounter(string result)
        {
            // Count the number of occurrences of "OKAY" in the result
            return result.Split(new[] { FAST_OKAY }, StringSplitOptions.None).Length - 1;
        }

        public bool Flash(string partitionName, string filePath)
        {
            var result = Command($"flash {partitionName} \"{filePath}\"");

            return OkayCounter(result) == 2;
        }

        public bool Erase(string partitionName)
        {
            var result = Command($"erase {partitionName}");

            return OkayCounter(result) == 1;
        }

        public bool Reboot(FastbootMode mode)
        {
            var cmd = string.Empty;

            if (mode == FastbootMode.Fastboot)
            {
                cmd = "reboot-bootloader";
            }
            else if (mode == FastbootMode.Normal)
            {
                cmd = "reboot";
            }

            var result = Command(cmd);

            return OkayCounter(result) == 1;
        }

        public bool Reboot()
        {
            return Reboot(FastbootMode.Normal);
        }

        // Static methods

        // Detect connected devices
        public static List<string> DetectDevices(int tries = 60, int delayMs = 1000)
        {
            var devices = new List<string>();

            for (int attempt = 0; attempt < tries; attempt++)
            {
                string output = string.Empty;
                try
                {
                    output = FastCMD("devices");
                }
                catch /*(Exception ex)*/
                {
                    // Optionally log the exception here
                    // Aux($"DetectDevices attempt {attempt + 1} failed: {ex.Message}");
                }

                if (!string.IsNullOrEmpty(output))
                {
                    using (var sr = new StringReader(output))
                    {
                        string line;
                        while ((line = sr.ReadLine()) != null)
                        {
                            if (!string.IsNullOrWhiteSpace(line) && line.Contains('\t'))
                            {
                                devices.Add(line.Substring(0, line.IndexOf('\t')).Trim());
                              }
                            }
                        }
                    }

                if (devices.Count > 0)
                    break;

                if (attempt < tries - 1)
                    Generic.Wait(delayMs);
            }

            return devices;
        }

        // Core fastboot command execution
        public static string FastCMD(string arg, string sn = "", int timer = 0)
        {
            using (var pro = InitProcess(FAST_EXE, arg))
            {
                var stdOutput = new StringBuilder();
                var errOutput = new StringBuilder();

                var stop = new Stopwatch();
                var lastLine = string.Empty;
                var checkDevice = arg != "devices";
                var verifyDevice = string.IsNullOrEmpty(sn) ?
                    "< waiting for any device >" :
                    $"< waiting for {sn}>";

                stop.Start();

                pro.OutputDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        stdOutput.AppendLine(e.Data);
                    }
                };

                pro.ErrorDataReceived += (sender, e) =>
                {
                    if (e.Data != null)
                    {
                        if (checkDevice && !string.IsNullOrWhiteSpace(e.Data))
                            lastLine = e.Data;

                        errOutput.AppendLine(e.Data);
                    }
                };

                try
                {
                    pro.Start();
                    pro.BeginOutputReadLine();
                    pro.BeginErrorReadLine();

                    // Wait for output to stabilize
                    Generic.Wait(800);

                    // Check if the device is connected (3 seconds timeout)
                    while (checkDevice && 
                        lastLine == verifyDevice && 
                        stop.ElapsedMilliseconds < 3000)
                    {
                        Generic.Wait(100);
                    }

                    stop.Stop();

                    if (lastLine == verifyDevice)
                    {
                        try { pro.Kill(); } catch { }
                        throw new FastException("No device connected or detected.");
                    }

                    checkDevice = false;

                    if (timer > 0)
                    {
                        if (!pro.WaitForExit(timer))
                        {
                            try { pro.Kill(); } catch { }
                            throw new TimeoutException("Fastboot command timed out.");
                        }
                    }
                    else
                    {
                        pro.WaitForExit();
                    }
                }
                catch (Exception ex)
                {
                    throw new FastException("Fastboot process failed: \n\n" +
                        ex.Message, ex);
                }

                var std = stdOutput.ToString();
                var err = errOutput.ToString();

                // Prefer standard output, but include error output if present
                if (!string.IsNullOrWhiteSpace(err))
                    return std + Environment.NewLine + err;

                return std;
            }
        }
    }
}
