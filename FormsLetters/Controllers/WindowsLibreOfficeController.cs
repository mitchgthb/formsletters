using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System.Text.Json;
using FormsLetters.Services;
using FormsLetters.Services.Interfaces;
using FormsLetters.DTOs;

namespace FormsLetters.Controllers;

/// <summary>
/// Windows-native LibreOffice integration controller
/// Uses LibreOffice headless mode for document editing and PDF generation
/// </summary>
[ApiController]
[Route("api/windows-libreoffice")]
public class WindowsLibreOfficeController : ControllerBase
{
    private readonly IDocumentGenerationService _documentService;
    private readonly IClientInfoService _clientService;
    private readonly ILogger<WindowsLibreOfficeController> _logger;
    private readonly IConfiguration _configuration;

    public WindowsLibreOfficeController(
        IDocumentGenerationService documentService,
        IClientInfoService clientService,
        ILogger<WindowsLibreOfficeController> logger,
        IConfiguration configuration)
    {
        _documentService = documentService;
        _clientService = clientService;
        _logger = logger;
        _configuration = configuration;
    }

    /// <summary>
    /// Prepare a document for editing using Windows LibreOffice
    /// </summary>
    [HttpPost("prepare-document")]
    public async Task<IActionResult> PrepareDocument([FromBody] WindowsPrepareDocumentRequest request)
    {
        try
        {
            _logger.LogInformation("Preparing document: {TemplateName} for client: {ClientId}", 
                request.TemplateName, request.ClientId);

            // Get client data
            var clientData = await _clientService.GetClientAsync(request.ClientId);
            if (clientData == null)
            {
                return BadRequest("Client not found");
            }

            // Find template path - check multiple possible locations
            var possiblePaths = new[]
            {
                Path.Combine(Directory.GetCurrentDirectory(), "DocumentsTest", "Templates", request.TemplateName),
                Path.Combine(Directory.GetCurrentDirectory(), "UploadedTemplates", request.TemplateName),
                Path.Combine(Directory.GetCurrentDirectory(), "Templates", request.TemplateName)
            };

            string? templatePath = null;
            foreach (var path in possiblePaths)
            {
                if (System.IO.File.Exists(path))
                {
                    templatePath = path;
                    break;
                }
            }

            if (templatePath == null)
            {
                return NotFound($"Template not found: {request.TemplateName}. Searched in: {string.Join(", ", possiblePaths)}");
            }

            // Create working document
            var fileId = Guid.NewGuid();
            var workingDirectory = Path.Combine(Path.GetTempPath(), "libreoffice-sessions", fileId.ToString());
            Directory.CreateDirectory(workingDirectory);

            var workingFileName = $"{fileId}.docx";
            var workingFilePath = Path.Combine(workingDirectory, workingFileName);

            // Copy template to working directory
            System.IO.File.Copy(templatePath, workingFilePath, true);

            // Populate template with client data
            await _documentService.PopulateTemplateWithClientDataAsync(workingFilePath, clientData);

            // Create document URL
            var documentUrl = $"{Request.Scheme}://{Request.Host}/api/windows-libreoffice/document/{fileId}";

            return Ok(new
            {
                success = true,
                fileId = fileId.ToString(),
                documentPath = workingFilePath,
                documentUrl = documentUrl,
                sessionId = fileId.ToString(),
                clientName = clientData.Name,
                templateName = request.TemplateName
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing document");
            return StatusCode(500, $"Error preparing document: {ex.Message}");
        }
    }

    /// <summary>
    /// Open document in LibreOffice for editing (Windows native)
    /// </summary>
    [HttpGet("edit/{fileId}")]
    public async Task<IActionResult> EditDocument(Guid fileId)
    {
        try
        {
            var workingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "DocumentsTest", "WorkingDocuments");
            var workingFiles = Directory.GetFiles(workingDirectory, $"{fileId}_*");

            if (!workingFiles.Any())
            {
                return NotFound("Document not found");
            }

            var filePath = workingFiles.First();
            var libreOfficePath = _configuration["LibreOffice:ExecutablePath"] ?? 
                @"C:\Program Files\LibreOffice\program\soffice.exe";

            if (!System.IO.File.Exists(libreOfficePath))
            {
                return BadRequest("LibreOffice not found. Please install LibreOffice or configure the path.");
            }

            // Launch LibreOffice with the document
            var startInfo = new ProcessStartInfo
            {
                FileName = libreOfficePath,
                Arguments = $"\"{filePath}\"",
                UseShellExecute = true,
                CreateNoWindow = false
            };

            var process = await Task.Run(() => Process.Start(startInfo));

            return Ok(new
            {
                success = true,
                message = "Document opened in LibreOffice",
                processId = process?.Id,
                documentPath = filePath
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error opening document in LibreOffice");
            return StatusCode(500, $"Error opening document: {ex.Message}");
        }
    }

    /// <summary>
    /// Generate PDF from edited document
    /// </summary>
    [HttpPost("generate-pdf/{fileId}")]
    public async Task<IActionResult> GeneratePdf(Guid fileId)
    {
        try
        {
            var workingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "DocumentsTest", "WorkingDocuments");
            var workingFiles = Directory.GetFiles(workingDirectory, $"{fileId}_*");

            if (!workingFiles.Any())
            {
                return NotFound("Document not found");
            }

            var documentPath = workingFiles.First();
            
            // Generate PDF using existing DocumentGenerationService
            var pdfPath = await _documentService.ConvertToPdfAsync(documentPath);

            if (string.IsNullOrEmpty(pdfPath) || !System.IO.File.Exists(pdfPath))
            {
                return StatusCode(500, "Failed to generate PDF");
            }

            var pdfUrl = $"/api/windowslibreoffice/pdf/{Path.GetFileName(pdfPath)}";

            return Ok(new
            {
                success = true,
                pdfUrl = pdfUrl,
                fileName = Path.GetFileName(pdfPath)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF");
            return StatusCode(500, $"Error generating PDF: {ex.Message}");
        }
    }

    /// <summary>
    /// Serve generated PDF files
    /// </summary>
    [HttpGet("pdf/{fileName}")]
    public IActionResult GetPdf(string fileName)
    {
        try
        {
            var outputDirectory = Path.Combine(Directory.GetCurrentDirectory(), "DocumentsTest", "Output");
            var filePath = Path.Combine(outputDirectory, fileName);

            if (!System.IO.File.Exists(filePath))
            {
                return NotFound("PDF file not found");
            }

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            return File(fileBytes, "application/pdf", fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error serving PDF file: {FileName}", fileName);
            return StatusCode(500, "Error serving PDF file");
        }
    }

    /// <summary>
    /// Get document content for web editing
    /// </summary>
    [HttpGet("document/{fileId}")]
    public async Task<IActionResult> GetDocument(Guid fileId)
    {
        try
        {
            var workingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "DocumentsTest", "WorkingDocuments");
            var workingFiles = Directory.GetFiles(workingDirectory, $"{fileId}_*");

            if (!workingFiles.Any())
            {
                return NotFound("Document not found");
            }

            var filePath = workingFiles.First();

            // Convert document to HTML for web editing
            var htmlContent = await _documentService.ConvertToHtmlAsync(filePath);

            return Ok(new
            {
                success = true,
                fileId = fileId,
                htmlContent = htmlContent,
                fileName = Path.GetFileName(filePath)
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting document content");
            return StatusCode(500, $"Error getting document: {ex.Message}");
        }
    }

    /// <summary>
    /// Save document content from web editor
    /// </summary>
    [HttpPost("save-document/{fileId}")]
    public async Task<IActionResult> SaveDocument(Guid fileId, [FromBody] WindowsSaveDocumentRequest request)
    {
        try
        {
            var workingDirectory = Path.Combine(Directory.GetCurrentDirectory(), "DocumentsTest", "WorkingDocuments");
            var workingFiles = Directory.GetFiles(workingDirectory, $"{fileId}_*");

            if (!workingFiles.Any())
            {
                return NotFound("Document not found");
            }

            var filePath = workingFiles.First();

            // Save HTML content back to document
            await _documentService.UpdateDocumentFromHtmlAsync(filePath, request.HtmlContent);

            return Ok(new
            {
                success = true,
                message = "Document saved successfully",
                lastModified = DateTime.Now
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving document");
            return StatusCode(500, $"Error saving document: {ex.Message}");
        }
    }

    /// <summary>
    /// Get LibreOffice installation status
    /// </summary>
    [HttpGet("status")]
    public IActionResult GetStatus()
    {
        try
        {
            var libreOfficePath = _configuration["LibreOffice:ExecutablePath"] ?? 
                @"C:\Program Files\LibreOffice\program\soffice.exe";

            var isInstalled = System.IO.File.Exists(libreOfficePath);
            
            return Ok(new
            {
                libreOfficeInstalled = isInstalled,
                libreOfficePath = libreOfficePath,
                windowsCompatible = true,
                nativeIntegration = true,
                capabilities = new[]
                {
                    "Native Windows LibreOffice editing",
                    "PDF generation",
                    "HTML conversion",
                    "Direct file manipulation",
                    "No Docker required"
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking LibreOffice status");
            return StatusCode(500, $"Error checking status: {ex.Message}");
        }
    }
}
