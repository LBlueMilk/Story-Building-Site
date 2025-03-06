using System.Net.Mail;
using System.Net;

namespace BackendAPI.Services
{
    // 定義發送郵件的接口
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;

        public EmailService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendAsync(string to, string subject, string body)
        {
            string smtpServer = _config["Email:SmtpServer"] ?? throw new InvalidOperationException("SMTP 伺服器未設定");
            int smtpPort = int.Parse(_config["Email:SmtpPort"] ?? "587");
            string smtpUser = _config["Email:SmtpUser"] ?? throw new InvalidOperationException("SMTP 使用者未設定");
            string smtpPass = _config["Email:SmtpPass"] ?? throw new InvalidOperationException("SMTP 密碼未設定");

            using var client = new SmtpClient(smtpServer, smtpPort)
            {
                Credentials = new NetworkCredential(smtpUser, smtpPass),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(smtpUser),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            mailMessage.To.Add(to);
            await client.SendMailAsync(mailMessage);
        }
    }
}
