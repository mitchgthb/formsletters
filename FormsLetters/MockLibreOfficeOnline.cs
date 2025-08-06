using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace FormsLetters;

/// <summary>
/// Mock LibreOffice Online server for development when Docker Linux containers are not available
/// This provides the basic endpoints needed for testing the integration
/// </summary>
[ApiController]
[Route("mock-libreoffice")]
public class MockLibreOfficeOnlineController : ControllerBase
{
    private readonly ILogger<MockLibreOfficeOnlineController> _logger;

    public MockLibreOfficeOnlineController(ILogger<MockLibreOfficeOnlineController> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Mock hosting capabilities endpoint
    /// </summary>
    [HttpGet("hosting/capabilities")]
    public IActionResult GetCapabilities()
    {
        var capabilities = new
        {
            convert_to = new
            {
                available = true,
                formats = new[] { "pdf", "docx", "odt", "html" }
            },
            hasMobileSupport = true,
            hasProxyPrefix = false,
            hasSSLTermination = false,
            productName = "Mock LibreOffice Online",
            productVersion = "1.0.0",
            productVersionHash = "mock123",
            ui_defaults = new
            {
                UIMode = "classic"
            }
        };

        return Ok(capabilities);
    }

    /// <summary>
    /// Mock LOOL/Cool viewer endpoint
    /// This would normally serve the LibreOffice Online editor interface
    /// </summary>
    [HttpGet("lool/{fileId:guid}")]
    public IActionResult GetViewer(Guid fileId, [FromQuery] string WOPISrc)
    {
        _logger.LogInformation("Mock LibreOffice viewer requested for file {FileId}", fileId);

        // Return a simple HTML page that simulates the LibreOffice Online interface
        var html = $@"
<!DOCTYPE html>
<html>
<head>
    <title>Mock LibreOffice Online Editor</title>
    <style>
        body {{ 
            font-family: Arial, sans-serif; 
            margin: 0; 
            padding: 20px; 
            background: #f5f5f5;
        }}
        .editor-container {{
            background: white;
            border: 1px solid #ddd;
            border-radius: 4px;
            padding: 20px;
            min-height: 400px;
            box-shadow: 0 2px 4px rgba(0,0,0,0.1);
        }}
        .toolbar {{
            background: #f8f9fa;
            border-bottom: 1px solid #dee2e6;
            padding: 10px;
            margin: -20px -20px 20px -20px;
            border-radius: 4px 4px 0 0;
        }}
        .btn {{
            background: #007bff;
            color: white;
            border: none;
            padding: 8px 16px;
            margin-right: 10px;
            border-radius: 4px;
            cursor: pointer;
        }}
        .btn:hover {{ background: #0056b3; }}
        .content-area {{
            border: 1px solid #ced4da;
            min-height: 300px;
            padding: 15px;
            background: white;
            font-family: 'Times New Roman', serif;
            line-height: 1.6;
        }}
        .status {{ 
            color: #6c757d; 
            font-size: 0.9em; 
            margin-top: 15px;
        }}
    </style>
</head>
<body>
    <div class='editor-container'>
        <div class='toolbar'>
            <button class='btn' onclick='saveDocument()'>💾 Save</button>
            <button class='btn' onclick='generatePdf()'>📄 Generate PDF</button>
            <button class='btn' onclick='closeEditor()'>❌ Close</button>
        </div>
        <div class='content-area' contenteditable='true' id='documentContent'>
            <h2>Mock LibreOffice Online Editor</h2>
            <p>This is a development mock of LibreOffice Online.</p>
            <p>File ID: {fileId}</p>
            <p>WOPI Source: {WOPISrc ?? "Not provided"}</p>
            <p>You can edit this content as if it were a real LibreOffice document.</p>
            <br>
            <p><strong>Template Content:</strong></p>
            <p>Dear [Client Name],</p>
            <p>This letter is generated from template with ID: {fileId}</p>
            <p>Content controls and formatting would be available in the real LibreOffice Online.</p>
        </div>
        <div class='status'>
            Status: Connected to Mock LibreOffice Online | Auto-save: Enabled
        </div>
    </div>

    <script>
        function saveDocument() {{
            const content = document.getElementById('documentContent').innerHTML;
            console.log('Saving document content:', content);
            
            // Simulate saving to parent window
            if (window.parent) {{
                window.parent.postMessage({{
                    MessageId: 'App_LoadingStatus',
                    Values: {{ Status: 'Document_Saved' }}
                }}, '*');
            }}
            
            alert('Document saved successfully!');
        }}

        function generatePdf() {{
            console.log('Generating PDF...');
            
            // Simulate PDF generation
            if (window.parent) {{
                window.parent.postMessage({{
                    MessageId: 'Action_GeneratePdf',
                    Values: {{ FileId: '{fileId}' }}
                }}, '*');
            }}
            
            alert('PDF generation requested!');
        }}

        function closeEditor() {{
            if (window.parent) {{
                window.parent.postMessage({{
                    MessageId: 'App_LoadingStatus',
                    Values: {{ Status: 'Document_Closed' }}
                }}, '*');
            }}
            
            window.close();
        }}

        // Simulate document loading
        setTimeout(() => {{
            if (window.parent) {{
                window.parent.postMessage({{
                    MessageId: 'App_LoadingStatus',
                    Values: {{ Status: 'Document_Loaded' }}
                }}, '*');
            }}
        }}, 1000);

        // Listen for messages from parent
        window.addEventListener('message', function(event) {{
            console.log('Mock LibreOffice received message:', event.data);
        }});
    </script>
</body>
</html>";

        return Content(html, "text/html");
    }

    /// <summary>
    /// Mock discovery endpoint
    /// </summary>
    [HttpGet("hosting/discovery")]
    public IActionResult GetDiscovery()
    {
        var discovery = new
        {
            BaseUrl = "http://localhost:5000/mock-libreoffice/",
            Actions = new[]
            {
                new
                {
                    name = "view",
                    ext = "docx",
                    urlsrc = "http://localhost:5000/mock-libreoffice/lool/<ui=UI_LLCC&><rs=DC_LLCC&><dchat=DISABLE_CHAT&><embed=EMBEDDED&><permission=PERMISSION&><navbarheader=HEADER&><integratorhostpage=INTEGRATOR_HOST_PAGE&>WOPISrc=<WOPISrc>"
                },
                new
                {
                    name = "edit",
                    ext = "docx",
                    urlsrc = "http://localhost:5000/mock-libreoffice/lool/<ui=UI_LLCC&><rs=DC_LLCC&><dchat=DISABLE_CHAT&><embed=EMBEDDED&><permission=PERMISSION&><navbarheader=HEADER&><integratorhostpage=INTEGRATOR_HOST_PAGE&>WOPISrc=<WOPISrc>"
                }
            }
        };

        return Ok(discovery);
    }
}
