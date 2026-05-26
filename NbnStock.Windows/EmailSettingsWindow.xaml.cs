using NbnStock.Core.Services;
using NbnStock.Windows.Services;
using System.Windows;

namespace NbnStock.Windows
{
    public partial class EmailSettingsWindow : Window
    {
        private readonly WindowsCredentialVault _vault = new WindowsCredentialVault();

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
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (int.TryParse(TxtPort.Text, out int port))
            {
                var newConfig = new EmailConfig
                {
                    ImapServer = TxtServer.Text.Trim(),
                    Port = port,
                    UseSsl = ChkUseSsl.IsChecked ?? true,
                    Username = TxtEmail.Text.Trim(),
                    Password = TxtPassword.Password.Trim()
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