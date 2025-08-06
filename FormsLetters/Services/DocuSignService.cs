using FormsLetters.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace FormsLetters.Services;

public class DocuSignService : IDocuSignService
{
    private readonly ILogger<DocuSignService> _logger;

    public DocuSignService(ILogger<DocuSignService> logger)
    {
        _logger = logger;
    }

    public Task SendForSignatureAsync(string pdfPath, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Sending PDF to DocuSign (stub)");
        // TODO: Implement DocuSign API integration
        return Task.CompletedTask;
    }
}
