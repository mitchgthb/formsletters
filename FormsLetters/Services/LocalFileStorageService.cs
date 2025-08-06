using FormsLetters.Config;
using FormsLetters.DTOs;
using FormsLetters.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FormsLetters.Services;

// Local implementation of ISharePointService for dev/testing without Microsoft Graph.
public class LocalFileStorageService : ISharePointService
{
    private readonly ILogger<LocalFileStorageService> _logger;
    private readonly LocalFileOptions _options;

    public LocalFileStorageService(ILogger<LocalFileStorageService> logger, IOptions<LocalFileOptions> options)
    {
        _logger = logger;
        _options = options.Value;
    }

    private string TemplatesRoot => Path.Combine(_options.RootPath, _options.TemplatesFolder);
    // Force Output folder for dev/test
    private string GeneratedRoot => @"C:\Users\mitch\OneDrive\Desktop\FomsLettersDevTest\Output";

    public Task<IEnumerable<TemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        if (!Directory.Exists(TemplatesRoot))
        {
            _logger.LogWarning("Templates directory {Dir} does not exist", TemplatesRoot);
            return Task.FromResult<IEnumerable<TemplateDto>>(new List<TemplateDto>());
        }

        var files = Directory.GetFiles(TemplatesRoot, "*.docx");
        var list = files.Select(f => new TemplateDto { Name = Path.GetFileName(f), Path = Path.GetFileName(f) }).ToList();
        return Task.FromResult<IEnumerable<TemplateDto>>(list);
    }

    public Task<byte[]> GetTemplateBytesAsync(string path, CancellationToken cancellationToken = default)
    {
        var filePath = Path.Combine(TemplatesRoot, path);
        if (!File.Exists(filePath))
        {
            _logger.LogWarning("Template file {File} not found", filePath);
            return Task.FromResult(Array.Empty<byte>());
        }

        var bytes = File.ReadAllBytes(filePath);
        return Task.FromResult(bytes);
    }

    public Task<string> StorePdfAsync(byte[] pdfBytes, string fileName, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(GeneratedRoot);
        var filePath = Path.Combine(GeneratedRoot, fileName);
        File.WriteAllBytes(filePath, pdfBytes);
        _logger.LogInformation("Saved PDF to {Path}", filePath);
        // Return relative path so frontend can pull via static file middleware later
        return Task.FromResult($"/Generated/{fileName}");
    }
}
