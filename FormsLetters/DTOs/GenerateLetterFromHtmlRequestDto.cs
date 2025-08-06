namespace FormsLetters.DTOs
{
    public record GenerateLetterFromHtmlRequestDto
    {
        public string TemplateName { get; init; } = string.Empty;
        public int ClientId { get; init; }
        public string UpdatedHtml { get; init; } = string.Empty;
    }
}
