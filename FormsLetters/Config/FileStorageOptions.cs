namespace FormsLetters.Config;

/// <summary>
/// Simple options record to hold paths for file-based storage (templates, generated PDFs, etc.).
/// This is bound from the **FileStorage** section in appsettings (or environment variables via
/// the double-underscore syntax, e.g. FileStorage__TemplatesPath).
/// </summary>
public class FileStorageOptions
{
    /// <summary>
    /// Full absolute path where uploaded / existing Word templates are stored.
    /// When running in a Docker container this should point to a path that is volume-mounted
    /// from the host so that files persist.
    /// </summary>
    public string TemplatesPath { get; set; } = "/app/Templates";
}
