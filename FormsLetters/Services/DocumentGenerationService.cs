using System.Text.RegularExpressions;
using FormsLetters.DTOs;
using FormsLetters.Services.Interfaces;
using Microsoft.Extensions.Logging;
using DocumentFormat.OpenXml.Wordprocessing;
using DocumentFormat.OpenXml;
using OpenXmlPowerTools;
using DocumentFormat.OpenXml.Packaging;
using System.Xml.Linq;


namespace FormsLetters.Services;

public class DocumentGenerationService : IDocumentGenerationService
{
    private readonly ISharePointService _storage;
    private readonly IOdooService _odoo;
    private readonly ILogger<DocumentGenerationService> _logger;
    private readonly string _soffice;

    public DocumentGenerationService(ISharePointService storage, IOdooService odoo, ILogger<DocumentGenerationService> logger, Microsoft.Extensions.Options.IOptions<FormsLetters.Config.LibreOfficeOptions> options)
    {
        _storage = storage;
        _odoo = odoo;
        _logger = logger;
        
        // Use the configured path, but validate it exists and is accessible
        var configuredPath = options.Value.ExecutablePath;
        if (!string.IsNullOrWhiteSpace(configuredPath))
        {
            _soffice = configuredPath;
            _logger.LogInformation("Using configured LibreOffice path: {Path}", _soffice);
        }
        else
        {
            _soffice = "soffice";
            _logger.LogInformation("Using default LibreOffice command: {Command}", _soffice);
        }
        
        // Log the final configuration for debugging
        _logger.LogInformation("LibreOffice executable configured as: {Executable}", _soffice);
    }

    public async Task<string> GenerateAsync(GenerateDocumentRequestDto request, CancellationToken cancellationToken = default)
    {
        // 1. Load template bytes (.docx)
        var templateBytes = await _storage.GetTemplateBytesAsync(request.TemplateName, cancellationToken);
        if (templateBytes.Length == 0)
            throw new FileNotFoundException($"Template {request.TemplateName} not found");

        // 2. Open the package in memory
        using var mem = new MemoryStream();
        await mem.WriteAsync(templateBytes, cancellationToken);
        using var doc = DocumentFormat.OpenXml.Packaging.WordprocessingDocument.Open(mem, true);

        var mainPart = doc.MainDocumentPart ?? throw new InvalidOperationException("No main document part");

        // 3. Replace editable block with UpdatedHtml
        ReplaceEditableBlockWithHtml(mainPart, request.UpdatedHtml);

        // 4. Merge placeholders
        await MergePlaceholdersAsync(mainPart, request, cancellationToken);

        doc.Save();

        // 5. Persist modified docx to temp file
        var tempDir = GetTempDirectory();
        Directory.CreateDirectory(tempDir);
        var tempDocx = Path.Combine(tempDir, $"letter_{Guid.NewGuid():N}.docx");
        await File.WriteAllBytesAsync(tempDocx, mem.ToArray(), cancellationToken);

        // 6. Convert to PDF using LibreOffice
        var tempPdf = Path.ChangeExtension(tempDocx, ".pdf");
        RunLibreOfficeConvert(tempDocx, tempDir);

        var pdfBytes = await File.ReadAllBytesAsync(tempPdf, cancellationToken);

        // 7. Store PDF
        var fileName = Path.GetFileName(tempPdf);
        var pdfPath = await _storage.StorePdfAsync(pdfBytes, fileName, cancellationToken);

        // 8. Cleanup temp files (best-effort)
        TryDelete(tempDocx);
        TryDelete(tempPdf);

        return pdfPath;
    }

    private void ReplaceEditableBlockWithHtml(MainDocumentPart mainPart, string html)
    {
        var sdt = mainPart.Document.Body?
            .Descendants<SdtElement>()
            .FirstOrDefault(e => e.SdtProperties?.GetFirstChild<Tag>()?.Val == "BodyEditable");

        if (sdt is null)
        {
            _logger.LogWarning("Editable block not found; skipping HTML replacement");
            return;
        }

        OpenXmlCompositeElement? content = sdt switch
        {
            SdtRun sdtRun => sdtRun.SdtContentRun,
            SdtBlock sdtBlock => sdtBlock.SdtContentBlock,
            _ => null
        };

        if (content == null)
        {
            _logger.LogWarning("Unsupported content control type; cannot insert HTML");
            return;
        }

        // Convert HTML string to OpenXML elements using HtmlToWmlConverter (Html->Wml round-trip)
        try
        {
            var settings = new HtmlToWmlConverterSettings();

            // Convert HTML string to XHTML XElement (must be well-formed XML)
            XElement xhtml;
            try
            {
                xhtml = XElement.Parse(html);
            }
            catch (Exception parseEx)
            {
                _logger.LogError(parseEx, "HTML to XHTML parsing failed. Input HTML was: {Html}", html);
                return;
            }

            var wmlDoc = HtmlToWmlConverter.ConvertHtmlToWml(
                string.Empty, // defaultCss
                string.Empty, // authorCss
                string.Empty, // userCss
                xhtml,        // xhtml
                settings,     // settings
                null,         // templateDoc
                null          // annotatedHtmlDumpFileName
            );
            using var ms = new MemoryStream();
            wmlDoc.WriteByteArray(ms);
            using var wdoc = WordprocessingDocument.Open(ms, true);
            var bodyNodes = wdoc.MainDocumentPart!.Document.Body?.Elements<OpenXmlElement>().ToList() ?? new List<OpenXmlElement>();
            if (!bodyNodes.Any())
            {
                _logger.LogWarning("Converted HTML contained no usable content");
                return;
            }
            content.RemoveAllChildren();
            foreach (var node in bodyNodes)
                content.AppendChild(node.CloneNode(true));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "HTML to OpenXML conversion failed");
        }
    }

    private async Task MergePlaceholdersAsync(DocumentFormat.OpenXml.Packaging.MainDocumentPart mainPart, GenerateDocumentRequestDto request, CancellationToken token)
    {
        // Fetch mapping via Odoo service (reuse endpoint logic)
        var templateMetadata = await _odoo.GetFieldsAsync("res.partner", request.ClientId, new[] { "name", "email" }, token);
        var map = templateMetadata.ToDictionary(k => k.Key, v => v.Value?.ToString() ?? string.Empty, StringComparer.OrdinalIgnoreCase);
        var regex = new Regex("{{(.*?)}}", RegexOptions.Compiled);
        foreach (var text in mainPart.Document.Descendants<DocumentFormat.OpenXml.Wordprocessing.Text>())
        {
            var replaced = regex.Replace(text.Text, m => map.TryGetValue(m.Groups[1].Value, out var val) ? val : m.Value);
            text.Text = replaced;
        }
    }

    private void RunLibreOfficeConvert(string docxPath, string outputDir)
    {
        _logger.LogInformation("Starting LibreOffice conversion using: {Executable}", _soffice);
        _logger.LogInformation("Converting: {DocxPath} to PDF in directory: {OutputDir}", docxPath, outputDir);

        var psi = new System.Diagnostics.ProcessStartInfo
        {
            FileName = _soffice,
            Arguments = $"--headless --convert-to pdf \"{docxPath}\" --outdir \"{outputDir}\"",
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            UseShellExecute = false
        };

        // Set environment variables for LibreOffice in Docker
        if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true")
        {
            psi.Environment["HOME"] = "/tmp";
            psi.Environment["TMPDIR"] = "/tmp/libreoffice";
        }

        try
        {
            using var proc = System.Diagnostics.Process.Start(psi);
            proc!.WaitForExit(20000); // 20s timeout
            
            var output = proc.StandardOutput.ReadToEnd();
            var error = proc.StandardError.ReadToEnd();
            
            _logger.LogInformation("LibreOffice output: {Output}", output);
            if (!string.IsNullOrWhiteSpace(error))
            {
                _logger.LogWarning("LibreOffice stderr: {Error}", error);
            }

            if (proc.ExitCode != 0)
            {
                _logger.LogError("LibreOffice conversion failed with exit code {ExitCode}. Error: {Error}", proc.ExitCode, error);
                throw new InvalidOperationException($"PDF conversion failed with exit code {proc.ExitCode}. Error: {error}");
            }
        }
        catch (System.ComponentModel.Win32Exception ex)
        {
            _logger.LogError(ex, "Failed to start LibreOffice process. Executable: {Executable}", _soffice);
            throw new InvalidOperationException($"Failed to start LibreOffice process. Please ensure LibreOffice is installed and the path is correct: {_soffice}", ex);
        }
    }

    private bool IsCommandAvailable(string command)
    {
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = OperatingSystem.IsWindows() ? "where" : "which",
                Arguments = command,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                UseShellExecute = false
            };
            using var proc = System.Diagnostics.Process.Start(psi);
            proc!.WaitForExit(5000);
            return proc.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private string GetTempDirectory()
    {
        // In Docker, use /tmp/libreoffice for better permissions
        if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" ||
            File.Exists("/.dockerenv"))
        {
            return "/tmp/libreoffice";
        }
        
        // On Windows/local development
        return Path.Combine(Path.GetTempPath(), "FormsLetters");
    }

    private void TryDelete(string path)
    {
        try { File.Delete(path); } catch { }
    }

    /// <summary>
    /// Convert Word document to HTML for web editing
    /// </summary>
    public async Task<string> ConvertToHtmlAsync(string documentPath)
    {
        try
        {
            var outputPath = Path.ChangeExtension(documentPath, ".html");
            
            // Use LibreOffice to convert to HTML
            var outputDir = Path.GetDirectoryName(outputPath) ?? Directory.GetCurrentDirectory();
            RunLibreOfficeConvert(documentPath, outputDir);
            
            if (File.Exists(outputPath))
            {
                return await File.ReadAllTextAsync(outputPath);
            }
            
            return "<html><body><p>Failed to convert document to HTML</p></body></html>";
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting document to HTML: {DocumentPath}", documentPath);
            return "<html><body><p>Error converting document to HTML</p></body></html>";
        }
    }

    /// <summary>
    /// Update Word document from HTML content
    /// </summary>
    public async Task UpdateDocumentFromHtmlAsync(string documentPath, string htmlContent)
    {
        try
        {
            // Create temporary HTML file
            var tempHtmlPath = Path.ChangeExtension(documentPath, ".temp.html");
            await File.WriteAllTextAsync(tempHtmlPath, htmlContent);
            
            // Convert HTML back to Word document
            var tempDocPath = Path.ChangeExtension(documentPath, ".temp.docx");
            var tempDocDir = Path.GetDirectoryName(tempDocPath) ?? Directory.GetCurrentDirectory();
            RunLibreOfficeConvert(tempHtmlPath, tempDocDir);
            
            // Replace original document
            if (File.Exists(tempDocPath))
            {
                File.Copy(tempDocPath, documentPath, true);
                File.Delete(tempDocPath);
            }
            
            // Cleanup
            if (File.Exists(tempHtmlPath))
            {
                File.Delete(tempHtmlPath);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating document from HTML: {DocumentPath}", documentPath);
            throw;
        }
    }

    /// <summary>
    /// Populate template with client data and return file path
    /// </summary>
    public Task<string> PopulateTemplateWithClientDataAsync(string templatePath, ClientInfoDto clientData)
    {
        try
        {
            // Read the template document
            using var doc = WordprocessingDocument.Open(templatePath, true);
            var mainPart = doc.MainDocumentPart;
            
            if (mainPart?.Document.Body != null)
            {
                // Replace placeholders with client data
                var replacements = new Dictionary<string, string>
                {
                    { "[Client Name]", clientData.Name ?? "N/A" },
                    { "[Client Address]", clientData.Address ?? "N/A" },
                    { "[Client Email]", clientData.Email ?? "N/A" },
                    { "[Date]", DateTime.Now.ToString("MMMM dd, yyyy") }
                };

                // Replace text in all text elements
                foreach (var textElement in mainPart.Document.Body.Descendants<Text>())
                {
                    foreach (var replacement in replacements)
                    {
                        if (textElement.Text.Contains(replacement.Key))
                        {
                            textElement.Text = textElement.Text.Replace(replacement.Key, replacement.Value);
                        }
                    }
                }
            }
            
            return Task.FromResult(templatePath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error populating template with client data: {TemplatePath}", templatePath);
            throw;
        }
    }

    /// <summary>
    /// Convert document to PDF
    /// </summary>
    public Task<string> ConvertToPdfAsync(string documentPath)
    {
        try
        {
            var outputPath = Path.ChangeExtension(documentPath, ".pdf");
            var outputDir = Path.GetDirectoryName(outputPath) ?? Directory.GetCurrentDirectory();
            
            // Ensure output directory exists
            Directory.CreateDirectory(outputDir);
            
            RunLibreOfficeConvert(documentPath, outputDir);
            
            return Task.FromResult(outputPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error converting document to PDF: {DocumentPath}", documentPath);
            throw;
        }
    }

    private static class SimplePdfCreator
    {
        public static byte[] Create(string text)
        {
            // minimal PDF – fine for placeholder only
            var pdf = "%PDF-1.4\n1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj\n2 0 obj << /Type /Pages /Count 1 /Kids [3 0 R] >> endobj\n3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Contents 4 0 R >> endobj\n4 0 obj << /Length 44 >> stream\nBT /F1 24 Tf 50 700 Td (" + Escape(text) + ") Tj ET\nendstream endobj\n5 0 obj << /Type /Font /Subtype /Type1 /BaseFont /Helvetica >> endobj\nxref 0 6 0000000000 65535 f 0000000010 00000 n 0000000060 00000 n 0000000117 00000 n 0000000215 00000 n 0000000335 00000 n trailer << /Size 6 /Root 1 0 R >> startxref 445 %%EOF";
            return System.Text.Encoding.ASCII.GetBytes(pdf);
        }

        private static string Escape(string s) => s.Replace("(", "\\(").Replace(")", "\\)");
    }
}
