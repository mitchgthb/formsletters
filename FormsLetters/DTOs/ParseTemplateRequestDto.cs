namespace FormsLetters.DTOs;

public record ParseTemplateRequestDto
{
    public string TemplatePath { get; init; } = string.Empty;
}
