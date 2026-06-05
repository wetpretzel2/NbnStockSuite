using MailKit;
using MailKit.Net.Imap;
using MailKit.Search;
using MailKit.Security;
using MimeKit;

namespace NbnStock.Core.Services;

public enum EmailProvider
{
    CustomImap, // For legacy Basic Auth (App Passwords, cPanel, generic IMAP)
    Microsoft365, // For Modern Auth (Requires OAuth Token)
    GoogleWorkspace // For Modern Auth (Requires OAuth Token)
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

        var appData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        _downloadDirectory = Path.Combine(appData, "NbnStockSuite", "PendingJobCards");

        if (!Directory.Exists(_downloadDirectory)) Directory.CreateDirectory(_downloadDirectory);
    }

    public async Task<(int Processed, int Errors)> ProcessUnreadJobCardsAsync(
        Func<string, Task> processPdfAsync,
        string freshAccessToken = null)
    {
        var processedCount = 0;
        var errorCount = 0;

        using (var client = new ImapClient())
        {
            // Connect to the specified IMAP server
            await client.ConnectAsync(_config.ImapServer, _config.Port, _config.UseSsl);

            // Determine authentication method based on the selected provider
            if (_config.ProviderType == EmailProvider.Microsoft365 ||
                _config.ProviderType == EmailProvider.GoogleWorkspace)
            {
                // Prioritize the fresh token passed from the UI over the saved config
                var tokenToUse = !string.IsNullOrEmpty(freshAccessToken) ? freshAccessToken : _config.AccessToken;

                if (string.IsNullOrEmpty(tokenToUse))
                    throw new Exception("OAuth Access Token is missing or expired. Please sign in via Settings.");

                // Authenticate securely using the valid token
                var oauth2 = new SaslMechanismOAuth2(_config.Username, tokenToUse);
                await client.AuthenticateAsync(oauth2);
            }
            else
            {
                // Legacy Basic Authentication (App Passwords)
                await client.AuthenticateAsync(_config.Username, _config.Password);
            }

            var inbox = client.Inbox;
            await inbox.OpenAsync(FolderAccess.ReadWrite);

            // Search for unread emails with "Completed Jobs" in the subject
            var query = SearchQuery.NotSeen.And(SearchQuery.SubjectContains("Completed Jobs"));
            var uids = await inbox.SearchAsync(query);

            foreach (var uid in uids)
            {
                var savedFiles = new List<string>();

                try
                {
                    var message = await inbox.GetMessageAsync(uid);

                    foreach (var attachment in message.Attachments)
                    {
                        // Ensure it is a valid PDF attachment
                        if (attachment is not MimePart part ||
                            !part.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var filePath = Path.Combine(_downloadDirectory, part.FileName);

                        using (var stream = File.Create(filePath))
                        {
                            part.Content?.DecodeTo(stream);
                        }

                        savedFiles.Add(filePath);
                    }

                    if (savedFiles.Count == 0)
                        throw new InvalidOperationException(
                            $"Completed Jobs email contains no PDF attachments. Subject: {message.Subject}");

                    foreach (var pdfPath in savedFiles) await processPdfAsync(pdfPath);

                    // Mark as read only after all PDFs on this email have processed successfully.
                    await inbox.AddFlagsAsync(uid, MessageFlags.Seen, true);
                    processedCount += savedFiles.Count;
                }
                catch (Exception ex)
                {
                    errorCount++;
                    LogSyncError(uid.ToString(), savedFiles, ex);

                    // Do not mark this email as read. Leaving it unread preserves the retry path.
                }
            }

            await client.DisconnectAsync(true);
        }

        return (processedCount, errorCount);
    }

    private static void LogSyncError(string uid, IReadOnlyCollection<string> savedFiles, Exception exception)
    {
        var errorFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "NbnStockSuite",
            "JobCardErrors");

        Directory.CreateDirectory(errorFolder);

        var errorFile = Path.Combine(errorFolder, "sync-errors.txt");
        var files = savedFiles.Count == 0 ? "No files saved" : string.Join(", ", savedFiles);

        File.AppendAllText(errorFile,
            $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Email UID: {uid}{Environment.NewLine}" +
            $"Saved PDFs: {files}{Environment.NewLine}" +
            $"{exception}{Environment.NewLine}{Environment.NewLine}");
    }
}