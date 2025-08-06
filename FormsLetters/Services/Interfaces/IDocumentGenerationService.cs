using FormsLetters.DTOs;

namespace FormsLetters.Services.Interfaces;

public interface IDocumentGenerationService
{
    /// <summary>
    /// Generates a PDF from a template and client data. Returns the generated PDF path.
    /// </summary>
    Task<string> GenerateAsync(GenerateDocumentRequestDto request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Convert Word document to HTML for web editing
    /// </summary>
    Task<string> ConvertToHtmlAsync(string documentPath);

    /// <summary>
    /// Update Word document from HTML content
    /// </summary>
    Task UpdateDocumentFromHtmlAsync(string documentPath, string htmlContent);

    /// <summary>
    /// Populate template with client data and return file path
    /// </summary>
    Task<string> PopulateTemplateWithClientDataAsync(string templatePath, ClientInfoDto clientData);

    /// <summary>
    /// Convert document to PDF
    /// </summary>
    Task<string> ConvertToPdfAsync(string documentPath);
}
