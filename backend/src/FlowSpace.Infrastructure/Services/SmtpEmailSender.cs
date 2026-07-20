using FlowSpace.Application.Interfaces;
using MailKit.Net.Smtp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using System;
using System.Threading.Tasks;

namespace FlowSpace.Infrastructure.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(IConfiguration configuration, ILogger<SmtpEmailSender> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendAsync(string to, string subject, string htmlBody)
        {
            var host = _configuration["Smtp:Host"];
            var port = _configuration.GetValue<int>("Smtp:Port");
            var user = _configuration["Smtp:User"];
            var from = _configuration["Smtp:From"];
            var enableSsl = _configuration.GetValue<bool>("Smtp:EnableSsl");
            var password = Environment.GetEnvironmentVariable("SMTP_PASSWORD");

            if (string.IsNullOrEmpty(host) || port == 0 || string.IsNullOrEmpty(user) || string.IsNullOrEmpty(from))
            {
                _logger.LogError("SMTP configuration is missing. Email not sent.");
                return;
            }
            if (string.IsNullOrEmpty(password))
            {
                _logger.LogError("SMTP_PASSWORD environment variable not set. Email not sent.");
                return;
            }

            var email = new MimeMessage();
            email.From.Add(MailboxAddress.Parse(from));
            email.To.Add(MailboxAddress.Parse(to));
            email.Subject = subject;
            email.Body = new TextPart("html") { Text = htmlBody };

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(host, port, enableSSsl);
            await smtp.AuthenticateAsync(user, password);
            await smtp.SendAsync(email);
            await smtp.DisconnectAsync(true);
        }
    }
}