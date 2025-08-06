namespace FormsLetters.DTOs;

public record TemplateDto
{
    public string Name { get; init; } = string.Empty;
    public string Path { get; init; } = string.Empty; // SharePoint path or ID
}
