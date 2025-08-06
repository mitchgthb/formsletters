namespace FormsLetters.Config;

public class LibreOfficeOptions
{
    public string ExecutablePath { get; set; } = GetDefaultLibreOfficePath();

    private static string GetDefaultLibreOfficePath()
    {
        // Check if running in Docker (common indicator)
        if (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" ||
            File.Exists("/.dockerenv"))
        {
            return "soffice"; // Linux path in Docker
        }

        // Default paths for different operating systems
        var windowsPath = @"C:\Program Files\LibreOffice\program\soffice.exe";
        var windowsPath2 = @"C:\Program Files (x86)\LibreOffice\program\soffice.exe";
        
        // Check if running on Windows
        if (OperatingSystem.IsWindows())
        {
            if (File.Exists(windowsPath))
                return windowsPath;
            if (File.Exists(windowsPath2))
                return windowsPath2;
        }
        
        // Fallback to 'soffice' command (works on Linux/Docker and Windows if in PATH)
        return "soffice";
    }
}
