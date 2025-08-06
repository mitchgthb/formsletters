using System.Text.Json;
using FormsLetters.DTOs;
using Microsoft.Extensions.Logging;

namespace FormsLetters.Services;

public interface IClientInfoService
{
    Task<IEnumerable<ClientInfoDto>> GetClientsAsync(CancellationToken cancellationToken = default);
    Task<ClientInfoDto?> GetClientAsync(int id, CancellationToken cancellationToken = default);
}

public class ClientInfoService : IClientInfoService
{
    private readonly ILogger<ClientInfoService> _logger;
    private readonly string _jsonPath;
    private List<ClientInfoDto>? _cache;

    public ClientInfoService(ILogger<ClientInfoService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _jsonPath = Path.Combine(env.ContentRootPath, "ClientsData.json");
    }

    private async Task EnsureLoadedAsync(CancellationToken cancellationToken)
    {
        if (_cache != null) return;
        if (!File.Exists(_jsonPath))
        {
            _logger.LogWarning("ClientsData.json not found at {Path}", _jsonPath);
            _cache = new List<ClientInfoDto>();
            return;
        }
        await using var stream = File.OpenRead(_jsonPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _cache = await JsonSerializer.DeserializeAsync<List<ClientInfoDto>>(stream, options, cancellationToken) ?? new();
    }

    public async Task<IEnumerable<ClientInfoDto>> GetClientsAsync(CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return _cache!;
    }

    public async Task<ClientInfoDto?> GetClientAsync(int id, CancellationToken cancellationToken = default)
    {
        await EnsureLoadedAsync(cancellationToken);
        return _cache!.FirstOrDefault(c => c.Id == id);
    }
}
