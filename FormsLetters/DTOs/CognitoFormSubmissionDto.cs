namespace FormsLetters.DTOs;

public record CognitoFormSubmissionDto
{
    public long EntryId { get; init; }
    public DateTime DateCreated { get; init; }
    public DataSection Data { get; init; } = new();

    public record DataSection
    {
        public string CompanyName { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
        public string TaxID { get; init; } = string.Empty;
        public string ContactName { get; init; } = string.Empty;
        public string PhoneNumber { get; init; } = string.Empty;
        public AddressSection Address { get; init; } = new();
    }

    public record AddressSection
    {
        public string Line1 { get; init; } = string.Empty;
        public string City { get; init; } = string.Empty;
        public string Zip { get; init; } = string.Empty;
    }
}
