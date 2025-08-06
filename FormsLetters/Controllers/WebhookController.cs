using FormsLetters.DTOs;
using FormsLetters.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FormsLetters.Controllers;

[ApiController]
[Route("webhook")]
public class WebhookController : ControllerBase
{
    private readonly ICognitoFormsService _cognitoFormsService;

    public WebhookController(ICognitoFormsService cognitoFormsService)
    {
        _cognitoFormsService = cognitoFormsService;
    }

    [HttpPost("cognito")]
    public async Task<IActionResult> Cognito([FromBody] CognitoFormSubmissionDto dto, CancellationToken cancellationToken)
    {
        await _cognitoFormsService.HandleSubmissionAsync(dto, cancellationToken);
        return Ok();
    }
}
