using FormsLetters.DTOs;

namespace FormsLetters.Services.Interfaces;

public interface ISharePointService
{
    Task<IEnumerable<TemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default);
    Task<byte[]> GetTemplateBytesAsync(string path, CancellationToken cancellationToken = default);
    Task<string> StorePdfAsync(byte[] pdfBytes, string fileName, CancellationToken cancellationToken = default);
}
