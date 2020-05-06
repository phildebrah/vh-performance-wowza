using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace WowzaPerformanceTest
{
    class Program
    {
        private static WowzaConfiguration wowzaConfiguration => Helper.GetWowzaConfiguration();

        private static readonly WowzaApi WowzaApi = new WowzaApi(wowzaConfiguration);

        private static readonly StreamAudio StreamAudio = new StreamAudio(wowzaConfiguration);

        private const string ArgCount = "count";
        private const string ArgStartCount = "startindex";
        private const string ArgAppName = "appname";
        private const string ArgStreamName = "streamname";
        private const string ArgStreamAudio = "streamaudio";
        private static int count = 1;
        private static int startCount = 1;
        private static string appPrefix = wowzaConfiguration.ApplicationName;
        private static string streamPrefix = wowzaConfiguration.StreamName;
        private static string streamAudioFile = wowzaConfiguration.StreamAudio;
        private static string operation = string.Empty;
        private static bool pollFlag = true;
               

        static void Main(string[] args)
        {
            if (ParseArguments(args))
            {
                Process();
            }
            else
            {
                Helper.PrintReadMe();
            }
        }  
         

        private static bool ParseArguments(string[] args)
        {
            bool readOperation = true;
            var prevFlag = string.Empty;
            var result = true;

            foreach (string arg in args)
            {
                if (readOperation)
                {
                    operation = arg;
                    readOperation = false;
                    continue;
                }

                if (Helper.IsPrefix(arg))
                {
                    switch (Helper.RemovePrefix(arg))
                    {
                        case "c":
                        case ArgCount:
                            prevFlag = ArgCount;
                            break;
                        case "si":
                        case ArgStartCount:
                            prevFlag = ArgStartCount;
                            break;
                        case "an":
                        case ArgAppName:
                            prevFlag = ArgAppName;
                            break;
                        case "sn":
                        case ArgStreamName:
                            prevFlag = ArgStreamName;
                            break;
                        case "sa":
                        case ArgStreamAudio:
                            prevFlag = ArgStreamAudio;
                            break;
                        default:
                            break;
                    }

                    continue;
                }

                switch (prevFlag)
                {
                    case ArgCount:
                        Int32.TryParse(arg, out count);
                        break;
                    case ArgStartCount:
                        Int32.TryParse(arg, out startCount);
                        break;
                    case ArgAppName:
                        result = Helper.ValidateSpecialCharacters(arg,"ApplicationName");
                        if(result) appPrefix = arg;
                        break;
                    case ArgStreamName:
                        result = Helper.ValidateSpecialCharacters(arg, "StreamName");
                        if (result) streamPrefix = arg;
                        break;
                    case ArgStreamAudio:
                        result = Helper.ValidateFile(arg, wowzaConfiguration.WorkingDirectory);
                        if(result) streamAudioFile = arg;
                        break;
                    default:
                        break;
                }

                if (!result) return result;

                prevFlag = string.Empty;
            }

            result = Helper.ValidateFile(streamAudioFile,wowzaConfiguration.WorkingDirectory);

            return result;
        }

        private static void Process()
        {
            switch (Helper.RemovePrefix(operation))
            {
                case "createandpublish":
                    Console.WriteLine("Create and publish stream(s)..");
                    ProcessMultipleStreams();
                    break;
                case "create":
                    Console.WriteLine("Creating Applications...");
                    ProcessMultipleStreams(false, true);
                    break;
                case "publish":
                    Console.WriteLine("Publishing stream(s)...");
                    ProcessMultipleStreams(true, false);
                    break;
                case "delete":
                    Console.WriteLine("Deleting Application(s)...");
                    WowzaApi.DeleteApplications(appPrefix, count, startCount);
                    break;               
                case "h":
                case "help":
                    Helper.PrintReadMe();
                    break;
                default:
                    Console.WriteLine("Invalid operation entered!");
                    Helper.PrintReadMe();
                    break;
            }

        }

        private static async void ProcessMultipleStreams(bool skipCreation = false, bool skipPublish = false)
        {

            Monitor(skipPublish,"Start");

            List<Task> taskList = new List<Task>();
            Stopwatch stopWatch = new Stopwatch();
            stopWatch.Start();

            var interval = wowzaConfiguration.PollMonitoringInterval;
            var counter = 1;

            for (var i = startCount; i <= (startCount - 1 + count); i++)
            {
                var appName = $"{appPrefix}{i}";
                var streamName = appName.Replace(appPrefix, streamPrefix);
                CreateAndProcessStream(appName, streamName,taskList, skipCreation, skipPublish);
                Thread.Sleep(1000);

                var currTime = stopWatch.Elapsed.TotalSeconds;
                if (currTime > interval)
                {
                    Monitor(skipPublish,counter.ToString());
                    counter++;
                    interval = interval * counter;
                }
            }

            Task.WaitAll(taskList.ToArray());

            Monitor(skipPublish,"End");
            stopWatch.Stop();

        }

        private static void Monitor(bool skipPublish,string eventName)
        {
            if (!skipPublish)
            {
                WowzaApi.GetMonitoringAsync($"MonitoringReport-{eventName}");
                Thread.Sleep(1000);
            }
        }
         
        private static async void CreateAndProcessStream(string applicationName, string streamName, List<Task> taskList, bool skipCreation = false, bool skipPublish = false)
        {
            var result = false;

            if(!skipCreation)
            {
                var tc = WowzaApi.CreateApplication(applicationName);
                var tu = WowzaApi.UpdateApplication(applicationName);

                Task.WaitAll(tc, tu);
                result = tu.Result && tc.Result;
            }

            if (!skipPublish &&  (result || skipCreation))
            { 
                Console.WriteLine($"Publishing to stream {streamName}");
                Console.WriteLine("----------------------------------");
                taskList.Add(StreamAudio.ProcessRTMP(applicationName, streamName, streamAudioFile, WowzaApi));
            }
        }


    }

}
