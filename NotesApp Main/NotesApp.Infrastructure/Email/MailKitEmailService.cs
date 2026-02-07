using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using NotesApp.Application.Interfaces.Emails;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace NotesApp.Infrastructure.Email
{
    public class MailKitEmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public MailKitEmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            try
            {
                var host = _config["Email:SmtpHost"];
                var portString = _config["Email:SmtpPort"];
                var username = _config["Email:Username"];
                var password = _config["Email:Password"];
                var from = _config["Email:From"];
                
                // Defaults
                if(string.IsNullOrEmpty(host)) host = "smtp.gmail.com";
                
                int port = 587;
                if(!string.IsNullOrEmpty(portString)) int.TryParse(portString, out port);

                var message = new MimeMessage();
                message.From.Add(MailboxAddress.Parse(from ?? "test@example.com"));
                message.To.Add(MailboxAddress.Parse(to));
                message.Subject = subject;
                
                var bodyBuilder = new BodyBuilder { HtmlBody = body };
                message.Body = bodyBuilder.ToMessageBody();

                Console.WriteLine($"📧 Email Service: Host={host}, Port={port}, User={username}");

                using var client = new SmtpClient();
                client.ServerCertificateValidationCallback = (s, c, h, e) => true;
                client.CheckCertificateRevocation = false;

                // Explicitly use StartTls for port 587 as proven in the standalone test
                var connectionOptions = (port == 587) ? SecureSocketOptions.StartTls : SecureSocketOptions.Auto;
                
                await client.ConnectAsync(host, port, connectionOptions);
                Console.WriteLine($"✅ SMTP Connected: {host}:{port} using {connectionOptions}");
 
                 if (!string.IsNullOrEmpty(username) && !string.IsNullOrEmpty(password))
                 {
                     await client.AuthenticateAsync(username, password);
                     Console.WriteLine("✅ SMTP Authenticated successfully");
                 }
                 
                 await client.SendAsync(message);
                 Console.WriteLine($"✅ SMTP Message sent to {to}");
                 await client.DisconnectAsync(true);
 
                 Console.WriteLine($"🎉 EMAIL DISPATCH COMPLETED: {to}");
            }
            catch (Exception ex)
            {
                // 🔥 Capture ALL Exceptions to prevent API 500
                Console.WriteLine($"❌ EMAIL SENDING FAILED: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"   Inner Exception: {ex.InnerException.Message}");
                }
                Console.WriteLine(ex.ToString());
                // We do NOT throw here. We let the flow continue.
            }
        }
    }
}
