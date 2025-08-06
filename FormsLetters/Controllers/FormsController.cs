using FormsLetters.DTOs;
using FormsLetters.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FormsLetters.Controllers;

[ApiController]
[Route("forms")]
public class FormsController : ControllerBase
{
    private readonly ICognitoFormsService _cognitoFormsService;
    private readonly ILogger<FormsController> _logger;

    public FormsController(ICognitoFormsService cognitoFormsService, ILogger<FormsController> logger)
    {
        _cognitoFormsService = cognitoFormsService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<FormDto>>> GetForms(CancellationToken cancellationToken)
    {
        try
        {
            var forms = await _cognitoFormsService.GetFormsAsync(cancellationToken);
            return Ok(forms);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving forms");
            return StatusCode(500, "An error occurred while retrieving forms");
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<FormDto>> GetForm(string id, CancellationToken cancellationToken)
    {
        try
        {
            var form = await _cognitoFormsService.GetFormAsync(id, cancellationToken);
            if (form == null)
            {
                return NotFound();
            }
            return Ok(form);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving form {FormId}", id);
            return StatusCode(500, $"An error occurred while retrieving form {id}");
        }
    }

    [HttpPost("prefill")]
    public async Task<ActionResult<PrefillFormResponseDto>> PrefillForm([FromBody] PrefillFormRequestDto request, CancellationToken cancellationToken)
    {
        try
        {
            var prefillUrl = await _cognitoFormsService.GetPrefillUrlAsync(request.ClientId, request.FormId, cancellationToken);
            return Ok(new PrefillFormResponseDto { PrefillUrl = prefillUrl });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error generating prefill URL for client {ClientId} and form {FormId}", request.ClientId, request.FormId);
            return StatusCode(500, "An error occurred while generating the prefilled form URL");
        }
    }
}
