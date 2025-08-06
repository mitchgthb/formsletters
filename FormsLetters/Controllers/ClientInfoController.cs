using FormsLetters.DTOs;
using FormsLetters.Services;
using FormsLetters.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace FormsLetters.Controllers;

[ApiController]
[Route("client-info")]
public class ClientInfoController : ControllerBase
{
    private readonly IClientInfoService _service;
    private readonly ITemplateMetadataService _metadataService;
    private readonly IOdooService _odooService;

    public ClientInfoController(IClientInfoService service, ITemplateMetadataService metadataService, IOdooService odooService)
    {
        _service = service;
        _metadataService = metadataService;
        _odooService = odooService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientInfoDto>>> GetAll(CancellationToken cancellationToken)
    {
        var list = await _service.GetClientsAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ClientInfoDto>> Get(int id, CancellationToken cancellationToken)
    {
        var client = await _service.GetClientAsync(id, cancellationToken);
        return client is null ? NotFound() : Ok(client);
    }

    [HttpGet("{id:int}/data")] // /client-info/{id}/data?template=name
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
}
