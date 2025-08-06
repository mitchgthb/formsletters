using FormsLetters.DTOs;

namespace FormsLetters.Services.Interfaces;

public interface ICognitoFormProcessor
{
    Task ProcessAsync(CognitoFormSubmissionDto submission, CancellationToken cancellationToken = default);
}
