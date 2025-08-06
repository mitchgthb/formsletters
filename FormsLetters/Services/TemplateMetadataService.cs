using System.Text.Json;
using FormsLetters.DTOs;
using FormsLetters.Services.Interfaces;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace FormsLetters.Services;

public class TemplateMetadataService : ITemplateMetadataService
{
    private readonly ILogger<TemplateMetadataService> _logger;
    private readonly IMemoryCache _cache;
    private readonly IWebHostEnvironment _env;
    private const string CacheKey = "TemplateMetadataCache";

    public TemplateMetadataService(ILogger<TemplateMetadataService> logger, IMemoryCache cache, IWebHostEnvironment env)
    {
        _logger = logger;
        _cache = cache;
        _env = env;
    }

    public async Task<TemplateMetadata?> GetMetadataAsync(string templateName, CancellationToken cancellationToken = default)
    {
        var all = await GetAllAsync(cancellationToken);
        
        // Extract just the filename from the templateName in case it includes a path
        var fileName = Path.GetFileName(templateName);
        
        all.TryGetValue(fileName, out var meta);
        return meta;
    }

    public async Task<IDictionary<string, TemplateMetadata>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(CacheKey, out IDictionary<string, TemplateMetadata> cached))
        {
            return cached;
        }

        var path = Path.Combine(_env.ContentRootPath, "Config", "template-metadata.json");
        if (!File.Exists(path))
        {
            _logger.LogWarning("Template metadata file not found at {Path}", path);
            return new Dictionary<string, TemplateMetadata>();
        }

        await using var stream = File.OpenRead(path);
        var metadata = await JsonSerializer.DeserializeAsync<Dictionary<string, TemplateMetadata>>(stream, cancellationToken: cancellationToken) 
                       ?? new Dictionary<string, TemplateMetadata>();

        _cache.Set(CacheKey, metadata, TimeSpan.FromMinutes(10));
        return metadata;
    }
}
