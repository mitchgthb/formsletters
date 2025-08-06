namespace FormsLetters.DTOs;

public class WindowsPrepareDocumentRequest
{
    public string TemplateName { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public object? ClientData { get; set; }
}

public class WindowsSaveDocumentRequest
{
    public string HtmlContent { get; set; } = string.Empty;
}
