using System;
using System.IO;
using System.Text.Json;

namespace NbnStock.Core.Services
{
    public static class ConfigManager
    {
        private static readonly string ConfigFilePath;

        static ConfigManager()
        {
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string configDirectory = Path.Combine(appData, "NbnStockSuite");
            ConfigFilePath = Path.Combine(configDirectory, "emailsettings.json");

            if (!Directory.Exists(configDirectory))
            {
                Directory.CreateDirectory(configDirectory);
            }
        }

        public static void SaveEmailConfig(EmailConfig config, ICredentialVault vault)
        {
            var secureConfig = new EmailConfig
            {
                ImapServer = config.ImapServer,
                Port = config.Port,
                UseSsl = config.UseSsl,
                Username = config.Username,
                Password = vault.Encrypt(config.Password) // Scramble it using whatever OS vault is provided
            };

            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(secureConfig, options));
        }

        public static EmailConfig LoadEmailConfig(ICredentialVault vault)
        {
            if (!File.Exists(ConfigFilePath)) return null;

            string json = File.ReadAllText(ConfigFilePath);
            var config = JsonSerializer.Deserialize<EmailConfig>(json);

            if (config != null && !string.IsNullOrEmpty(config.Password))
            {
                config.Password = vault.Decrypt(config.Password); // Unscramble it
            }

            return config;
        }
    }
}