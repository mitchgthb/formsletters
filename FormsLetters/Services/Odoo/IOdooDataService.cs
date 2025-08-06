using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FormsLetters.Services.Odoo
{
    /// <summary>
    /// Fetches all required data from Odoo needed to assemble a letter.
    /// Returns a simple key->value dictionary so upstream components can just substitute placeholders.
    /// </summary>
    public interface IOdooDataService
    {
        /// <summary>
        /// Fetches the data dictionary for a client record.
        /// </summary>
        /// <param name="clientId">Client identifier in Odoo.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>Dictionary where key is the placeholder (e.g. "company_name") and value is the actual string.</returns>
        Task<Dictionary<string, string>> GetLetterDataAsync(int clientId, CancellationToken cancellationToken = default);
    }
}
