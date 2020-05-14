using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WowzaPerformanceTest
{
    public  class WowzaApi
    {

        private WowzaConfiguration wowzaConfig;
        private HttpClientHandler httpClientHandler;
        private HttpClient httpClient;
        private string applicationsUrl;
        private string serverMonitoringUrl = "/v2/machine/monitoring/current";

        private ModuleConfig[] ModulesList => new ModuleConfig[]
                                        {
                                            new ModuleConfig
                                            {
                                                Name = "base",
                                                Description = "Base",
                                                Class = "com.wowza.wms.module.ModuleCore",
                                                Order = 0
                                            },
                                            new ModuleConfig
                                            {
                                                Name = "logging",
                                                Description = "Client Logging",
                                                Class = "com.wowza.wms.module.ModuleClientLogging",
                                                Order =1
                                            },
                                            new ModuleConfig
                                            {
                                                Name = "flvplayback",
                                                Description = "FLVPlayback12",
                                                Class = "com.wowza.wms.module.ModuleFLVPlayback",
                                                Order =2
                                            },
                                            new ModuleConfig
                                            {
                                                Name = "ModuleCoreSecurity",
                                                Description = "Core Security Module for Applications",
                                                Class = "com.wowza.wms.module.ModuleCoreSecurity",
                                                Order =3
                                            },
                                            new ModuleConfig
                                            {
                                                Name = "ModuleMediaWriterFileMover",
                                                Description = "ModuleMediaWriterFileMover",
                                                Class = "com.wowza.wms.module.ModuleMediaWriterFileMover",
                                                Order =3
                                            }
                                        };

        public WowzaApi(WowzaConfiguration wowzaConfiguration)
        {
            wowzaConfig = wowzaConfiguration;
            Init();
        }

        private void Init()
        {
            applicationsUrl = $"v2/servers/{wowzaConfig.ServerName}/vhosts/{wowzaConfig.HostName}/applications";

            httpClientHandler = new HttpClientHandler
            {
                Credentials = new CredentialCache
                    {
                        {
                            new Uri(wowzaConfig.RestEndpoint),
                            "Digest",
                            new NetworkCredential(wowzaConfig.Username, wowzaConfig.Password)
                        }
                    }
            };

            httpClient = new HttpClient(httpClientHandler) { BaseAddress = new Uri(wowzaConfig.RestEndpoint) };
            httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
            httpClient.DefaultRequestHeaders.Add("ContentType", "application/json");
        }

        public  async Task<bool> DeleteApp(string applicationName)
        {
            
            Console.WriteLine($"Deleting application {applicationName}");

            var response = await httpClient.DeleteAsync
                (
                    $"{applicationsUrl}/{applicationName}"
                );

            var result = await response.Content.ReadAsStringAsync();

            return response.IsSuccessStatusCode;
        }

        public  void DeleteApplications(string appPrefix,int count,int startCount=1)
        {
            for (var i = startCount; i <= (startCount - 1 + count); i++)
            {
                var applicationName = $"{appPrefix}{i}";
                _ = DeleteApp(applicationName);
                Thread.Sleep(1000);
            }
        }

        public  async Task<bool> CreateApplication(string applicationName)
        {

            var createRequest = new CreateApplicationRequest
            {
                AppType = "Live",
                Name = applicationName,
                ClientStreamReadAccess = "*",
                ClientStreamWriteAccess = "*",
                StreamRecorderRecordAllStreams = true,
                Description = "Video Hearings Application for Audio Recordings",
                StreamConfig = new StreamConfigurationConfig
                {
                    CreateStorageDir = true,
                    StreamType = wowzaConfig.StreamType,
                    StorageDir = $"{wowzaConfig.StorageDirectory}{applicationName}",
                    StorageDirExists = false,
                },
                SecurityConfig = new SecurityConfigRequest
                {
                    PublishBlockDuplicateStreamNames = true,
                    PublishIPWhiteList = "*",
                }
            };

            Console.WriteLine($"Creating application {applicationName}");

            var response = await httpClient.PostAsync
              (
                  applicationsUrl, new StringContent(SerialiseRequestToCamelCaseJson(createRequest), Encoding.UTF8, wowzaConfig.MediaType)
              );

            var result = await response.Content.ReadAsStringAsync();


            return response.IsSuccessStatusCode;
        }

        public async Task<bool> UpdateApplication(string applicationName)
        {
            var url = $"{applicationsUrl}/{applicationName}/adv";
            var azureStorageDirectory = "${com.wowza.wms.context.VHostConfigHome}/content/azurecopy/";

            var updateRequest = new ApplicationConfigAdv
            {                
                Modules = ModulesList,
                AdvancedSettings = new AdvancedSetting[] {
                                new AdvancedSetting
                                {
                                    SectionName = "Application",
                                    Section = "/Root/Application",
                                    Name = "fileMoverDestinationPath",
                                    Type = "String",
                                    Value = $"{azureStorageDirectory}{applicationName}",
                                    Documented = false,
                                    Enabled = true
                                },
                                new AdvancedSetting
                                {
                                    SectionName = "Application",
                                    Section = "/Root/Application",
                                    Name = "fileMoverDeleteOriginal",
                                    Type = "Boolean",
                                    Value = "true",
                                    Documented = false,
                                    Enabled = true
                                },
                                new AdvancedSetting
                                {
                                    SectionName = "Application",
                                    Section = "/Root/Application",
                                    Name = "fileMoverVersionFile",
                                    Type = "Boolean",
                                    Value = "true",
                                    Documented = false,
                                    Enabled = true
                                }
                }
            };  

            Console.WriteLine($"Updating application {applicationName}");

            var response = await httpClient.PostAsync
              (
                  url, new StringContent(SerialiseRequestToCamelCaseJson(updateRequest), Encoding.UTF8, wowzaConfig.MediaType)
              );

            var result = await response.Content.ReadAsStringAsync();


            return response.IsSuccessStatusCode;
        }

        public  async void GetApplicationsAsync(string applicationName)
        {
            var url = applicationsUrl;

            if (!string.IsNullOrEmpty(applicationName)) url = $"/{applicationName}";

            var response = await httpClient.GetAsync
            (
                url 
            );

            var result =  await response.Content.ReadAsStringAsync();
            Console.WriteLine(result);            
        }

        public async void GetMonitoringAsync(string fileName)
        {
            var response = await httpClient.GetAsync
            (
                serverMonitoringUrl
            );

            var result = await response.Content.ReadAsStringAsync();

            Helper.PrintToFile(JsonConvert.DeserializeObject<ServerMonitoring>(result), fileName, wowzaConfig.WowzaServer);
        }      

        private  string SerialiseRequestToCamelCaseJson(object request)
        {
            return JsonConvert.SerializeObject(request, new JsonSerializerSettings
            {
                ContractResolver = new DefaultContractResolver
                {
                    NamingStrategy = new CamelCaseNamingStrategy()
                },
                Formatting = Formatting.Indented
            });
        }

        private class CreateApplicationRequest
        {
            public string AppType { get; set; }
            public string Name { get; set; }
            public bool StreamRecorderRecordAllStreams { get; set; }
            public StreamConfigurationConfig StreamConfig { get; set; }
            public string ClientStreamWriteAccess { get; set; }
            public string ClientStreamReadAccess { get; set; }
            public string Description { get; set; }
            public SecurityConfigRequest SecurityConfig { get; set; }
            public ModulesConfig Modules { get; set; }
        }

        private class ApplicationConfigAdv
        {
            public AdvancedSetting[] AdvancedSettings { get; set; }
            public ModuleConfig[] Modules { get; set; }
        }
        private class AdvancedSetting
        {
            public string SectionName { get; set; }
            public string Section { get; set; }
            public string Name { get; set; }
            public string Type { get; set; }
            public string Value { get; set; }
            public bool Documented { get; set; }
            public bool Enabled { get; set; }

        }

        private class SecurityConfigRequest
        {
            /// <summary>
            /// Comma separated string
            /// </summary>
            public string PublishIPWhiteList { get; set; }
            public bool PublishBlockDuplicateStreamNames { get; set; }
        }

        private class StreamConfigurationConfig
        {
            public bool StorageDirExists { get; set; }
            public bool CreateStorageDir { get; set; }
            public string StreamType { get; set; }
            public string StorageDir { get; set; }

        }

        private class ModulesConfig
        {
            public ModuleConfig[] ModuleList { get; set; }
        }

        private class ModuleConfig
        {
            public string Name { get; set; }
            public string Description { get; set; }
            public string Class { get; set; }
            public int Order { get; set; }
        }

        private class ServerMonitoring
        {
            public string ServerUptime { get; set; }
            public string CpuIdle { get; set; }

            public string CpuUser { get; set; }

            public string CpuSystem { get; set; }

            public string MemoryFree { get; set; }

            public string MemoryUsed { get; set; }

            public string HeapFree { get; set; }

            public string HeapUsed { get; set; }

            public string DiskFree { get; set; }

            public string DiskUsed { get; set; }

            public string ConnectionCount { get; set; }
        }
    }
}
