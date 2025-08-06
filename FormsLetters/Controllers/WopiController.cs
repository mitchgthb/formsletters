using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using FormsLetters.Config;
using FormsLetters.Services.Interfaces;
using FormsLetters.Services;
using FormsLetters.DTOs;
using System.Text.Json;
using DocumentFormat.OpenXml.Packaging;

namespace FormsLetters.Controllers
{
    [ApiController]
    [Route("wopi")]
    public class WopiController : ControllerBase
    {
        private readonly ILogger<WopiController> _logger;
        private readonly IClientInfoService _clientInfoService;
        private readonly IDocumentGenerationService _documentGenerationService;
        private readonly LocalFileOptions _fileStorageOptions;
        private static readonly Dictionary<string, WopiSession> _sessions = new();

        public WopiController(
            ILogger<WopiController> logger,
            IClientInfoService clientInfoService,
            IDocumentGenerationService documentGenerationService,
            IOptions<LocalFileOptions> fileStorageOptions)
        {
            _logger = logger;
            _clientInfoService = clientInfoService;
            _documentGenerationService = documentGenerationService;
            _fileStorageOptions = fileStorageOptions.Value;
        }

        [HttpPost("files/{fileId}/prepare")]
        public async Task<ActionResult<WopiPrepareResponse>> PrepareDocument(
            string fileId,
            [FromBody] WopiPrepareRequest request)
        {
            try
            {
                _logger.LogInformation("Preparing WOPI document for file: {FileId}", fileId);

                // Get template path
                var templatesPath = Path.Combine(Directory.GetCurrentDirectory(), "DocumentsTest", "Templates");
                var templatePath = Path.Combine(templatesPath, request.TemplateName);

                if (!System.IO.File.Exists(templatePath))
                {
                    return NotFound($"Template '{request.TemplateName}' not found");
                }

                // Get client data
                var client = await _clientInfoService.GetClientAsync(request.ClientId);
                if (client == null)
                {
                    return NotFound($"Client with ID {request.ClientId} not found");
                }

                // Create working directory
                var sessionId = Guid.NewGuid().ToString();
                var workingDir = Path.Combine(Path.GetTempPath(), "wopi-sessions", sessionId);
                Directory.CreateDirectory(workingDir);

                var workingFilePath = Path.Combine(workingDir, $"{fileId}.docx");

                // Copy and populate template
                System.IO.File.Copy(templatePath, workingFilePath, true);
                
                // Populate template with client data
                await PopulateTemplateWithClientData(workingFilePath, client);

                // Create WOPI session
                var session = new WopiSession
                {
                    FileId = fileId,
                    SessionId = sessionId,
                    FilePath = workingFilePath,
                    FileName = $"{request.TemplateName.Replace(".docx", "")}_{client.Name?.Replace(" ", "_")}.docx",
                    ClientId = request.ClientId,
                    ClientName = client.Name ?? "",
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    Version = 1
                };

                _sessions[fileId] = session;

                var wopiSrc = $"{Request.Scheme}://{Request.Host}/wopi/files/{fileId}";
                var loolUrl = $"http://localhost:9980/browser/dist/loleaflet.html?WOPISrc={Uri.EscapeDataString(wopiSrc)}&title={Uri.EscapeDataString(session.FileName)}&permission=edit&closebutton=1";

                return Ok(new WopiPrepareResponse
                {
                    FileId = fileId,
                    SessionId = sessionId,
                    WopiSrc = wopiSrc,
                    LoolUrl = loolUrl,
                    FileName = session.FileName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing WOPI document");
                return StatusCode(500, $"Error preparing document: {ex.Message}");
            }
        }

        [HttpPost("prepare-document")]
        public async Task<ActionResult> PrepareDocumentForLibreOffice([FromBody] WopiPrepareRequest request)
        {
            try
            {
                _logger.LogInformation("Preparing document for LibreOffice Online: Template={TemplateName}, Client={ClientId}", 
                    request.TemplateName, request.ClientId);

                // Generate a unique file ID for this session
                var fileId = Guid.NewGuid().ToString();

                // Get template path - try different locations
                string templatePath = "";
                var possiblePaths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "DocumentsTest", "Templates", $"{request.TemplateName}.docx"),
                    Path.Combine(Directory.GetCurrentDirectory(), "UploadedTemplates", $"{request.TemplateName}.docx"),
                    Path.Combine(Directory.GetCurrentDirectory(), "DocumentsTest", "Templates", request.TemplateName),
                    Path.Combine(Directory.GetCurrentDirectory(), "UploadedTemplates", request.TemplateName)
                };

                foreach (var path in possiblePaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        templatePath = path;
                        break;
                    }
                }

                if (string.IsNullOrEmpty(templatePath))
                {
                    return NotFound($"Template '{request.TemplateName}' not found");
                }

                // Get client data
                var client = await _clientInfoService.GetClientAsync(request.ClientId);
                if (client == null)
                {
                    return NotFound($"Client with ID {request.ClientId} not found");
                }

                // Create working directory
                var sessionId = Guid.NewGuid().ToString();
                var workingDir = Path.Combine(Path.GetTempPath(), "wopi-sessions", sessionId);
                Directory.CreateDirectory(workingDir);

                var workingFilePath = Path.Combine(workingDir, $"{fileId}.docx");

                // Copy and populate template
                System.IO.File.Copy(templatePath, workingFilePath, true);
                
                // Populate template with client data
                await PopulateTemplateWithClientData(workingFilePath, client);

                // Create WOPI session
                var session = new WopiSession
                {
                    FileId = fileId,
                    SessionId = sessionId,
                    FilePath = workingFilePath,
                    FileName = $"{Path.GetFileNameWithoutExtension(templatePath)}_{client.Name?.Replace(" ", "_")}.docx",
                    ClientId = request.ClientId,
                    ClientName = client.Name ?? "",
                    CreatedAt = DateTime.UtcNow,
                    LastModified = DateTime.UtcNow,
                    Version = 1
                };

                _sessions[fileId] = session;

                // Build LibreOffice Online URL
                var wopiSrc = $"{Request.Scheme}://{Request.Host}/wopi/files/{fileId}";
                var accessToken = sessionId; // Use session ID as access token for simplicity
                var loolUrl = $"http://localhost:9980/browser/dist/loleaflet.html?WOPISrc={Uri.EscapeDataString(wopiSrc)}&access_token={accessToken}&title={Uri.EscapeDataString(session.FileName)}&permission=edit&closebutton=1";

                return Ok(new
                {
                    sessionId = sessionId,
                    wopiSrc = wopiSrc,
                    accessToken = accessToken,
                    documentUrl = loolUrl
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error preparing document for LibreOffice Online");
                return StatusCode(500, $"Error preparing document: {ex.Message}");
            }
        }

        [HttpGet("files/{fileId}")]
        public ActionResult<WopiFileInfo> GetFileInfo(string fileId)
        {
            try
            {
                if (!_sessions.TryGetValue(fileId, out var session))
                {
                    return NotFound("File session not found");
                }

                if (!System.IO.File.Exists(session.FilePath))
                {
                    return NotFound("File not found");
                }

                var fileInfo = new System.IO.FileInfo(session.FilePath);

                var wopiFileInfo = new WopiFileInfo
                {
                    BaseFileName = session.FileName,
                    OwnerId = "user1",
                    Size = fileInfo.Length,
                    UserId = "user1",
                    UserFriendlyName = "User",
                    Version = session.Version.ToString(),
                    SupportsLocks = true,
                    SupportsGetLock = true,
                    SupportsExtendedLockLength = true,
                    SupportsUpdate = true,
                    UserCanWrite = true,
                    UserCanNotWriteRelative = false,
                    PostMessageOrigin = "*",
                    LastModifiedTime = session.LastModified.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    DisablePrint = false,
                    DisableExport = false,
                    DisableCopy = false
                };

                return Ok(wopiFileInfo);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file info for: {FileId}", fileId);
                return StatusCode(500, $"Error getting file info: {ex.Message}");
            }
        }

        [HttpGet("files/{fileId}/contents")]
        public IActionResult GetFile(string fileId)
        {
            try
            {
                if (!_sessions.TryGetValue(fileId, out var session))
                {
                    return NotFound("File session not found");
                }

                if (!System.IO.File.Exists(session.FilePath))
                {
                    return NotFound("File not found");
                }

                var fileBytes = System.IO.File.ReadAllBytes(session.FilePath);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting file contents for: {FileId}", fileId);
                return StatusCode(500, $"Error getting file: {ex.Message}");
            }
        }

        [HttpPost("files/{fileId}/contents")]
        public async Task<IActionResult> PutFile(string fileId)
        {
            try
            {
                if (!_sessions.TryGetValue(fileId, out var session))
                {
                    return NotFound("File session not found");
                }

                using var stream = new MemoryStream();
                await Request.Body.CopyToAsync(stream);
                var fileBytes = stream.ToArray();

                await System.IO.File.WriteAllBytesAsync(session.FilePath, fileBytes);

                // Update session
                session.LastModified = DateTime.UtcNow;
                session.Version++;

                _logger.LogInformation("File updated: {FileId}, Version: {Version}", fileId, session.Version);

                return Ok(new { message = "File updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating file: {FileId}", fileId);
                return StatusCode(500, $"Error updating file: {ex.Message}");
            }
        }

        [HttpPost("files/{fileId}/lock")]
        public IActionResult Lock(string fileId, [FromHeader(Name = "X-WOPI-Lock")] string lockId)
        {
            try
            {
                if (!_sessions.TryGetValue(fileId, out var session))
                {
                    return NotFound("File session not found");
                }

                session.LockId = lockId;
                session.LockExpiry = DateTime.UtcNow.AddMinutes(30);

                _logger.LogInformation("File locked: {FileId}, LockId: {LockId}", fileId, lockId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error locking file: {FileId}", fileId);
                return StatusCode(500, $"Error locking file: {ex.Message}");
            }
        }

        [HttpPost("files/{fileId}/unlock")]
        public IActionResult Unlock(string fileId, [FromHeader(Name = "X-WOPI-Lock")] string lockId)
        {
            try
            {
                if (!_sessions.TryGetValue(fileId, out var session))
                {
                    return NotFound("File session not found");
                }

                if (session.LockId != lockId)
                {
                    return Conflict("Lock ID mismatch");
                }

                session.LockId = null;
                session.LockExpiry = null;

                _logger.LogInformation("File unlocked: {FileId}", fileId);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unlocking file: {FileId}", fileId);
                return StatusCode(500, $"Error unlocking file: {ex.Message}");
            }
        }

        [HttpGet("files/{fileId}/download")]
        public IActionResult DownloadFile(string fileId)
        {
            try
            {
                if (!_sessions.TryGetValue(fileId, out var session))
                {
                    return NotFound("File session not found");
                }

                if (!System.IO.File.Exists(session.FilePath))
                {
                    return NotFound("File not found");
                }

                var fileBytes = System.IO.File.ReadAllBytes(session.FilePath);
                return File(fileBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", session.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading file: {FileId}", fileId);
                return StatusCode(500, $"Error downloading file: {ex.Message}");
            }
        }

        private async Task PopulateTemplateWithClientData(string filePath, ClientInfoDto client)
        {
            try
            {
                using var document = WordprocessingDocument.Open(filePath, true);
                var body = document.MainDocumentPart?.Document?.Body;
                
                if (body != null)
                {
                    var textElements = body.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>().ToList();
                    
                    var replacements = new Dictionary<string, string>
                    {
                        { "{{ClientName}}", client.Name ?? "N/A" },
                        { "{{ClientAddress}}", client.Address ?? "N/A" },
                        { "{{ClientEmail}}", client.Email ?? "N/A" },
                        { "{{ClientId}}", client.Id.ToString() },
                        { "{{ClientTaxId}}", client.TaxNumber ?? "N/A" },
                        { "{{AssessmentDate}}", DateTime.Now.ToString("MMMM dd, yyyy") },
                        { "{{TotalTaxDue}}", "$2,450.00" },
                        { "{{PaymentDueDate}}", DateTime.Now.AddDays(30).ToString("MMMM dd, yyyy") },
                        { "{{company_name}}", client.Name ?? "N/A" },
                        { "{{contact_name}}", client.Name ?? "N/A" },
                        { "{{contact_address_inline}}", client.Address ?? "N/A" },
                        { "{{email}}", client.Email ?? "N/A" },
                        { "{{project_ids}}", "TAX-2025-001" },
                        { "{{current_date}}", DateTime.Now.ToString("MMMM dd, yyyy") }
                    };

                    foreach (var textElement in textElements)
                    {
                        if (textElement.Text != null)
                        {
                            var originalText = textElement.Text;
                            var modifiedText = originalText;

                            foreach (var replacement in replacements)
                            {
                                modifiedText = modifiedText.Replace(replacement.Key, replacement.Value);
                            }

                            if (modifiedText != originalText)
                            {
                                textElement.Text = modifiedText;
                            }
                        }
                    }
                }

                document.Save();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error populating template with client data");
                throw;
            }
        }
    }

    public class WopiSession
    {
        public string FileId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public int ClientId { get; set; }
        public string ClientName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime LastModified { get; set; }
        public int Version { get; set; }
        public string? LockId { get; set; }
        public DateTime? LockExpiry { get; set; }
    }

    public class WopiPrepareRequest
    {
        public string TemplateName { get; set; } = string.Empty;
        public int ClientId { get; set; }
    }

    public class WopiPrepareResponse
    {
        public string FileId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        public string WopiSrc { get; set; } = string.Empty;
        public string LoolUrl { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
    }

    public class WopiFileInfo
    {
        public string BaseFileName { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public long Size { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string UserFriendlyName { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public bool SupportsLocks { get; set; }
        public bool SupportsGetLock { get; set; }
        public bool SupportsExtendedLockLength { get; set; }
        public bool SupportsUpdate { get; set; }
        public bool UserCanWrite { get; set; }
        public bool UserCanNotWriteRelative { get; set; }
        public string PostMessageOrigin { get; set; } = string.Empty;
        public string LastModifiedTime { get; set; } = string.Empty;
        public bool DisablePrint { get; set; }
        public bool DisableExport { get; set; }
        public bool DisableCopy { get; set; }
    }
}
