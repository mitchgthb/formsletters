namespace FormsLetters.DTOs;

public record SendDocumentRequestDto
{
    public string PdfPath { get; init; } = string.Empty; // Local or SharePoint path
    public bool SendViaEmail { get; init; }
    public bool SendViaDocuSign { get; init; }
    public string? RecipientEmail { get; init; }
}
