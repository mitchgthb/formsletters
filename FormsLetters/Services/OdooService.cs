using FormsLetters.DTOs;
using FormsLetters.Services.Interfaces;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace FormsLetters.Services;

public class OdooService : IOdooService
{
    private readonly ILogger<OdooService> _logger;
    private readonly IWebHostEnvironment _env;
    private List<ClientInfoDto>? _clientCache;

    public OdooService(ILogger<OdooService> logger, IWebHostEnvironment env)
    {
        _logger = logger;
        _env = env;
    }

    private async Task EnsureClientsLoadedAsync(CancellationToken cancellationToken)
    {
        if (_clientCache != null) return;
        
        var jsonPath = Path.Combine(_env.ContentRootPath, "ClientsData.json");
        if (!File.Exists(jsonPath))
        {
            _logger.LogWarning("ClientsData.json not found at {Path}", jsonPath);
            _clientCache = new List<ClientInfoDto>();
            return;
        }
        
        await using var stream = File.OpenRead(jsonPath);
        var options = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        _clientCache = await JsonSerializer.DeserializeAsync<List<ClientInfoDto>>(stream, options, cancellationToken) ?? new();
        _logger.LogInformation("Loaded {Count} clients from seed data", _clientCache.Count);
    }

    public async Task<IEnumerable<ClientDto>> GetClientsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching clients from seed data (temporary replacement for Odoo)");
        await EnsureClientsLoadedAsync(cancellationToken);
        
        // Convert ClientInfoDto to ClientDto
        return _clientCache!.Select(c => new ClientDto
        {
            Id = c.Id,
            Name = c.Name,
            Email = c.Email
        });
    }

    public async Task<ClientDto?> GetClientAsync(int id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching client {Id} from seed data", id);
        await EnsureClientsLoadedAsync(cancellationToken);
        
        var clientInfo = _clientCache!.FirstOrDefault(c => c.Id == id);
        if (clientInfo == null) return null;
        
        return new ClientDto
        {
            Id = clientInfo.Id,
            Name = clientInfo.Name,
            Email = clientInfo.Email
        };
    }

    public async Task<IDictionary<string, object?>> GetFieldsAsync(string model, int id, IEnumerable<string> fields, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Getting fields for model {Model}, id {Id} from seed data", model, id);
        await EnsureClientsLoadedAsync(cancellationToken);
        
        var clientInfo = _clientCache!.FirstOrDefault(c => c.Id == id);
        if (clientInfo == null)
        {
            return new Dictionary<string, object?>();
        }
        
        // Map fields from ClientInfoDto properties
        var dict = new Dictionary<string, object?>();
        foreach (var field in fields)
        {
            switch (field.ToLowerInvariant())
            {
                case "name":
                    dict[field] = clientInfo.Name;
                    break;
                case "email":
                    dict[field] = clientInfo.Email;
                    break;
                case "address":
                    dict[field] = clientInfo.Address;
                    break;
                case "taxnumber":
                case "tax_number":
                    dict[field] = clientInfo.TaxNumber;
                    break;
                default:
                    dict[field] = $"dummy_{field}_{id}";
                    break;
            }
        }
        return dict;
    }

    public Task CreateOrUpdateClientAsync(ClientDto client, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating/updating client {Name} (stub)", client.Name);
        return Task.CompletedTask;
    }
}
