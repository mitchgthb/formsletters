namespace FormsLetters.DTOs;

public record GenerateDocumentRequestDto
{
    public int ClientId { get; init; }
    public string TemplateName { get; init; } = string.Empty;
    public string UpdatedHtml { get; init; } = string.Empty;
}
