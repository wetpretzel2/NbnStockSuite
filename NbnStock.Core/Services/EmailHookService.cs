using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MimeKit;

namespace NbnStock.Core.Services
{
    // This is the missing model causing 90% of your errors!
    public class EmailConfig
    {
        public string ImapServer { get; set; }
        public int Port { get; set; }
        public bool UseSsl { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
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
                await client.ConnectAsync(_config.ImapServer, _config.Port, _config.UseSsl);
                await client.AuthenticateAsync(_config.Username, _config.Password);

                var inbox = client.Inbox;
                await inbox.OpenAsync(FolderAccess.ReadWrite);

                var query = SearchQuery.NotSeen.And(SearchQuery.SubjectContains("Work Order"));

                var uids = await inbox.SearchAsync(query);

                foreach (var uid in uids)
                {
                    var message = await inbox.GetMessageAsync(uid);

                    foreach (var attachment in message.Attachments)
                    {
                        if (attachment is MimePart part && part.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                        {
                            string filePath = Path.Combine(_downloadDirectory, part.FileName);

                            using (var stream = File.Create(filePath))
                            {
                                if (part.Content != null)
                                {
                                    part.Content.DecodeTo(stream);
                                }
                            }
                            downloadedFiles.Add(filePath);
                        }
                    }

                    await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true);
                }

                await client.DisconnectAsync(true);
            }

            return downloadedFiles;
        }
    }
}