namespace FormsLetters.Config;

public class SharePointOptions
{
    public string SiteId { get; set; } = string.Empty;
    public string DriveId { get; set; } = string.Empty; // the document library drive id
    public string AccessToken { get; set; } = string.Empty; // temporary dev token; replace with Entra auth flow
}
