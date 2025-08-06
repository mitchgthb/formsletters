using FormsLetters.DTOs;

namespace FormsLetters.Services.Interfaces;

public interface ICognitoFormsService
{
    Task HandleSubmissionAsync(CognitoFormSubmissionDto dto, CancellationToken cancellationToken = default);
    Task<string> GetPrefillUrlAsync(int clientId, string formId, CancellationToken cancellationToken = default);
    Task<IEnumerable<FormDto>> GetFormsAsync(CancellationToken cancellationToken = default);
    Task<FormDto> GetFormAsync(string id, CancellationToken cancellationToken = default);
}
