namespace FormsLetters.Services.Interfaces;

public interface IDocuSignService
{
    Task SendForSignatureAsync(string pdfPath, CancellationToken cancellationToken = default);
}
