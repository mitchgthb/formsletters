using Microsoft.AspNetCore.Mvc;
using FormsLetters.Services.Interfaces;
using FormsLetters.DTOs;
using FormsLetters.Services;

namespace FormsLetters.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LibreOfficeController : ControllerBase
{
    private readonly IDocumentGenerationService _documentService;
    private readonly IClientInfoService _clientService;
    private readonly ILogger<LibreOfficeController> _logger;
    private readonly string _baseUrl;

    public LibreOfficeController(
        IDocumentGenerationService documentService,
        IClientInfoService clientService,
        ILogger<LibreOfficeController> logger,
        IConfiguration configuration)
    {
        _documentService = documentService;
        _clientService = clientService;
        _logger = logger;
        _baseUrl = configuration.GetValue<string>("LibreOfficeOnline:BaseUrl") ?? "http://localhost:9980";
    }

    [HttpPost("prepare-document")]
    public async Task<IActionResult> PrepareDocument([FromBody] PrepareDocumentRequest request)
    {
        try
        {
            _logger.LogInformation("Preparing document for LibreOffice Online: {TemplateName}, Client: {ClientId}", 
                request.TemplateName, request.ClientId);

            // Get client data for merge fields
            var clientData = _clientService.GetClientAsync(request.ClientId);
            if (clientData == null)
            {
                _logger.LogError("Client data not found for ClientId: {ClientId}", request.ClientId);
                return NotFound(new { error = "Client data not found" });
            }
            
            // Create a working copy of the template with client data pre-populated
            var documentId = Guid.NewGuid().ToString();
            var documentUrl = await CreateWorkingDocument(request.TemplateName, request.ClientId, clientData, documentId);

            var response = new PrepareDocumentResponse
            {
                DocumentUrl = documentUrl,
                SessionId = documentId,
                WopiSrc = $"{Request.Scheme}://{Request.Host}/api/libreoffice/wopi/files/{documentId}",
                EditUrl = $"{_baseUrl}/loleaflet/dist/loleaflet.html?WOPISrc={Uri.EscapeDataString($"{Request.Scheme}://{Request.Host}/api/libreoffice/wopi/files/{documentId}")}"
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error preparing document for LibreOffice Online");
            return StatusCode(500, new { error = "Failed to prepare document", details = ex.Message });
        }
    }

    [HttpPost("generate-pdf")]
    public async Task<IActionResult> GeneratePdf([FromBody] GeneratePdfFromEditorRequest request)
    {
        try
        {
            _logger.LogInformation("Generating PDF from LibreOffice Online document: {TemplateName}, Client: {ClientId}", 
                request.TemplateName, request.ClientId);

            // Get the current document content from the working copy
            var documentContent = await GetWorkingDocumentContent(request.DocumentUrl);
            
            // Generate PDF using our existing service
            var generateRequest = new GenerateDocumentRequestDto
            {
                TemplateName = request.TemplateName,
                ClientId = request.ClientId,
                UpdatedHtml = documentContent
            };

            var pdfPath = await _documentService.GenerateAsync(generateRequest);
            var fileName = Path.GetFileName(pdfPath);
            var pdfUrl = $"{Request.Scheme}://{Request.Host}/letters/pdf/{Uri.EscapeDataString(fileName)}";

            return Ok(new { pdfUrl, fileName });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating PDF from LibreOffice Online");
            return StatusCode(500, new { error = "Failed to generate PDF", details = ex.Message });
        }
    }

    // WOPI endpoints for LibreOffice Online integration
    [HttpGet("wopi/files/{fileId}")]
    public async Task<IActionResult> GetFileInfo(string fileId)
    {
        try
        {
            var fileInfo = await GetWorkingDocumentInfo(fileId);
            
            var wopiInfo = new
            {
                BaseFileName = fileInfo.Name,
                Size = fileInfo.Size,
                Version = fileInfo.Version,
                UserId = "user1",
                UserFriendlyName = "User",
                UserCanWrite = true,
                UserCanNotWriteRelative = false,
                SupportsUpdate = true,
                SupportsLocks = true
            };

            return Ok(wopiInfo);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file info for WOPI: {FileId}", fileId);
            return NotFound();
        }
    }

    [HttpGet("wopi/files/{fileId}/contents")]
    public async Task<IActionResult> GetFileContents(string fileId)
    {
        try
        {
            var fileContent = await GetWorkingDocumentBytes(fileId);
            return File(fileContent, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting file contents for WOPI: {FileId}", fileId);
            return NotFound();
        }
    }

    [HttpPost("wopi/files/{fileId}/contents")]
    public async Task<IActionResult> UpdateFileContents(string fileId)
    {
        try
        {
            using var reader = new StreamReader(Request.Body);
            var content = await reader.ReadToEndAsync();
            
            await UpdateWorkingDocument(fileId, Request.Body);
            
            return Ok(new { Status = "Updated" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating file contents for WOPI: {FileId}", fileId);
            return StatusCode(500, new { error = "Failed to update document" });
        }
    }

    // Helper methods for document management
    private async Task<string> CreateWorkingDocument(string templateName, int clientId, object clientData, string documentId)
    {
        // Implementation: Create a working copy of the template with client data merged
        // Store it in a temporary location that can be accessed by LibreOffice Online
        // Return the URL that LibreOffice Online can use to access the document
        
        // For now, return a placeholder URL
        return $"{Request.Scheme}://{Request.Host}/api/libreoffice/documents/{documentId}";
    }

    private async Task<string> GetWorkingDocumentContent(string documentUrl)
    {
        // Implementation: Extract content from the working document
        // This might involve converting the DOCX to HTML or extracting the relevant parts
        return "";
    }

    private async Task<DocumentInfo> GetWorkingDocumentInfo(string fileId)
    {
        // Implementation: Get information about the working document
        return new DocumentInfo
        {
            Name = $"document_{fileId}.docx",
            Size = 1024,
            Version = 1
        };
    }

    private async Task<byte[]> GetWorkingDocumentBytes(string fileId)
    {
        // Implementation: Get the actual document bytes
        return Array.Empty<byte>();
    }

    private async Task UpdateWorkingDocument(string fileId, Stream content)
    {
        // Implementation: Update the working document with new content
    }

    private class DocumentInfo
    {
        public string Name { get; set; } = "";
        public long Size { get; set; }
        public int Version { get; set; }
    }
}

// DTOs
public class PrepareDocumentRequest
{
    public string TemplateName { get; set; } = "";
    public int ClientId { get; set; }
    public object? ClientData { get; set; }
}

public class PrepareDocumentResponse
{
    public string DocumentUrl { get; set; } = "";
    public string SessionId { get; set; } = "";
    public string WopiSrc { get; set; } = "";
    public string EditUrl { get; set; } = "";
}

public class GeneratePdfFromEditorRequest
{
    public string TemplateName { get; set; } = "";
    public int ClientId { get; set; }
    public string DocumentUrl { get; set; } = "";
}
