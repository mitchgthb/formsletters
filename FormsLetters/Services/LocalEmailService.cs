using FormsLetters.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FormsLetters.Config;

namespace FormsLetters.Services;

public class LocalEmailService : IEmailService
{
    private readonly ILogger<LocalEmailService> _logger;
    private readonly LocalFileOptions _options;

    public LocalEmailService(ILogger<LocalEmailService> logger, IOptions<LocalFileOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    public Task SendEmailAsync(string pdfPath, string recipientEmail, CancellationToken cancellationToken = default)
    {
        // Simply copy the pdf to an Outbox folder for manual review
        var outboxDir = Path.Combine(_options.RootPath, "Outbox");
        Directory.CreateDirectory(outboxDir);

        var fileName = Path.GetFileName(pdfPath);
        var destPath = Path.Combine(outboxDir, fileName);

        try
        {
            File.Copy(pdfPath, destPath, overwrite: true);
            _logger.LogInformation("[DEV] Saved email pdf copy to {Dest}", destPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to copy pdf to outbox");
        }
        return Task.CompletedTask;
    }
}
