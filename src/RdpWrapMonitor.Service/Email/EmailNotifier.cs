using System.Security.Cryptography;
using System.Net;
using System.Net.Mail;
using System.Text;
using RdpWrapMonitor.Service.Config;

namespace RdpWrapMonitor.Service.Email;

public interface IEmailNotifier
{
    Task SendUpdateNotificationAsync(string remoteVersion, CancellationToken cancellationToken);
    Task SendErrorNotificationAsync(string errorMessage, CancellationToken cancellationToken);
}

public class EmailNotifier : IEmailNotifier
{
    private readonly ServiceConfig _config;
    private readonly ILogger<EmailNotifier> _logger;

    public EmailNotifier(ServiceConfig config, ILogger<EmailNotifier> logger)
    {
        _config = config;
        _logger = logger;
    }

    public async Task SendUpdateNotificationAsync(string remoteVersion, CancellationToken cancellationToken)
    {
        try
        {
            var appPassword = DecryptPassword(_config.EncryptedAppPassword);

            using var message = new MailMessage
            {
                From = new MailAddress(_config.GmailAddress, "RDPWrap Monitor"),
                Subject = "RDPWrap INI Updated - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                Body = GenerateUpdateEmailBody(remoteVersion),
                IsBodyHtml = true
            };
            message.To.Add(_config.RecipientEmail);

            using var smtpClient = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(_config.GmailAddress, appPassword),
                EnableSsl = true
            };

            await smtpClient.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Update notification email sent to {Recipient}", _config.RecipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send update notification email");
            throw;
        }
    }

    public async Task SendErrorNotificationAsync(string errorMessage, CancellationToken cancellationToken)
    {
        try
        {
            var appPassword = DecryptPassword(_config.EncryptedAppPassword);

            using var message = new MailMessage
            {
                From = new MailAddress(_config.GmailAddress, "RDPWrap Monitor"),
                Subject = "RDPWrap Monitor Error - " + DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
                Body = GenerateErrorEmailBody(errorMessage),
                IsBodyHtml = true
            };
            message.To.Add(_config.RecipientEmail);

            using var smtpClient = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(_config.GmailAddress, appPassword),
                EnableSsl = true
            };

            await smtpClient.SendMailAsync(message, cancellationToken);
            _logger.LogInformation("Error notification email sent to {Recipient}", _config.RecipientEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send error notification email");
        }
    }

    private static string DecryptPassword(string encryptedPassword)
    {
        var encryptedBytes = Convert.FromBase64String(encryptedPassword);
        var decryptedBytes = ProtectedData.Unprotect(
            encryptedBytes,
            null,
            DataProtectionScope.CurrentUser);
        return Encoding.UTF8.GetString(decryptedBytes);
    }

    private static string GenerateUpdateEmailBody(string remoteVersion)
    {
        return $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2 style='color: #28a745;'>✓ RDPWrap INI Updated Successfully</h2>
    <p>The rdpwrap.ini file has been updated with the latest version from GitHub.</p>
    <table style='border-collapse: collapse; margin: 20px 0;'>
        <tr>
            <td style='padding: 8px; border: 1px solid #ddd;'><strong>Remote Version (Latest)</strong></td>
            <td style='padding: 8px; border: 1px solid #ddd;'>{remoteVersion}</td>
        </tr>
        <tr>
            <td style='padding: 8px; border: 1px solid #ddd;'><strong>Update Time</strong></td>
            <td style='padding: 8px; border: 1px solid #ddd;'>{DateTime.Now:yyyy-MM-dd HH:mm:ss}</td>
        </tr>
    </table>
    <p>The Terminal Services (TermService) has been restarted automatically.</p>
    <p style='color: #666; font-size: 12px;'>This email was sent by RDPWrap Monitor Service.</p>
</body>
</html>";
    }

    private static string GenerateErrorEmailBody(string errorMessage)
    {
        return $@"
<html>
<body style='font-family: Arial, sans-serif;'>
    <h2 style='color: #dc3545;'>✗ RDPWrap Monitor Error</h2>
    <p>An error occurred while monitoring or updating RDPWrap:</p>
    <div style='background-color: #f8d7da; padding: 15px; border-radius: 5px; margin: 15px 0;'>
        <pre style='margin: 0; white-space: pre-wrap;'>{errorMessage}</pre>
    </div>
    <p><strong>Time:</strong> {DateTime.Now:yyyy-MM-dd HH:mm:ss}</p>
    <p style='color: #666; font-size: 12px;'>This email was sent by RDPWrap Monitor Service.</p>
</body>
</html>";
    }
}
