using System.Text.Json;

namespace NbnStock.Core.Services;

public static class ConfigManager
{
    private static readonly string ConfigFilePath;

    static ConfigManager()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        var configDirectory = Path.Combine(appData, "NbnStockSuite");
        ConfigFilePath = Path.Combine(configDirectory, "emailsettings.json");

        if (!Directory.Exists(configDirectory)) Directory.CreateDirectory(configDirectory);
    }

    public static void SaveEmailConfig(EmailConfig config, ICredentialVault vault)
    {
        var secureConfig = new EmailConfig
        {
            ProviderType = config.ProviderType, // <-- FIXED: Tells the app we are using Microsoft 365!
            ImapServer = config.ImapServer,
            Port = config.Port,
            UseSsl = config.UseSsl,
            Username = config.Username,
            Password = vault.Encrypt(config.Password), // Scramble it using whatever OS vault is provided
            AccessToken = config.AccessToken // <-- FIXED: Saves the static token placeholder!
        };

        var options = new JsonSerializerOptions { WriteIndented = true };
        File.WriteAllText(ConfigFilePath, JsonSerializer.Serialize(secureConfig, options));
    }

    public static EmailConfig LoadEmailConfig(ICredentialVault vault)
    {
        if (!File.Exists(ConfigFilePath)) return null;

        var json = File.ReadAllText(ConfigFilePath);
        var config = JsonSerializer.Deserialize<EmailConfig>(json);

        if (config != null && !string.IsNullOrEmpty(config.Password))
            config.Password = vault.Decrypt(config.Password); // Unscramble it

        return config;
    }
}