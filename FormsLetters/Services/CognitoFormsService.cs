using FormsLetters.DTOs;
using FormsLetters.Services.Interfaces;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;

namespace FormsLetters.Services;

public class CognitoFormsService : ICognitoFormsService
{
    private readonly ILogger<CognitoFormsService> _logger;
    private readonly IOdooService _odooService;
    private readonly IConfiguration _configuration;

    // Sample static form data (in real implementation, would come from Cognito Forms API)
    private static readonly List<FormDto> _forms = new List<FormDto>
    {
        new FormDto 
        { 
            Id = "volmacht-2025", 
            Name = "Volmacht 2025", 
            Description = "Power of Attorney form for tax representation",
            Url = "https://www.cognitoforms.com/your-account/Volmacht2025",
            IsActive = true,
            LastModified = DateTime.Now.AddDays(-30)
        },
        new FormDto 
        { 
            Id = "tax-prep-intake", 
            Name = "Tax Prep Intake", 
            Description = "Client intake form for tax preparation services",
            Url = "https://www.cognitoforms.com/your-account/TaxPrepIntake",
            IsActive = true,
            LastModified = DateTime.Now.AddDays(-15)
        },
        new FormDto 
        { 
            Id = "business-registration", 
            Name = "Business Registration", 
            Description = "New business entity registration form",
            Url = "https://www.cognitoforms.com/your-account/BusinessRegistration",
            IsActive = true,
            LastModified = DateTime.Now.AddDays(-45)
        },
        new FormDto 
        { 
            Id = "amendment-request", 
            Name = "Amendment Request", 
            Description = "Tax return amendment request form",
            Url = "https://www.cognitoforms.com/your-account/AmendmentRequest",
            IsActive = true,
            LastModified = DateTime.Now.AddDays(-10)
        }
    };

    public CognitoFormsService(ILogger<CognitoFormsService> logger, IOdooService odooService, IConfiguration configuration)
    {
        _logger = logger;
        _odooService = odooService;
        _configuration = configuration;
    }

    public async Task<string> GetPrefillUrlAsync(int clientId, string formId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Generating prefill URL for client {ClientId} and form {FormId}", clientId, formId);
        
        // Get client data to use for prefilling
        var client = await _odooService.GetClientAsync(clientId, cancellationToken);
        if (client == null)
        {
            throw new KeyNotFoundException($"Client with ID {clientId} not found");
        }

        // Find the requested form
        var form = await GetFormAsync(formId, cancellationToken);
        if (form == null)
        {
            throw new KeyNotFoundException($"Form with ID {formId} not found");
        }

        // In a real implementation, we'd use the Cognito Forms API to generate a prefill URL
        // Here we'll simulate it with query parameters
        var baseUrl = form.Url;
        var prefillUrl = $"{baseUrl}?prefill_Name={Uri.EscapeDataString(client.Name)}&" +
                        $"prefill_Email={Uri.EscapeDataString(client.Email ?? "")}&" +
                        $"prefill_ClientId={client.Id}";
        
        _logger.LogInformation("Generated prefill URL for client {ClientId}", clientId);
        return prefillUrl;
    }

    public async Task HandleSubmissionAsync(CognitoFormSubmissionDto dto, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Received Cognito submission {EntryId}", dto.EntryId);

        var client = new ClientDto
        {
            Id = 0, // assuming new; adjust if lookup by TaxID later
            Name = dto.Data.CompanyName,
            Email = dto.Data.Email
        };

        await _odooService.CreateOrUpdateClientAsync(client, cancellationToken);
        _logger.LogInformation("Pushed submission {EntryId} to Odoo", dto.EntryId);
    }
    
    public Task<IEnumerable<FormDto>> GetFormsAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving available forms");
        
        // In a real implementation, this would call the Cognito Forms API
        // For now, return the sample data
        return Task.FromResult<IEnumerable<FormDto>>(_forms.Where(f => f.IsActive).ToList());
    }

    public Task<FormDto> GetFormAsync(string id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Retrieving form {FormId}", id);
        
        // In a real implementation, this would call the Cognito Forms API
        // For now, return from the sample data
        var form = _forms.FirstOrDefault(f => f.Id.Equals(id, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(form);
    }
}
