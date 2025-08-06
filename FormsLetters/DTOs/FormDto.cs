using System;

namespace FormsLetters.DTOs;

public class FormDto
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Url { get; set; }
    public DateTime? LastModified { get; set; }
    public bool IsActive { get; set; } = true;
}

public class PrefillFormRequestDto
{
    public int ClientId { get; set; }
    public string FormId { get; set; }
}

public class PrefillFormResponseDto
{
    public string PrefillUrl { get; set; }
}
