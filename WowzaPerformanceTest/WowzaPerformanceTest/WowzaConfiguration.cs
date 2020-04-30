using System;
using System.Collections.Generic;
using System.Text;

namespace WowzaPerformanceTest
{
    public class WowzaConfiguration
    {
        public string WowzaServer { get; set; }
        public string ServerName { get; set; }
        public string HostName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string StorageDirectory { get; set; }
        public string RestEndpoint { get; set; }
        public string StreamEndpoint { get; set; }
        public string StreamType { get; set; }
        public string MediaType { get; set; }
        public string StreamAudio { get; set; }
        public string WorkingDirectory { get; set; }
        public string StreamName { get; set; }
        public string ApplicationName { get; set; }

        public int PollMonitoringInterval { get; set; }
    }
}
