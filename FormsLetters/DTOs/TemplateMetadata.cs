namespace FormsLetters.DTOs;

public record TemplateMetadata
{
    public string OdooModel { get; init; } = string.Empty;
    public IEnumerable<string> RequiredFields { get; init; } = Enumerable.Empty<string>();
}
