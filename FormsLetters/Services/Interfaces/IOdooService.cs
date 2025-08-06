using FormsLetters.DTOs;

namespace FormsLetters.Services.Interfaces;

public interface IOdooService
{
    Task<IEnumerable<ClientDto>> GetClientsAsync(CancellationToken cancellationToken = default);
    Task<ClientDto?> GetClientAsync(int id, CancellationToken cancellationToken = default);
    Task<IDictionary<string, object?>> GetFieldsAsync(string model, int id, IEnumerable<string> fields, CancellationToken cancellationToken = default);
    Task CreateOrUpdateClientAsync(ClientDto client, CancellationToken cancellationToken = default);
}
