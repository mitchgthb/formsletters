using Microsoft.AspNetCore.Mvc;
using FormsLetters.Services.Interfaces;
using FormsLetters.Services;
using FormsLetters.DTOs;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace FormsLetters.Controllers
{
    /// <summary>
    /// Mock controller to simulate LibreOffice Online capabilities for development
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class MockLibreOfficeController : ControllerBase
    {
        private readonly ILogger<MockLibreOfficeController> _logger;
        private readonly IDocumentGenerationService _documentService;
        private readonly IClientInfoService _clientInfoService;

        public MockLibreOfficeController(
            ILogger<MockLibreOfficeController> logger,
            IDocumentGenerationService documentService,
            IClientInfoService clientInfoService)
        {
            _logger = logger;
            _documentService = documentService;
            _clientInfoService = clientInfoService;
        }

        /// <summary>
        /// Mock LibreOffice Online capabilities endpoint
        /// </summary>
        [HttpGet("capabilities")]
        public IActionResult GetCapabilities()
        {
            return Ok(new
            {
                hasMobileSupport = true,
                hasProxyPrefix = false,
                hasWebAuthnSupport = false,
                hasWOPILocks = true,
                hasWriteSupport = true,
                productName = "Mock LibreOffice Online",
                productVersion = "1.0.0",
                productVersionHash = "mock",
                supportedLOKFeatureSet = "1",
                templatesSupported = true,
                templatesPath = "/api/mocklibreoffice/templates"
            });
        }

        /// <summary>
        /// Get available templates from DocumentsTest/Templates
        /// </summary>
        [HttpGet("templates")]
        public IActionResult GetTemplates()
        {
            try
            {
                var templatesPath = Path.Combine(Directory.GetCurrentDirectory(), "DocumentsTest", "Templates");
                
                if (!Directory.Exists(templatesPath))
                {
                    return Ok(new { templates = new object[0], message = "Templates directory not found" });
                }

                var templateFiles = Directory.GetFiles(templatesPath, "*.docx")
                    .Select(file => new
                    {
                        id = Path.GetFileNameWithoutExtension(file),
                        name = Path.GetFileName(file),
                        path = file,
                        size = new FileInfo(file).Length,
                        lastModified = System.IO.File.GetLastWriteTime(file)
                    })
                    .ToArray();

                return Ok(new { templates = templateFiles });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving templates");
                return StatusCode(500, $"Error retrieving templates: {ex.Message}");
            }
        }

        /// <summary>
        /// Mock document editor interface with template content
        /// </summary>
        [HttpGet("editor/{templateId}")]
        public async Task<IActionResult> GetEditor(string templateId, [FromQuery] int? clientId = null)
        {
            try
            {
                var templatesPath = Path.Combine(Directory.GetCurrentDirectory(), "DocumentsTest", "Templates");
                var templatePath = Path.Combine(templatesPath, $"{templateId}.docx");

                if (!System.IO.File.Exists(templatePath))
                {
                    return NotFound($"Template {templateId} not found");
                }

                // Extract document content for preview
                string documentContent = "Loading document content...";
                try
                {
                    documentContent = await ExtractDocumentTextAsync(templatePath);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Could not extract document content, using placeholder");
                    documentContent = $"Document: {templateId}.docx (Content extraction failed)";
                }

                // Get client data if clientId provided
                ClientInfoDto? clientData = null;
                if (clientId.HasValue)
                {
                    try
                    {
                        clientData = await _clientInfoService.GetClientAsync(clientId.Value);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Could not load client data for ID: {ClientId}", clientId);
                    }
                }

                var html = GenerateEditorHtml(templateId, documentContent, clientData);
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating editor for template: {TemplateId}", templateId);
                return StatusCode(500, $"Error loading editor: {ex.Message}");
            }
        }

        /// <summary>
        /// Extract text content from Word document
        /// </summary>
        private async Task<string> ExtractDocumentTextAsync(string documentPath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    using var doc = WordprocessingDocument.Open(documentPath, false);
                    var body = doc.MainDocumentPart?.Document?.Body;
                    if (body == null) return "Document is empty";

                    var texts = body.Descendants<Text>().Select(t => t.Text).ToArray();
                    return string.Join(" ", texts);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error extracting text from document: {DocumentPath}", documentPath);
                    return $"Error reading document: {ex.Message}";
                }
            });
        }

        /// <summary>
        /// Generate HTML for the mock editor interface
        /// </summary>
        private string GenerateEditorHtml(string templateId, string documentContent, ClientInfoDto? clientData)
        {
            // Populate merge fields if client data is available
            var populatedContent = PopulateMergeFields(documentContent, clientData);
            
            var clientInfo = clientData != null 
                ? $"<div class='client-info'><h3>Client Information</h3><p><strong>Name:</strong> {clientData.Name}</p><p><strong>Email:</strong> {clientData.Email}</p><p><strong>Address:</strong> {clientData.Address}</p></div>"
                : "<div class='client-info'><p>No client data loaded</p></div>";

            return $@"
<!DOCTYPE html>
<html lang='en'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <title>LibreOffice Mock Editor - {templateId}</title>
    <style>
        body {{ 
            font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, sans-serif;
            margin: 0;
            padding: 20px;
            background-color: #f5f5f5;
        }}
        .container {{ 
            max-width: 1200px;
            margin: 0 auto;
            background: white;
            border-radius: 8px;
            box-shadow: 0 2px 10px rgba(0,0,0,0.1);
            overflow: hidden;
        }}
        .header {{ 
            background: #0078d4;
            color: white;
            padding: 15px 20px;
            display: flex;
            justify-content: space-between;
            align-items: center;
        }}
        .toolbar {{ 
            background: #f8f9fa;
            padding: 10px 20px;
            border-bottom: 1px solid #e1e4e8;
            display: flex;
            gap: 10px;
        }}
        .btn {{ 
            padding: 6px 12px;
            border: 1px solid #ccc;
            background: white;
            border-radius: 4px;
            cursor: pointer;
            font-size: 14px;
        }}
        .btn:hover {{ background: #f0f0f0; }}
        .btn.primary {{ background: #0078d4; color: white; border-color: #0078d4; }}
        .editor-area {{ 
            display: flex;
            height: 600px;
        }}
        .sidebar {{ 
            width: 300px;
            background: #f8f9fa;
            border-right: 1px solid #e1e4e8;
            padding: 20px;
            overflow-y: auto;
        }}
        .document-editor {{ 
            flex: 1;
            padding: 20px;
            background: white;
            overflow-y: auto;
        }}
        .document-content {{ 
            border: 1px solid #e1e4e8;
            padding: 20px;
            min-height: 500px;
            line-height: 1.6;
            font-size: 14px;
            white-space: pre-wrap;
            word-wrap: break-word;
        }}
        .client-info {{ 
            background: #e8f4fd;
            padding: 15px;
            border-radius: 6px;
            margin-bottom: 20px;
        }}
        .client-info h3 {{ 
            margin-top: 0;
            color: #0078d4;
        }}
        .status {{ 
            padding: 10px 20px;
            background: #d4edda;
            color: #155724;
            border-bottom: 1px solid #c3e6cb;
        }}
        .template-info {{ 
            background: #fff3cd;
            padding: 15px;
            border-radius: 6px;
            margin-bottom: 20px;
        }}
        .template-info h3 {{ 
            margin-top: 0;
            color: #856404;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>LibreOffice Mock Editor</h1>
            <div>Template: {templateId}.docx</div>
        </div>
        
        <div class='status'>
            ✅ Mock LibreOffice Online is running. Template loaded successfully.
        </div>
        
        <div class='toolbar'>
            <button class='btn'>📄 File</button>
            <button class='btn'>✏️ Edit</button>
            <button class='btn'>📝 Insert</button>
            <button class='btn'>🎨 Format</button>
            <button class='btn primary' onclick='saveDocument()'>💾 Save</button>
            <button class='btn' onclick='generatePdf()'>📄 Export PDF</button>
            <button class='btn' onclick='closeEditor()'>❌ Close</button>
        </div>
        
        <div class='editor-area'>
            <div class='sidebar'>
                <div class='template-info'>
                    <h3>Template Information</h3>
                    <p><strong>Name:</strong> {templateId}.docx</p>
                    <p><strong>Type:</strong> Word Document</p>
                    <p><strong>Status:</strong> Ready for editing</p>
                </div>
                
                {clientInfo}
                
                <div style='margin-top: 20px;'>
                    <h4>Available Actions:</h4>
                    <button class='btn' style='width: 100%; margin-bottom: 5px;' onclick='populateTemplate()'>
                        🔄 Populate with Client Data
                    </button>
                    <button class='btn' style='width: 100%; margin-bottom: 5px;' onclick='previewDocument()'>
                        👁️ Preview Document
                    </button>
                    <button class='btn' style='width: 100%;' onclick='downloadTemplate()'>
                        ⬇️ Download Template
                    </button>
                </div>
            </div>
            
            <div class='document-editor'>
                <h3>Document Content:</h3>
                <div class='document-content' contenteditable='true' id='documentContent'>
{populatedContent}
                </div>
            </div>
        </div>
    </div>
    
    <script>
        function saveDocument() {{
            alert('Document saved successfully!\\n(This is a mock implementation)');
        }}
        
        function generatePdf() {{
            alert('PDF generation started!\\n(This is a mock implementation)');
        }}
        
        function closeEditor() {{
            if (confirm('Close editor? Any unsaved changes will be lost.')) {{
                window.close();
            }}
        }}
        
        function populateTemplate() {{
            // The template is already populated with client data on page load
            alert('Template is already populated with client data!\\nAll merge fields have been replaced with actual client information.');
        }}
        
        function previewDocument() {{
            window.open('/api/mocklibreoffice/preview/{templateId}', '_blank');
        }}
        
        function downloadTemplate() {{
            window.open('/api/windowslibreoffice/template/{templateId}/download', '_blank');
        }}
        
        // Auto-save simulation
        setInterval(() => {{
            console.log('Auto-saving document...');
        }}, 30000);
    </script>
</body>
</html>";
        }

        /// <summary>
        /// Mock document preview
        /// </summary>
        [HttpGet("preview/{templateId}")]
        public async Task<IActionResult> PreviewDocument(string templateId)
        {
            try
            {
                var templatesPath = Path.Combine(Directory.GetCurrentDirectory(), "DocumentsTest", "Templates");
                var templatePath = Path.Combine(templatesPath, $"{templateId}.docx");

                if (!System.IO.File.Exists(templatePath))
                {
                    return NotFound($"Template {templateId} not found");
                }

                // For now, return a simple preview
                var content = await ExtractDocumentTextAsync(templatePath);
                
                var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Document Preview - {templateId}</title>
    <style>
        body {{ font-family: Arial, sans-serif; padding: 20px; max-width: 800px; margin: 0 auto; }}
        .preview {{ border: 1px solid #ccc; padding: 20px; background: white; line-height: 1.6; }}
    </style>
</head>
<body>
    <h1>Document Preview: {templateId}.docx</h1>
    <div class='preview'>{content}</div>
    <button onclick='window.close()'>Close Preview</button>
</body>
</html>";
                
                return Content(html, "text/html");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating preview for template: {TemplateId}", templateId);
                return StatusCode(500, $"Error generating preview: {ex.Message}");
            }
        }

        /// <summary>
        /// Populate merge fields in document content with client data
        /// </summary>
        private string PopulateMergeFields(string documentContent, ClientInfoDto? clientData)
        {
            if (clientData == null)
            {
                return documentContent;
            }

            var populatedContent = documentContent;

            // Define merge field mappings
            var mergeFields = new Dictionary<string, string>
            {
                { "{{ClientName}}", clientData.Name ?? "N/A" },
                { "{{ClientAddress}}", clientData.Address ?? "N/A" },
                { "{{ClientEmail}}", clientData.Email ?? "N/A" },
                { "{{ClientId}}", clientData.Id.ToString() },
                { "{{ClientTaxId}}", clientData.TaxNumber ?? "N/A" },
                { "{{AssessmentDate}}", DateTime.Now.ToString("MMMM dd, yyyy") },
                { "{{TotalTaxDue}}", "$2,450.00" }, // Mock data
                { "{{PaymentDueDate}}", DateTime.Now.AddDays(30).ToString("MMMM dd, yyyy") },
                { "{{company_name}}", clientData.Name ?? "N/A" },
                { "{{contact_name}}", clientData.Name ?? "N/A" },
                { "{{contact_address_inline}}", clientData.Address ?? "N/A" },
                { "{{email}}", clientData.Email ?? "N/A" },
                { "{{project_ids}}", "TAX-2025-001" }, // Mock data
                { "{{current_date}}", DateTime.Now.ToString("MMMM dd, yyyy") }
            };

            // Replace all merge fields
            foreach (var field in mergeFields)
            {
                populatedContent = populatedContent.Replace(field.Key, field.Value);
            }

            return populatedContent;
        }
    }
}
