using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FormsLetters.Services.Letter
{
    public class EndingBuilder : ITemplateSectionBuilder
    {
        private readonly ILogger<EndingBuilder> _logger;
        public EndingBuilder(ILogger<EndingBuilder> logger)
        {
            _logger = logger;
        }

        public Task<string> BuildAsync(Dictionary<string, string> data, CancellationToken cancellationToken = default)
        {
            var sb = new StringBuilder();
            sb.Append("<hr><div style=\"margin-top:20px;font-family:Arial, sans-serif;\">");
            sb.Append($"<p>Kind regards,</p><p>{data.GetValueOrDefault("employee_name", string.Empty)}</p>");
            sb.Append("</div>");
            var html = sb.ToString();
            _logger.LogDebug("Generated ending HTML: {Html}", html);
            return Task.FromResult(html);
        }
    }
}
