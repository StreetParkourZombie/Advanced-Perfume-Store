using System.Net;
using System.Net.Mail;

namespace PerfumeStore.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendVerificationEmailAsync(string toEmail, string token, string callbackUrl)
        {
            try
            {
                var emailSettings = _config.GetSection("EmailSettings");
                string? fromEmail = emailSettings["SenderEmail"];
                string? password = emailSettings["SenderPassword"];
                string? smtpServer = emailSettings["SmtpServer"];
                string? portStr = emailSettings["SmtpPort"];

                if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(smtpServer))
                {
                    _logger.LogError("Email settings are missing or incomplete");
                    throw new InvalidOperationException("Email configuration is missing");
                }

                if (string.IsNullOrEmpty(portStr) || !int.TryParse(portStr, out int port))
                {
                    _logger.LogError("Invalid SMTP port configuration");
                    throw new InvalidOperationException("Invalid SMTP port");
                }

                _logger.LogInformation($"Attempting to send verification email to: {toEmail}");
                _logger.LogInformation($"SMTP Server: {smtpServer}:{port}");
                _logger.LogInformation($"From Email: {fromEmail}");

                string subject = "Xác minh tài khoản - WebNuocHoa";
                string body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto;'>
                <h2 style='color: #d9534f;'>Welcome to WebNuocHoa!</h2>
                <p>Cảm ơn bạn đã đăng ký tài khoản tại WebNuocHoa.</p>
                <p>Vui lòng nhấn vào nút bên dưới để xác minh tài khoản của bạn:</p>
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{callbackUrl}' style='background-color: #d9534f; color: white; padding: 15px 30px; text-decoration: none; border-radius: 5px; display: inline-block;'>XÁC MINH NGAY</a>
                </div>
                <p>Hoặc copy link này vào trình duyệt:</p>
                <p style='word-break: break-all; background: #f5f5f5; padding: 10px; border-radius: 5px;'>{callbackUrl}</p>
                <p><strong>Lưu ý:</strong> Link xác thực có hiệu lực trong 24 giờ.</p>
                <hr style='margin: 30px 0; border: none; border-top: 1px solid #ddd;'>
                <p style='color: #666; font-size: 12px;'>Nếu bạn không thực hiện đăng ký này, vui lòng bỏ qua email này.</p>
            </div>";

                using (var client = new SmtpClient(smtpServer, port))
                {
                    client.Credentials = new NetworkCredential(fromEmail, password);
                    client.EnableSsl = true;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.Timeout = 30000; // 30 seconds timeout

                    var mail = new MailMessage(fromEmail, toEmail, subject, body)
                    {
                        IsBodyHtml = true
                    };

                    // Cải thiện deliverability
                    mail.Headers.Add("Message-ID", $"<{token}@webnuochoa.com>");
                    mail.Headers.Add("X-Mailer", "WebNuocHoa E-commerce");
                    mail.Headers.Add("List-Unsubscribe", "<mailto:unsubscribe@webnuochoa.com>");

                    // Set reply-to để improve reputation
                    mail.ReplyToList.Add(fromEmail);

                    await client.SendMailAsync(mail);

                    _logger.LogInformation($"Email successfully sent to: {toEmail}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send verification email to: {toEmail}");
                throw;
            }
        }

        public async Task SendOtpEmailAsync(string toEmail, string otpCode)
        {
            try
            {
                var emailSettings = _config.GetSection("EmailSettings");
                string? fromEmail = emailSettings["SenderEmail"];
                string? password = emailSettings["SenderPassword"];
                string? smtpServer = emailSettings["SmtpServer"];
                string? portStr = emailSettings["SmtpPort"];

                if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(smtpServer))
                {
                    throw new InvalidOperationException("Email configuration is missing");
                }

                if (string.IsNullOrEmpty(portStr) || !int.TryParse(portStr, out int port))
                {
                    throw new InvalidOperationException("Invalid SMTP port");
                }

                using (var client = new SmtpClient(smtpServer, port))
                {
                    client.Credentials = new NetworkCredential(fromEmail, password);
                    client.EnableSsl = true;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.Timeout = 30000;

                    string subject = "Mã OTP đặt lại mật khẩu";
                    string body = $"Mã OTP của bạn là: {otpCode}. Mã sẽ hết hạn sau 10 phút.";

                    var mail = new MailMessage(fromEmail, toEmail, subject, body)
                    {
                        IsBodyHtml = false
                    };

                    await client.SendMailAsync(mail);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send OTP email to: {toEmail}");
                throw;
            }
        }
        public async Task SendSimpleTextEmailAsync(string toEmail, string subject, string body)
        {
            try
            {
                var emailSettings = _config.GetSection("EmailSettings");
                string? fromEmail = emailSettings["SenderEmail"];
                string? password = emailSettings["SenderPassword"];
                string? smtpServer = emailSettings["SmtpServer"];
                string? portStr = emailSettings["SmtpPort"];

                if (string.IsNullOrEmpty(fromEmail) || string.IsNullOrEmpty(password) || string.IsNullOrEmpty(smtpServer))
                {
                    throw new InvalidOperationException("Email configuration is missing");
                }

                if (string.IsNullOrEmpty(portStr) || !int.TryParse(portStr, out int port))
                {
                    throw new InvalidOperationException("Invalid SMTP port");
                }

                using (var client = new SmtpClient(smtpServer, port))
                {
                    client.Credentials = new NetworkCredential(fromEmail, password);
                    client.EnableSsl = true;
                    client.DeliveryMethod = SmtpDeliveryMethod.Network;
                    client.Timeout = 30000;

                    var mail = new MailMessage(fromEmail, toEmail, subject, body)
                    {
                        IsBodyHtml = false // Plain text
                    };

                    mail.Headers.Add("X-Mailer", "WebNuocHoa E-commerce");

                    await client.SendMailAsync(mail);

                    _logger.LogInformation($"Simple email successfully sent to: {toEmail}");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Failed to send simple email to: {toEmail}");
                throw;
            }
        }

        public async Task SendEmailToAliasAsync(string aliasName, string token, string callbackUrl)
        {
            // Send email to an alias (placeholder implementation)
            // This could be used to send to different email addresses
            // for the same user account
            await SendVerificationEmailAsync($"{aliasName}@gmail.com", token, callbackUrl);
        }
    }
}
