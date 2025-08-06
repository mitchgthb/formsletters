namespace FormsLetters.DTOs;

public record ClientInfoDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string TaxNumber { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
}
