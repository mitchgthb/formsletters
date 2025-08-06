namespace FormsLetters.DTOs;

public record CognitoWebhookDto
{
    public string FormId { get; init; } = string.Empty;
    public string SubmissionId { get; init; } = string.Empty;
    public IDictionary<string, object> Data { get; init; } = new Dictionary<string, object>();
}
