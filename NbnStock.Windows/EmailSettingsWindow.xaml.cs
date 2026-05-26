using Microsoft.Identity.Client;
using NbnStock.Core.Services;
using NbnStock.Windows.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace NbnStock.Windows
{
    public partial class EmailSettingsWindow : Window
    {
        private readonly WindowsCredentialVault _vault = new WindowsCredentialVault();
        private string _currentAccessToken = string.Empty;

        // --- DROP YOUR AZURE IDs HERE BEFORE BUILDING ---
        private const string ClientId = "PASTE_YOUR_CLIENT_ID_HERE";
        private const string TenantId = "PASTE_YOUR_TENANT_ID_HERE";

        public EmailSettingsWindow()
        {
            InitializeComponent();
            LoadCurrentSettings();
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

                if (config.ProviderType == EmailProvider.Microsoft365)
                {
                    ComboProvider.SelectedIndex = 0;
                    if (!string.IsNullOrEmpty(_currentAccessToken))
                    {
                        TxtTokenStatus.Text = "Token Loaded from Vault";
                        TxtTokenStatus.Foreground = new SolidColorBrush(Colors.Green);
                    }
                }
                else
                {
                    ComboProvider.SelectedIndex = 1;
                }
            }
            else
            {
                ComboProvider.SelectedIndex = 0;
            }
        }

        private void ComboProvider_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ComboProvider.SelectedItem is ComboBoxItem selectedItem)
            {
                string tag = selectedItem.Tag.ToString();

                if (tag == "Microsoft365")
                {
                    PanelModernAuth.Visibility = Visibility.Visible;
                    PanelBasicAuth.Visibility = Visibility.Collapsed;

                    // Auto-fill Microsoft IMAP settings
                    if (string.IsNullOrEmpty(TxtServer.Text) || TxtServer.Text.Contains("google") || TxtServer.Text.Contains("gmail"))
                    {
                        TxtServer.Text = "outlook.office365.com";
                        TxtPort.Text = "993";
                        ChkUseSsl.IsChecked = true;
                    }
                }
                else
                {
                    PanelModernAuth.Visibility = Visibility.Collapsed;
                    PanelBasicAuth.Visibility = Visibility.Visible;
                }
            }
        }

        private async void BtnSignInM365_Click(object sender, RoutedEventArgs e)
        {
            if (ClientId.Contains("PASTE_YOUR"))
            {
                MessageBox.Show("Please paste your Client ID and Tenant ID into the code before attempting to sign in.", "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnSignInM365.IsEnabled = false;
            BtnSignInM365.Content = "Authenticating...";

            try
            {
                var pca = PublicClientApplicationBuilder.Create(ClientId)
                    .WithAuthority(AzureCloudInstance.AzurePublic, TenantId)
                    .WithDefaultRedirectUri() // Tells Azure to route the token back to this app
                    .Build();

                // The exact scope required for Exchange Online IMAP access
                string[] scopes = new string[] { "https://outlook.office.com/IMAP.AccessAsUser.All", "offline_access" };

                // This triggers the official Microsoft login prompt
                var result = await pca.AcquireTokenInteractive(scopes).ExecuteAsync();

                _currentAccessToken = result.AccessToken;
                TxtEmail.Text = result.Account.Username; // Auto-populates your email address

                TxtTokenStatus.Text = "Successfully Authenticated!";
                TxtTokenStatus.Foreground = new SolidColorBrush(Colors.Green);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to acquire token: {ex.Message}", "Authentication Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                BtnSignInM365.IsEnabled = true;
                BtnSignInM365.Content = "Sign in with Microsoft";
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtPort.Text, out int port))
            {
                var provider = ComboProvider.SelectedIndex == 0 ? EmailProvider.Microsoft365 : EmailProvider.CustomImap;

                if (provider == EmailProvider.Microsoft365 && string.IsNullOrEmpty(_currentAccessToken))
                {
                    MessageBox.Show("Please click 'Sign in with Microsoft' to authenticate before saving.", "Authentication Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var newConfig = new EmailConfig
                {
                    ProviderType = provider,
                    ImapServer = TxtServer.Text.Trim(),
                    Port = port,
                    UseSsl = ChkUseSsl.IsChecked ?? true,
                    Username = TxtEmail.Text.Trim(),
                    Password = TxtPassword.Password.Trim(),
                    AccessToken = _currentAccessToken
                };

                ConfigManager.SaveEmailConfig(newConfig, _vault);
                MessageBox.Show("Email settings saved securely.", "Settings Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Please enter a valid numeric Port.", "Invalid Port", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}