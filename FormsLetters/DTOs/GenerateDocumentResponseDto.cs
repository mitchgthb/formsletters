namespace FormsLetters.DTOs;

public record GenerateDocumentResponseDto
{
    public string PdfPath { get; init; } = string.Empty;
}
