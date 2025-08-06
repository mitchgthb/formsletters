namespace FormsLetters.DTOs;

public record ParseTemplateResponseDto
{
    public string EditableHtml { get; init; } = string.Empty;
    public IEnumerable<string> Placeholders { get; init; } = Enumerable.Empty<string>();
    public IEnumerable<string>? MalformedPlaceholders { get; init; }
}
