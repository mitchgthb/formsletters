using FormsLetters.DTOs;

namespace FormsLetters.Services.Interfaces;

public interface ITemplateMetadataService
{
    Task<TemplateMetadata?> GetMetadataAsync(string templateName, CancellationToken cancellationToken = default);
    Task<IDictionary<string, TemplateMetadata>> GetAllAsync(CancellationToken cancellationToken = default);
}
