namespace FormsLetters.Config;

public class LocalFileOptions
{
    // Root directory where test templates and generated PDFs live, e.g. c:\Files\FormsLetters
    public string RootPath { get; set; } = "Files";

    // Subfolder names
    public string TemplatesFolder { get; set; } = "Templates";
    public string GeneratedFolder { get; set; } = "Generated";
}
