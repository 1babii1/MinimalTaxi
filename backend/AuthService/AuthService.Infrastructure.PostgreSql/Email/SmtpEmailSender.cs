using System.Net;
using System.Net.Mail;
using AuthService.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthService.Infrastructure.PostgreSql.Email;

public class SmtpEmailSender(
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken cancellationToken = default)
    {
        var emailOptions = options.Value;

        if (string.IsNullOrWhiteSpace(emailOptions.User) || string.IsNullOrWhiteSpace(emailOptions.Pass))
        {
            logger.LogWarning("SMTP credentials are missing. Email to {Email} was not sent.", toEmail);
            return;
        }

        if (!MailAddress.TryCreate(toEmail, out var recipientAddress))
        {
            logger.LogWarning("Recipient email has invalid format: {Email}", toEmail);
            return;
        }

        var senderAddressRaw = emailOptions.User;
        var senderDisplayName = emailOptions.FromName;

        if (!string.IsNullOrWhiteSpace(emailOptions.From))
        {
            if (MailAddress.TryCreate(emailOptions.From, out _))
            {
                senderAddressRaw = emailOptions.From;
            }
            else
            {
                senderDisplayName = emailOptions.From;
            }
        }

        if (!MailAddress.TryCreate(senderAddressRaw, out var senderAddress))
        {
            logger.LogWarning("SMTP sender address is invalid: {Address}", senderAddressRaw);
            return;
        }

        using var client = new SmtpClient(emailOptions.Host, emailOptions.Port)
        {
            EnableSsl = emailOptions.EnableSsl,
            Credentials = new NetworkCredential(emailOptions.User, emailOptions.Pass)
        };

        using var message = new MailMessage
        {
            From = string.IsNullOrWhiteSpace(senderDisplayName)
                ? senderAddress
                : new MailAddress(senderAddress.Address, senderDisplayName),
            Subject = subject,
            Body = htmlBody,
            IsBodyHtml = true
        };

        message.To.Add(recipientAddress);

        try
        {
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to send email to {Email}", toEmail);
        }
    }
}
