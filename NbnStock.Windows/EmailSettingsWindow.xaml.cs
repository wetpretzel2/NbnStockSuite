using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Microsoft.Identity.Client;
using NbnStock.Core.Services;
using NbnStock.Windows.Services;

namespace NbnStock.Windows;

public partial class EmailSettingsWindow : Window
{
    private const string CacheFileName = "msal_cache.bin";

    private const string ClientId = "3688d358-a23c-4142-98ed-783ee491edb6";
    private const string TenantId = "common";
    private readonly string[] _scopes = new[] { "https://outlook.office.com/IMAP.AccessAsUser.All", "offline_access" };
    private readonly WindowsCredentialVault _vault = new();
    private string _currentAccessToken = string.Empty;

    private IPublicClientApplication _pca;

    public EmailSettingsWindow()
    {
        InitializeComponent();
        LoadCurrentSettings();
    }

    private async Task InitializeAuth()
    {
        if (_pca != null) return;

        _pca = PublicClientApplicationBuilder.Create(ClientId)
            .WithAuthority(AzureCloudInstance.AzurePublic, TenantId)
            .WithDefaultRedirectUri()
            .Build();

        var cacheDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NbnStockSuite");
        if (!Directory.Exists(cacheDir)) Directory.CreateDirectory(cacheDir);
        var cacheFilePath = Path.Combine(cacheDir, CacheFileName);

        // Wire up the token cache storage events correctly
        _pca.UserTokenCache.SetBeforeAccess(args =>
        {
            if (File.Exists(cacheFilePath))
                try
                {
                    byte[] encryptedData = File.ReadAllBytes(cacheFilePath);
                    byte[] decryptedData = System.Security.Cryptography.ProtectedData.Unprotect(
                        encryptedData,
                        null,
                        System.Security.Cryptography.DataProtectionScope.CurrentUser);

                    args.TokenCache.DeserializeMsalV3(decryptedData);
                }
                catch (Exception)
                {
                    File.Delete(cacheFilePath); // Wipe corrupted cache file safely if decryption fails
                }
        });

        _pca.UserTokenCache.SetAfterAccess(args =>
        {
            if (args.HasStateChanged)
            {
                byte[] decryptedData = args.TokenCache.SerializeMsalV3();
                byte[] encryptedData = System.Security.Cryptography.ProtectedData.Protect(
                    decryptedData,
                    null,
                    System.Security.Cryptography.DataProtectionScope.CurrentUser);

                File.WriteAllBytes(cacheFilePath, encryptedData);
            }
        });
    }

    private void LoadCurrentSettings()
    {
        var config = ConfigManager.LoadEmailConfig(_vault);
        if (config != null)
        {
            TxtServer.Text = config.ImapServer;
            TxtPort.Text = config.Port.ToString();
            ChkUseSsl.IsChecked = config.UseSsl;
            TxtEmail.Text = config.Username;
            TxtPassword.Password = config.Password;
            _currentAccessToken = config.AccessToken;

            ComboProvider.SelectedIndex = config.ProviderType == EmailProvider.Microsoft365 ? 0 : 1;

            if (config.ProviderType == EmailProvider.Microsoft365 && !string.IsNullOrEmpty(_currentAccessToken))
            {
                TxtTokenStatus.Text = "Token Loaded from Vault";
                TxtTokenStatus.Foreground = new SolidColorBrush(Colors.Green);
            }
        }
    }

    private async void BtnSignInM365_Click(object sender, RoutedEventArgs e)
    {
        await InitializeAuth();

        BtnSignInM365.IsEnabled = false;
        BtnSignInM365.Content = "Authenticating...";

        try
        {
            var result = await _pca.AcquireTokenInteractive(_scopes).ExecuteAsync();

            _currentAccessToken = result.AccessToken;
            TxtEmail.Text = result.Account.Username;

            TxtTokenStatus.Text = "Successfully Authenticated!";
            TxtTokenStatus.Foreground = new SolidColorBrush(Colors.Green);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Authentication failed: {ex.Message}", "Authentication Error", MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
        finally
        {
            BtnSignInM365.IsEnabled = true;
            BtnSignInM365.Content = "Sign in with Microsoft";
        }
    }

    // Call this public method from your background Job Card / Email Hook Sync Service
    public async Task<string> GetTokenAsync()
    {
        await InitializeAuth();
        var accounts = await _pca.GetAccountsAsync();
        try
        {
            var result = await _pca.AcquireTokenSilent(_scopes, accounts.FirstOrDefault()).ExecuteAsync();
            return result.AccessToken;
        }
        catch (MsalUiRequiredException)
        {
            return null;
        }
    }

    private void ComboProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ComboProvider.SelectedItem is ComboBoxItem item)
        {
            bool isM365 = item.Tag?.ToString() == "Microsoft365";
            PanelModernAuth.Visibility = isM365 ? Visibility.Visible : Visibility.Collapsed;
            PanelBasicAuth.Visibility = isM365 ? Visibility.Collapsed : Visibility.Visible;

            if (isM365 && (string.IsNullOrEmpty(TxtServer.Text) || TxtServer.Text.Contains("gmail")))
            {
                TxtServer.Text = "outlook.office365.com";
                TxtPort.Text = "993";
                ChkUseSsl.IsChecked = true;
            }
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        if (int.TryParse(TxtPort.Text, out int port))
        {
            var newConfig = new EmailConfig
            {
                ProviderType = ComboProvider.SelectedIndex == 0 ? EmailProvider.Microsoft365 : EmailProvider.CustomImap,
                ImapServer = TxtServer.Text.Trim(),
                Port = port,
                UseSsl = ChkUseSsl.IsChecked ?? true,
                Username = TxtEmail.Text.Trim(),

                // THE FIX: If the password box is empty, give the Vault a dummy string so it doesn't crash
                Password = string.IsNullOrWhiteSpace(TxtPassword.Password)
                    ? "M365_OAUTH_TOKEN"
                    : TxtPassword.Password.Trim(),

                AccessToken = _currentAccessToken
            };

            ConfigManager.SaveEmailConfig(newConfig, _vault);
            MessageBox.Show("Email settings saved securely.", "Settings Saved", MessageBoxButton.OK,
                MessageBoxImage.Information);
            DialogResult = true;
        }
        else
        {
            MessageBox.Show("Please enter a valid numeric Port.", "Invalid Port", MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}