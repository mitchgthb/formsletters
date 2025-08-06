using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FormsLetters.Services.Odoo;
using Microsoft.Extensions.Logging;
using FormsLetters.Services.Interfaces;

namespace FormsLetters.Services.Letter
{
    public interface ILetterAssemblerService
    {
        Task<string> AssembleHtmlAsync(string templateName, int clientId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Combines generated header, parsed editable body from Word template, and generated ending into a single HTML.
    /// Performs placeholder substitution using data fetched from Odoo.
    /// </summary>
    public class LetterAssemblerService : ILetterAssemblerService
    {
        private readonly IOdooDataService _odoo;
        private readonly ITemplateSectionBuilder _headerBuilder;
        private readonly ITemplateSectionBuilder _endingBuilder;
        private readonly ITemplateParsingService _templateParser;
        private readonly ISharePointService _templateStorage;
        private readonly ILogger<LetterAssemblerService> _logger;

        public LetterAssemblerService(
            IOdooDataService odoo,
            HeaderBuilder headerBuilder,
            EndingBuilder endingBuilder,
            ITemplateParsingService templateParser,
            ISharePointService templateStorage,
            ILogger<LetterAssemblerService> logger)
        {
            _odoo = odoo;
            _headerBuilder = headerBuilder;
            _endingBuilder = endingBuilder;
            _templateParser = templateParser;
            _templateStorage = templateStorage;
            _logger = logger;
        }

        public async Task<string> AssembleHtmlAsync(string templateName, int clientId, CancellationToken cancellationToken = default)
        {
            var data = await _odoo.GetLetterDataAsync(clientId, cancellationToken);

            // 1. header
            var headerHtml = await _headerBuilder.BuildAsync(data, cancellationToken);

            // 2. editable body from Word template
            var templateBytes = await _templateStorage.GetTemplateBytesAsync(templateName, cancellationToken);
            var parseResult = await _templateParser.ParseAsync(templateBytes, cancellationToken);
            var bodyHtml = SubstitutePlaceholders(parseResult.EditableHtml ?? string.Empty, data);

            // 3. ending
            var endingHtml = await _endingBuilder.BuildAsync(data, cancellationToken);

            var fullHtml = new StringBuilder();
            fullHtml.Append("<html><body style=\"font-family:Arial, sans-serif;\">")
                    .Append(headerHtml)
                    .Append(bodyHtml)
                    .Append(endingHtml)
                    .Append("</body></html>");

            var html = fullHtml.ToString();
            _logger.LogDebug("Assembled letter HTML length {Length}", html.Length);
            return html;
        }

        private static string SubstitutePlaceholders(string html, Dictionary<string, string> data)
        {
            if (string.IsNullOrEmpty(html)) return html;

            return data.Aggregate(html, (current, pair) =>
                current.Replace($"[{pair.Key}]", pair.Value));
        }
    }
}
