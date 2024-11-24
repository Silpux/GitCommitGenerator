using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GitCommitGenerator.Classes
{
    public class ProcessManager : IDisposable
    {
        private Process gitProcess;
        private StreamWriter inputWriter;
        private StreamReader outputReader;
        private StreamReader errorReader;

        public ProcessManager(string folderPath)
        {
            gitProcess = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    RedirectStandardInput = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = folderPath
                }
            };

            gitProcess.Start();

            inputWriter = gitProcess.StandardInput;
            outputReader = gitProcess.StandardOutput;
            errorReader = gitProcess.StandardError;

            outputReader.ReadLine();

        }


        public void RunCommand(string command)
        {
            if (gitProcess == null || gitProcess.HasExited)
                throw new InvalidOperationException("Git process is not running.");

            string marker = "Command finished";
            command += $" && echo. && echo {marker}";

            inputWriter.WriteLine(command);
            inputWriter.Flush();

            string line;
            while ((line = outputReader.ReadLine()!) != null)
            {
                //Debug.WriteLine($"Line: " + line);
                if (line.Trim() == marker)
                {
                    break;
                }
            }

        }

        public void Dispose()
        {
            if (!gitProcess.HasExited)
            {
                inputWriter.WriteLine("exit");
                gitProcess.WaitForExit();
            }

            inputWriter?.Dispose();
            outputReader?.Dispose();
            errorReader?.Dispose();
            gitProcess?.Dispose();
        }
    }
}
