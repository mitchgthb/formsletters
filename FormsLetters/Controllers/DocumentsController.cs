using FormsLetters.DTOs;
using FormsLetters.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FormsLetters.Controllers;

[ApiController]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentGenerationService _generator;
    private readonly IDocuSignService _docuSignService;
    private readonly IEmailService _emailService;

    public DocumentsController(IDocumentGenerationService generator, IDocuSignService docuSignService, IEmailService emailService)
    {
        _generator = generator;
        _docuSignService = docuSignService;
        _emailService = emailService;
    }

    [HttpPost("generate")]
    public async Task<ActionResult<GenerateDocumentResponseDto>> Generate([FromBody] GenerateDocumentRequestDto request, CancellationToken cancellationToken)
    {
        var pdfPath = await _generator.GenerateAsync(request, cancellationToken);
        var response = new GenerateDocumentResponseDto { PdfPath = pdfPath };
        return Ok(response);
    }

    [HttpPost("send")]
    public async Task<IActionResult> Send([FromBody] SendDocumentRequestDto request, CancellationToken cancellationToken)
    {
        if (request.SendViaDocuSign)
        {
            await _docuSignService.SendForSignatureAsync(request.PdfPath, cancellationToken);
        }
        if (request.SendViaEmail && !string.IsNullOrWhiteSpace(request.RecipientEmail))
        {
            await _emailService.SendEmailAsync(request.PdfPath, request.RecipientEmail!, cancellationToken);
        }
        return Accepted();
    }
}
