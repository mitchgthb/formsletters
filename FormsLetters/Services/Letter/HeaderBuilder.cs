using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FormsLetters.Services.Letter
{
    public class HeaderBuilder : ITemplateSectionBuilder
    {
        private readonly ILogger<HeaderBuilder> _logger;
        public HeaderBuilder(ILogger<HeaderBuilder> logger)
        {
            _logger = logger;
        }

        // Very simple string interpolation for now; can switch to Razor later.
        public Task<string> BuildAsync(Dictionary<string, string> data, CancellationToken cancellationToken = default)
        {
            var sb = new StringBuilder();
            sb.Append("<div style=\"margin-bottom:20px;font-family:Arial, sans-serif;\">");
            sb.Append($"<h2 style=\"margin:0\">{data.GetValueOrDefault("company_name", string.Empty)}</h2>");
            sb.Append($"<p style=\"margin:0\">{data.GetValueOrDefault("address_inline", string.Empty)}</p>");
            sb.Append($"<p style=\"margin:0\">Tax Nr: {data.GetValueOrDefault("tax_number", string.Empty)}</p>");
            sb.Append("<hr></div>");
            var html = sb.ToString();
            _logger.LogDebug("Generated header HTML: {Html}", html);
            return Task.FromResult(html);
        }
    }
}
