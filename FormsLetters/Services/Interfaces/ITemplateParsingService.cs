using FormsLetters.DTOs;

namespace FormsLetters.Services.Interfaces;

public interface ITemplateParsingService
{
    Task<ParseTemplateResponseDto> ParseAsync(byte[] docxBytes, CancellationToken cancellationToken = default);
}
