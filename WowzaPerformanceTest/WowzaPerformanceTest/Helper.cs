using Microsoft.Extensions.Configuration;
using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace WowzaPerformanceTest
{
    public static class Helper
    {
        public static WowzaConfiguration GetWowzaConfiguration()
        {
            var config = new ConfigurationBuilder()
                            .AddJsonFile("appsettings.json")
                            .Build()
                            .GetSection("WowzaConfiguration")
                            .Get<WowzaConfiguration>();

            config.RestEndpoint = config.RestEndpoint.Replace("{WowzaServer}", config.WowzaServer);
            config.StreamEndpoint = config.StreamEndpoint.Replace("{WowzaServer}", config.WowzaServer);

            return config;
        }

        public static bool ValidateSpecialCharacters(string text, string param)
        {
            var regexItem = new Regex("^[a-zA-Z0-9 ]*$");
            var result = regexItem.IsMatch(text);

            if (!result) Console.WriteLine($"Please ensure {param} contains only alphanumeric characters!");
            return result;
        }

        public static bool ValidateFile(string fileName,string workingDirectory = "")
        {
            if (!File.Exists(fileName) && !File.Exists($"{workingDirectory}\\{fileName}"))
            {
                Console.WriteLine($"Please verify input audio file {fileName} exists under path specified!");
                return false;
            }

            return true;
        }

        public static bool IsPrefix(string arg)
        {
            return PrefixPosition(arg) > 0;
        }

        public static string RemovePrefix(string arg)
        {
            var len = PrefixPosition(arg);
            return (len > 0) ? arg.Substring(len, arg.Length - len).ToLower(): arg.ToLower();
        }

        private static int PrefixPosition(string arg)
        {
            return arg.StartsWith("--") ? 2 : arg.StartsWith("-") ? 1 : 0;
        }

        public static void PrintReadMe()
        {
            try
            {
                StreamReader sr = new StreamReader("readme.txt");

                var line = sr.ReadLine();

                while (line != null)
                {
                    Console.WriteLine(line);
                    line = sr.ReadLine();
                }

                sr.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception: " + e.Message);
            }

        }

        public static void PrintToFile(object obj,string fileName, string serverName)
        {
            if (obj == null) return;

            StringBuilder fileContent = new StringBuilder();

            fileContent.AppendLine($"ServerName : {serverName}");

            foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(obj))
            {
                string name = descriptor.Name;
                object value = descriptor.GetValue(obj);
                fileContent.AppendLine($"{name} : {value}");
            }

            
            WriteToFile(fileName, fileContent.ToString());
        }

        public static void WriteToFile(string fileName,string fileContent)
        {
            fileName += $"-{DateTime.Now.ToString("dd-MM-yyyy-hh-mm-ss-tt", CultureInfo.InvariantCulture)}.txt";
            var filePath = new FileInfo($"Logs\\{fileName}");
            filePath.Directory.Create();

            File.WriteAllText(filePath.FullName, fileContent);
        }
    }
}
