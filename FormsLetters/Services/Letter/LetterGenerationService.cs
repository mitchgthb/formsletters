using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FormsLetters.Config;
using FormsLetters.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FormsLetters.Services.Letter
{
    public interface ILetterGenerationService
    {
        Task<string> GenerateAsync(string templateName, int clientId, CancellationToken cancellationToken = default);
    }

    /// <summary>
    /// Generates a full PDF letter by assembling the HTML (via LetterAssemblerService) and converting it to PDF using LibreOffice.
    /// </summary>
    public class LetterGenerationService : ILetterGenerationService
    {
        private readonly ILetterAssemblerService _assembler;
        private readonly ISharePointService _storage; // existing abstraction used for templates & pdf saving
        private readonly ILogger<LetterGenerationService> _logger;
        private readonly string _soffice;

        public LetterGenerationService(ILetterAssemblerService assembler,
            ISharePointService storage,
            ILogger<LetterGenerationService> logger,
            IOptions<LibreOfficeOptions> options)
        {
            _assembler = assembler;
            _storage = storage;
            _logger = logger;
            _soffice = options.Value.ExecutablePath ?? "soffice";
        }

        public async Task<string> GenerateAsync(string templateName, int clientId, CancellationToken cancellationToken = default)
        {
            var html = await _assembler.AssembleHtmlAsync(templateName, clientId, cancellationToken);

            // 1. Write HTML to temp file
            var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
            Directory.CreateDirectory(tempDir);
            var tempHtml = Path.Combine(tempDir, "letter.html");
            await File.WriteAllTextAsync(tempHtml, html, cancellationToken);

            // 2. Convert to PDF via LibreOffice CLI
            RunLibreOfficeConvert(tempHtml, tempDir);
            var tempPdf = Path.ChangeExtension(tempHtml, ".pdf");
            if (!File.Exists(tempPdf))
                throw new FileNotFoundException("LibreOffice failed to produce PDF", tempPdf);

            var pdfBytes = await File.ReadAllBytesAsync(tempPdf, cancellationToken);
            var pdfFileName = $"letter_{clientId}_{Path.GetFileNameWithoutExtension(templateName)}.pdf";
            var pdfPath = await _storage.StorePdfAsync(pdfBytes, pdfFileName, cancellationToken);

            // cleanup
            TryDelete(tempHtml);
            TryDelete(tempPdf);
            TryDelete(tempDir, recursive: true);

            return pdfPath;
        }

        private void RunLibreOfficeConvert(string inputFile, string workingDir)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = _soffice,
                Arguments = $"--headless --convert-to pdf \"{inputFile}\" --outdir \"{workingDir}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = workingDir
            };
            using var proc = Process.Start(startInfo);
            proc?.WaitForExit();
            if (proc?.ExitCode != 0)
            {
                var err = proc?.StandardError.ReadToEnd();
                _logger.LogError("LibreOffice conversion failed: {Error}", err);
                throw new IOException($"LibreOffice failed with code {proc?.ExitCode}: {err}");
            }
        }

        private static void TryDelete(string path, bool recursive = false)
        {
            try
            {
                if (Directory.Exists(path) && recursive)
                    Directory.Delete(path, true);
                else if (File.Exists(path))
                    File.Delete(path);
            }
            catch { }
        }
    }
}
