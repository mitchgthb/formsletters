using FormsLetters.DTOs;
using FormsLetters.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;

namespace FormsLetters.Controllers;

[ApiController]
[Route("clients")]
public class ClientsController : ControllerBase
{
    private readonly IOdooService _odooService;

    public ClientsController(IOdooService odooService)
    {
        _odooService = odooService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientDto>>> GetClients(CancellationToken cancellationToken)
    {
        var clients = await _odooService.GetClientsAsync(cancellationToken);
        return Ok(clients);
    }
}
