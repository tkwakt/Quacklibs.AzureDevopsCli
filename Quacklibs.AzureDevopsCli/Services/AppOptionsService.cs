using System.Text.Json;
using System.Text.Json.Serialization;

namespace Quacklibs.AzureDevopsCli.Services
{
    public record AppOptionKeyValue(string Name, object? Value);

    public class SettingsService
    {
        public Settings Settings;

        private static object _FileLock = new();

        private readonly JsonSerializerOptions _options = new() { DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, WriteIndented = true, };

        public SettingsService()
        {
            Load();
        }

        public string GetConfigurationFilePath()
        {
            string homeUserProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            return Path.Join(homeUserProfile, ".devops", "config.json");
        }

        void Load()

        {
            try
            {
                lock (_FileLock)
                {
                    string configurationFile = this.GetConfigurationFilePath();

                    CreateDefaultConfigFileIfNotExists(configurationFile);

                    try
                    {
                        var configFileContents = File.ReadAllText(configurationFile);
                        var settings = JsonSerializer.Deserialize<Settings>(configFileContents, _options);
                        Settings = settings;
                    }
                    catch (JsonException ex)
                    {
                        Console.WriteLine(ex);
                        Console.WriteLine("Invalid config file. It will be deleted. Please try again");
                        Delete();
                    }
                    finally
                    {
                        if (Settings == null)
                            Settings = new Settings();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }

        private static void CreateDefaultConfigFileIfNotExists(string configurationFile)
        {
            string? configurationDirectory = Path.GetDirectoryName(configurationFile);

            if (!Directory.Exists(configurationDirectory))
            {
                Directory.CreateDirectory(configurationDirectory);
            }

            if (!File.Exists(configurationFile))
            {
                var defaultJson = JsonSerializer.Serialize(new Settings());
                File.WriteAllText(configurationFile, defaultJson);
            }
        }

        internal void Delete()
        {
            lock (_FileLock)
            {
                if (File.Exists(this.GetConfigurationFilePath()))
                {
                    File.Delete(this.GetConfigurationFilePath());
                    Console.WriteLine("Configuration deleted");
                }
                else
                {
                    Console.WriteLine($"No configuration file found to delete");
                }
            }
        }

        internal void Save(Settings updatetSettings)
        {
            string configurationFile = this.GetConfigurationFilePath();
            CreateDefaultConfigFileIfNotExists(configurationFile);

            var output = JsonSerializer.Serialize(updatetSettings, _options);

            File.WriteAllText(configurationFile, output);
        }
    }
}