namespace MoqExample.Core.Interfaces;

public interface IEmailService
{
    bool SendEmail(string to, string subject, string body);
    Task<bool> SendEmailAsync(string to, string subject, string body);
}
