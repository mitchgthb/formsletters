using FormsLetters.DTOs;
using FormsLetters.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Net.Http;
using System.Net.Http.Headers;
using Microsoft.Extensions.Options;
using FormsLetters.Config;

namespace FormsLetters.Services;

public class SharePointService : ISharePointService
{
    private readonly ILogger<SharePointService> _logger;
    private readonly HttpClient _http;
    private readonly SharePointOptions _options;

    public SharePointService(ILogger<SharePointService> logger, IHttpClientFactory httpFactory, IOptions<SharePointOptions> options)
    {
        _logger = logger;
        _http = httpFactory.CreateClient("graph");
        _options = options.Value;
    }

    public async Task<IEnumerable<TemplateDto>> GetTemplatesAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching templates from SharePoint");
        await EnsureAuthHeaderAsync();
        var url = $"https://graph.microsoft.com/v1.0/sites/{_options.SiteId}/drives/{_options.DriveId}/root/children";
        var res = await _http.GetFromJsonAsync<GraphListResponse>(url, cancellationToken);
        var templates = res?.Value.Where(i => i.File != null)
            .Select(i => new TemplateDto { Name = i.Name, Path = i.Name })
            .ToList() ?? new List<TemplateDto>();
        return templates;
    }

    public async Task<byte[]> GetTemplateBytesAsync(string path, CancellationToken cancellationToken = default)
    {
        await EnsureAuthHeaderAsync();
        var url = $"https://graph.microsoft.com/v1.0/sites/{_options.SiteId}/drives/{_options.DriveId}/root:/{path}:/content";
        _logger.LogInformation("Downloading template {Path}", path);
        var bytes = await _http.GetByteArrayAsync(url, cancellationToken);
        return bytes;
    }

    public async Task<string> StorePdfAsync(byte[] pdfBytes, string fileName, CancellationToken cancellationToken = default)
    {
        await EnsureAuthHeaderAsync();
        var url = $"https://graph.microsoft.com/v1.0/sites/{_options.SiteId}/drives/{_options.DriveId}/root:/Generated/{fileName}:/content";
        _logger.LogInformation("Uploading PDF {File}", fileName);
        using var content = new ByteArrayContent(pdfBytes);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        var res = await _http.PutAsync(url, content, cancellationToken);
        res.EnsureSuccessStatusCode();
        return $"/Generated/{fileName}";
    }

    private Task EnsureAuthHeaderAsync()
    {
        if (!_http.DefaultRequestHeaders.Contains("Authorization"))
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.AccessToken);
        }
        return Task.CompletedTask;
    }

    private class GraphListResponse
    {
        public List<Item> Value { get; set; } = new();
    }
    private class Item
    {
        public string Name { get; set; } = string.Empty;
        public FileFacet? File { get; set; }
    }
    private class FileFacet { }
}
