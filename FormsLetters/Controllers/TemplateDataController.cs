using FormsLetters.DTOs;
using FormsLetters.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FormsLetters.Controllers;

[ApiController]
[Route("clients")] // base route
public class TemplateDataController : ControllerBase
{
    private readonly ITemplateMetadataService _metadataService;
    private readonly IOdooService _odooService;

    public TemplateDataController(ITemplateMetadataService metadataService, IOdooService odooService)
    {
        _metadataService = metadataService;
        _odooService = odooService;
    }

    [HttpGet("{id:int}/data")] // /clients/{id}/data?template=name
    public async Task<ActionResult<TemplateDataResponseDto>> GetData(int id, [FromQuery] string template, CancellationToken cancellationToken)
    {
        var meta = await _metadataService.GetMetadataAsync(template, cancellationToken);
        if (meta is null)
        {
            return NotFound($"Template metadata for '{template}' not found");
        }

        var dict = await _odooService.GetFieldsAsync(meta.OdooModel, id, meta.RequiredFields, cancellationToken);
        var response = new TemplateDataResponseDto { Data = dict };
        return Ok(response);
    }

    // Bonus: expose full mapping list
    [HttpGet("template-metadata")]
    public async Task<ActionResult<IDictionary<string, TemplateMetadata>>> GetAllMetadata(CancellationToken cancellationToken)
    {
        var all = await _metadataService.GetAllAsync(cancellationToken);
        return Ok(all);
    }
}
