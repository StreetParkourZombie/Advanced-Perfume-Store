namespace PerfumeStore.Services
{
    public interface IEmailService
    {
        Task SendVerificationEmailAsync(string toEmail, string token, string callbackUrl);
        Task SendSimpleTextEmailAsync(string toEmail, string subject, string body);
        Task SendEmailToAliasAsync(string aliasName, string token, string callbackUrl);
        Task SendOtpEmailAsync(string toEmail, string otpCode);
    }
}