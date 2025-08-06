namespace FormsLetters.DTOs
{
    public record GenerateLetterRequestDto
    {
        public string TemplateName { get; init; } = string.Empty;
        public int ClientId { get; init; }
    }
}
