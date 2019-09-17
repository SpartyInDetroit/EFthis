using System;
using System.IO;
using Newtonsoft.Json;

namespace EFthis
{
    public class ConfigurationManager
    {
        public void Save(string connectionString, string schema)
        {
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var configPath = Path.Combine(homePath, ".efthis");

            var configuration = new Configuration
            {
                ConnectionString = connectionString,
                Schema = schema
            };

            var json = JsonConvert.SerializeObject(configuration);
            File.WriteAllText(configPath, json);
        }

        public Configuration Retrieve()
        {
            var homePath = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

            var configPath = Path.Combine(homePath, ".efthis");

            if (!File.Exists(configPath))
            {
                return null;
            }

            var configFile = File.ReadAllText(configPath);
            return JsonConvert.DeserializeObject<Configuration>(configFile);
        }
    }
}
