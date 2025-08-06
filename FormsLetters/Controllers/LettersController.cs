using System.Threading;
using System.Threading.Tasks;
using FormsLetters.DTOs;
using FormsLetters.Services.Letter;
using FormsLetters.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FormsLetters.Controllers
{
    [ApiController]
    [Route("letters")]
    public class LettersController : ControllerBase
    {
        private readonly ILetterGenerationService _generator;
        private readonly IDocumentGenerationService _documentGenerator;
        private readonly ILogger<LettersController> _logger;

        public LettersController(ILetterGenerationService generator, IDocumentGenerationService documentGenerator, ILogger<LettersController> logger)
        {
            _generator = generator;
            _documentGenerator = documentGenerator;
            _logger = logger;
        }

        /// <summary>
        /// Generates a PDF letter composed of generated header, editable body (from Word template), and ending.
        /// </summary>
        [HttpPost("generate")]
        public async Task<ActionResult<GenerateDocumentResponseDto>> Generate([FromBody] GenerateLetterRequestDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.TemplateName))
                return BadRequest("templateName is required");

            var pdfPath = await _generator.GenerateAsync(request.TemplateName, request.ClientId, cancellationToken);
            _logger.LogInformation("Generated PDF letter {Path}", pdfPath);
            return Ok(new GenerateDocumentResponseDto { PdfPath = pdfPath });
        }

        /// <summary>
        /// Generates a PDF from complete HTML content (includes header, body, and signature)
        /// </summary>
        [HttpPost("generate-from-html")]
        public async Task<ActionResult<GenerateDocumentResponseDto>> GenerateFromHtml([FromBody] GenerateLetterFromHtmlRequestDto request, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(request.TemplateName))
                return BadRequest("templateName is required");
            
            if (string.IsNullOrWhiteSpace(request.UpdatedHtml))
                return BadRequest("updatedHtml is required");

            try
            {
                // Create a document generation request with the complete HTML
                var docRequest = new GenerateDocumentRequestDto
                {
                    TemplateName = request.TemplateName,
                    ClientId = request.ClientId,
                    UpdatedHtml = request.UpdatedHtml
                };

                // Use the existing document generation service
                var pdfPath = await _documentGenerator.GenerateAsync(docRequest, cancellationToken);
                _logger.LogInformation("Generated PDF from HTML {Path}", pdfPath);
                return Ok(new GenerateDocumentResponseDto { PdfPath = pdfPath });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating PDF from HTML for template {TemplateName}", request.TemplateName);
                return StatusCode(500, "Error generating PDF from HTML");
            }
        }

        /// <summary>
        /// Serves a generated PDF file for preview or download
        /// </summary>
        [HttpGet("pdf/{fileName}")]
        public async Task<IActionResult> GetPdf(string fileName)
        {
            try
            {
                // Construct the full path (assuming PDFs are stored in a specific output directory)
                var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "DocumentsTest", "Output");
                var filePath = Path.Combine(outputPath, fileName);

                if (!System.IO.File.Exists(filePath))
                {
                    _logger.LogWarning("PDF file not found: {FilePath}", filePath);
                    return NotFound($"PDF file '{fileName}' not found");
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
                var contentType = "application/pdf";
                
                return File(fileBytes, contentType, fileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error serving PDF file: {FileName}", fileName);
                return StatusCode(500, "Error retrieving PDF file");
            }
        }
    }
}
