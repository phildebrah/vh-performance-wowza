using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WowzaPerformanceTest
{
    public class StreamAudio
    {
        private WowzaConfiguration wowzaConfig;

        public StreamAudio(WowzaConfiguration wowzaConfiguration)
        {
            wowzaConfig = wowzaConfiguration;
        }
        public async Task ProcessRTMP(string application, string streamName, string streamAudioFile, WowzaApi wowzaApi)
        {
            var commandName = $"{wowzaConfig.WorkingDirectory}\\ffmpeg";
            var arguments = $"-re -i {streamAudioFile} -c copy -f flv {wowzaConfig.StreamEndpoint}{application}/{streamName}";
            var process = CreateProcess(commandName, arguments, wowzaConfig.WorkingDirectory);
            await RunProcessAsync(process,wowzaApi, streamName);
        }

        private ProcessStartInfo CreateProcess(string fileName, string arguments, string workingDirectory = null)
        {
            var processStartInfo = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                RedirectStandardOutput = true,
                //RedirectStandardError = true,
                //RedirectStandardInput = true,
                //CreateNoWindow = true,
                //UseShellExecute = false
            };

            if (!string.IsNullOrEmpty(workingDirectory)) processStartInfo.WorkingDirectory = workingDirectory;

            return processStartInfo;
        }

        private async Task<int> RunProcessAsync(ProcessStartInfo processStartInfo, WowzaApi wowzaApi, string streamName)
        {
            var tcs = new TaskCompletionSource<int>();
            StringBuilder output = new StringBuilder();
            StringBuilder error = new StringBuilder();
            try
            {
                //var process = Process.Start(processStartInfo);
                var process = new Process
                {
                    StartInfo = processStartInfo,
                    EnableRaisingEvents = true
                };

                //process.OutputDataReceived += (s, e) =>
                //{
                //    lock (output)
                //    {
                //        output.AppendLine(e.Data);
                //    };
                //};
                //process.ErrorDataReceived += (s, e) =>
                //{
                //    lock (error)
                //    {
                //        error.AppendLine(e.Data);
                //    };
                //};
                process.Exited += (sender, args) =>
                {
                    //Helper.WriteToFile($"ConsoleLog-{streamName}", output.ToString() + error.ToString());
                    wowzaApi.GetMonitoringAsync($"MonitoringReport-{streamName}");
                    Thread.Sleep(1000);
                    tcs.SetResult(process.ExitCode);
                    process.Dispose();
                };

                process.Start();
                //process.BeginOutputReadLine();
                //process.BeginErrorReadLine();
                //process.StandardInput.AutoFlush = true;

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                tcs.SetResult(1);
            }
            
            return await tcs.Task;
        }
    }
}
