namespace FormsLetters.DTOs;

public record TemplateDataResponseDto
{
    public IDictionary<string, object?> Data { get; init; } = new Dictionary<string, object?>();
}
