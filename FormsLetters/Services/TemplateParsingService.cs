using System.Text.RegularExpressions;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using FormsLetters.DTOs;
using FormsLetters.Services.Interfaces;
using Microsoft.Extensions.Logging;
using OpenXmlPowerTools;
using System.Collections.Generic;
using System.Xml.Linq;

namespace FormsLetters.Services;

public class TemplateParsingService : ITemplateParsingService
{
    private readonly ILogger<TemplateParsingService> _logger;

    private static readonly Regex PlaceholderRegex = new("{{(.*?)}}", RegexOptions.Compiled);

    public TemplateParsingService(ILogger<TemplateParsingService> logger)
    {
        _logger = logger;
    }

    public Task<ParseTemplateResponseDto> ParseAsync(byte[] docxBytes, CancellationToken cancellationToken = default)
    {
        using var mem = new MemoryStream(docxBytes);
        using var doc = WordprocessingDocument.Open(mem, true);
        var mainPart = doc.MainDocumentPart ?? throw new InvalidOperationException("No main document part found");

        string html = string.Empty;
        string extractedText = string.Empty;

        // 1. Try content control with tag "BodyEditable"
        _logger.LogInformation("Searching for content control with tag 'BodyEditable'");
        // Look for all SdtElement types (SdtBlock, SdtRun, SdtCell)
        var allSdtElements = mainPart.Document.Descendants<SdtElement>().ToList();
        _logger.LogInformation("Found {Count} content controls total", allSdtElements.Count);
        // Log all found tags for debugging
        foreach (var sdt in allSdtElements)
        {
            var tag = sdt.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value;
            var alias = sdt.SdtProperties?.GetFirstChild<SdtAlias>()?.Val?.Value;
            _logger.LogInformation("Found content control - Tag: '{Tag}', Alias: '{Alias}', Type: {Type}", 
                tag ?? "null", alias ?? "null", sdt.GetType().Name);
        }
        
        // Try to find by tag first
        var editableSdt = allSdtElements
            .FirstOrDefault(s => s.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value == "BodyEditable");
            
        // If not found by tag, try by alias/title
        if (editableSdt == null)
        {
            editableSdt = allSdtElements
                .FirstOrDefault(s => s.SdtProperties?.GetFirstChild<SdtAlias>()?.Val?.Value == "BodyEditable");
            if (editableSdt != null)
            {
                _logger.LogInformation("Found content control by alias 'BodyEditable'");
            }
        }

        if (editableSdt != null)
        {
            _logger.LogInformation("Editable content control found with tag/alias 'BodyEditable'");
            
            // Extract content directly from the content control - simplified approach
            // Just get the text content and convert to simple HTML paragraphs
            var paragraphs = editableSdt.Descendants<Paragraph>().ToList();
            var sb = new System.Text.StringBuilder();
            foreach (var p in paragraphs)
            {
                var text = p.InnerText?.Trim() ?? "";
                if (text.Length == 0) continue;
                sb.Append("<p>").Append(System.Net.WebUtility.HtmlEncode(text)).Append("</p>");
            }
            html = sb.ToString();
            
            if (string.IsNullOrEmpty(html))
            {
                _logger.LogWarning("Content control found but contains no paragraphs with text");
                html = "<p>No content found in editable section</p>";
            }
        }
        else
        {
            _logger.LogWarning("Editable content control with tag/alias 'BodyEditable' not found");
            _logger.LogWarning("Available content controls: {Controls}", 
                string.Join(", ", allSdtElements.Select(s => $"Tag: '{s.SdtProperties?.GetFirstChild<Tag>()?.Val?.Value ?? "null"}', Alias: '{s.SdtProperties?.GetFirstChild<SdtAlias>()?.Val?.Value ?? "null"}'")));
            _logger.LogWarning("Will search for <!--EDIT--> delimiters as fallback");
            // fallback: delimiters <!--EDIT-->
            extractedText = GetDelimitedText(mainPart);
            if (!string.IsNullOrEmpty(extractedText))
            {
                html = extractedText; // plain text fallback
            }
        }

        // If still empty, convert whole document
        if (string.IsNullOrEmpty(html))
        {
            try
            {
                var settings = new HtmlConverterSettings { FabricateCssClasses = true };
                html = HtmlConverter.ConvertToHtml(doc, settings).ToString(SaveOptions.DisableFormatting);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Full document HTML conversion failed, using plain text");
                html = System.Net.WebUtility.HtmlEncode(mainPart.Document.InnerText);
            }
        }

        var placeholders = PlaceholderRegex.Matches(html)
            .Select(m => m.Groups[1].Value)
            .Distinct()
            .ToList();
        Console.WriteLine(placeholders.ToString());
        // detect malformed placeholders (e.g., unmatched braces)
        var malformed = DetectMalformed(html);

        var response = new ParseTemplateResponseDto
        {
            EditableHtml = html,
            Placeholders = placeholders,
            MalformedPlaceholders = malformed.Any() ? malformed : null
        };

        return Task.FromResult(response);
    }

    private static string GetDelimitedText(MainDocumentPart mainPart)
    {
        var text = mainPart.Document.InnerText;
        const string start = "<!--EDIT-->";
        const string end = "<!--/EDIT-->";
        var startIdx = text.IndexOf(start, StringComparison.Ordinal);
        var endIdx = text.IndexOf(end, StringComparison.Ordinal);
        if (startIdx >= 0 && endIdx > startIdx)
        {
            return text.Substring(startIdx + start.Length, endIdx - startIdx - start.Length);
        }
        return string.Empty;
    }

    private static IEnumerable<string> DetectMalformed(string html)
    {
        // Detect single '{{' or '}}' without pair
        var malformed = new List<string>();
        int openCount = html.Count(c => c == '{');
        int closeCount = html.Count(c => c == '}');
        if (openCount % 2 != 0 || closeCount % 2 != 0 || openCount != closeCount)
        {
            malformed.Add("Unmatched braces detected");
        }
        return malformed;
    }
}
