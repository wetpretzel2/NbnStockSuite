using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;

namespace NbnStock.Core.Services
{
    public enum EmailProvider
    {
        CustomImap,       // For legacy Basic Auth (App Passwords, cPanel, generic IMAP)
        Microsoft365,     // For Modern Auth (Requires OAuth Token)
        GoogleWorkspace   // For Modern Auth (Requires OAuth Token)
    }

    public class EmailConfig
    {
        // Connection Details
        public EmailProvider ProviderType { get; set; } = EmailProvider.CustomImap;
        public string ImapServer { get; set; }
        public int Port { get; set; }
        public bool UseSsl { get; set; }
        public string Username { get; set; }

        // Basic Auth (Encrypted by your Vault)
        public string Password { get; set; }

        // Modern Auth (OAuth 2.0)
        public string AccessToken { get; set; }
    }

    public class EmailHookService
    {
        private readonly EmailConfig _config;
        private readonly string _downloadDirectory;

        public EmailHookService(EmailConfig config)
        {
            _config = config;

            string appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            _downloadDirectory = Path.Combine(appData, "NbnStockSuite", "PendingJobCards");

            if (!Directory.Exists(_downloadDirectory))
            {
                Directory.CreateDirectory(_downloadDirectory);
            }
        }

        public async Task<List<string>> DownloadNewJobCardsAsync()
        {
            var downloadedFiles = new List<string>();

            using (var client = new ImapClient())
            {
                // Connect to the specified IMAP server
                await client.ConnectAsync(_config.ImapServer, _config.Port, _config.UseSsl);

                // Determine authentication method based on the selected provider
                if (_config.ProviderType == EmailProvider.Microsoft365 || _config.ProviderType == EmailProvider.GoogleWorkspace)
                {
                    // Modern OAuth2 Authentication
                    if (string.IsNullOrEmpty(_config.AccessToken))
                    {
                        throw new Exception("OAuth Access Token is missing. Please re-authenticate in Settings.");
                    }

                    var oauth2 = new SaslMechanismOAuth2(_config.Username, _config.AccessToken);
                    await client.AuthenticateAsync(oauth2);
                }
                else
                {
                    // Legacy Basic Authentication (App Passwords)
                    await client.AuthenticateAsync(_config.Username, _config.Password);
                }

                var inbox = client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadWrite);

                // Search for unread emails with "Work Order" in the subject
                var query = SearchQuery.NotSeen.And(SearchQuery.SubjectContains("Work Order"));
                var uids = await inbox.SearchAsync(query);

                foreach (var uid in uids)
                {
                    var message = await inbox.GetMessageAsync(uid);

                    foreach (var attachment in message.Attachments)
                    {
                        // Ensure it is a valid PDF attachment
                        if (attachment is MimePart part && part.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            string filePath = Path.Combine(_downloadDirectory, part.FileName);

                            using (var stream = File.Create(filePath))
                            {
                                part.Content?.DecodeTo(stream);
                            }

                            downloadedFiles.Add(filePath);
                        }
                    }

                    // Mark as read so it isn't processed again on the next sync
                    await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true);
                }

                await client.DisconnectAsync(true);
            }

            return downloadedFiles;
        }
    }
}