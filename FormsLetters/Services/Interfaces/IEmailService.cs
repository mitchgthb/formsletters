namespace FormsLetters.Services.Interfaces;

public interface IEmailService
{
    Task SendEmailAsync(string pdfPath, string recipientEmail, CancellationToken cancellationToken = default);
}
