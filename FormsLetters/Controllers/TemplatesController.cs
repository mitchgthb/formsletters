using FormsLetters.DTOs;
using FormsLetters.Services.Interfaces;
using Microsoft.AspNetCore.Mvc;

using FormsLetters.Config;
using Microsoft.Extensions.Options;

namespace FormsLetters.Controllers;

[ApiController]
[Route("templates")]
public class TemplatesController : ControllerBase
{
    private readonly ISharePointService _sharePointService;
    private readonly string _templatesPath;

    public TemplatesController(ISharePointService sharePointService, IOptions<FileStorageOptions> storageOptions)
    {
        _sharePointService = sharePointService;
        _templatesPath = storageOptions.Value.TemplatesPath;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<TemplateDto>>> GetTemplates(CancellationToken cancellationToken)
    {
        var templatesRoot = _templatesPath;

        Console.WriteLine($"TemplatesPath: {_templatesPath}");
        Console.WriteLine($"Files: {string.Join(", ", Directory.GetFiles(_templatesPath))}");

        if (!Directory.Exists(templatesRoot))
        {
            return Ok(new List<TemplateDto>()); // Could also return NotFound()
        }

        var files = Directory.GetFiles(templatesRoot, "*.docx");
        var list = files
            .Select(f => new TemplateDto
            {
                Name = Path.GetFileName(f),
                Path = f
            })
            .ToList();

        return Ok(list); // Confirm in Postman or browser
    }

    [HttpPost("parse-template")]
    public async Task<ActionResult<ParseTemplateResponseDto>> ParseTemplate([FromBody] ParseTemplateRequestDto request,
        [FromServices] ITemplateParsingService parser,
        CancellationToken cancellationToken)
    {
        Console.WriteLine("TEMPLATE PATH:" + request.TemplatePath);
        // TODO: acquire Entra ID token in future and pass to SharePointService
        
        byte[] bytes;
        if (System.IO.File.Exists(request.TemplatePath))
        {
            bytes = await System.IO.File.ReadAllBytesAsync(request.TemplatePath, cancellationToken);
        }
        else
        {
            bytes = await _sharePointService.GetTemplateBytesAsync(request.TemplatePath, cancellationToken);
        }
        if (bytes.Length == 0)
        {
            return NotFound("Template not found or empty");
        }
        var result = await parser.ParseAsync(bytes, cancellationToken);
        return Ok(result);
    }

    [HttpPost("upload")]
    public async Task<ActionResult<TemplateDto>> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest("No file uploaded.");

        // Save to Templates folder
        var templatesRoot = _templatesPath;
        if (!Directory.Exists(templatesRoot))
            Directory.CreateDirectory(templatesRoot);
        var filePath = Path.Combine(templatesRoot, file.FileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

        var dto = new TemplateDto
        {
            Name = file.FileName,
            Path = filePath
        };

        return Ok(dto);
    }
}
