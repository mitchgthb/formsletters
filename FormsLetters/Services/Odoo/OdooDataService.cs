using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace FormsLetters.Services.Odoo
{
    /// <summary>
    /// Temporary stub that returns hard-coded demo data until the real JSON-RPC Odoo integration is ready.
    /// </summary>
    public class OdooDataService : IOdooDataService
    {
        private readonly ILogger<OdooDataService> _logger;

        public OdooDataService(ILogger<OdooDataService> logger)
        {
            _logger = logger;
        }

        public Task<Dictionary<string, string>> GetLetterDataAsync(int clientId, CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Fetching letter data from Odoo for client {ClientId}", clientId);

            // TODO: Replace with actual Odoo API calls.
            var data = new Dictionary<string, string>
            {
                ["company_name"] = "ACME Ltd.",
                ["client_name"] = "John Doe",
                ["address_inline"] = "123 Main St, Gotham",
                ["email"] = "john.doe@example.com",
                ["employee_name"] = "Jane Smith",
                ["tax_number"] = "DE123456789",
                ["tariffs"] = "Standard",
                ["project_ids"] = "PRJ-001, PRJ-002"
            };

            return Task.FromResult(data);
        }
    }
}
