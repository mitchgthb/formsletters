using DocumentFormat.OpenXml.Office2016.Drawing.ChartDrawing;
using FormsLetters.Config;
using FormsLetters.Services.Interfaces;
using FormsLetters.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

builder.Services.AddControllers();
builder.Services.AddMemoryCache();

// Domain services
builder.Services.Configure<SharePointOptions>(builder.Configuration.GetSection("SharePoint"));
builder.Services.Configure<FormsLetters.Config.LibreOfficeOptions>(builder.Configuration.GetSection("LibreOffice"));
// File storage configuration
builder.Services.Configure<FormsLetters.Config.FileStorageOptions>(builder.Configuration.GetSection("FileStorage"));

// Determine if we're running with local file storage before configuration below
var useLocal = builder.Configuration.GetValue<bool>("LocalStorage:Enabled");

// If we're using local storage and FileStorage.TemplatesPath isn't explicitly set,
// derive it from LocalStorage section so controllers have a valid path.
if (useLocal)
{
    var localRoot = builder.Configuration.GetValue<string>("LocalStorage:RootPath");
    var templatesFolder = builder.Configuration.GetValue<string>("LocalStorage:TemplatesFolder") ?? "Templates";
    var path = Path.Combine(localRoot ?? string.Empty, templatesFolder);
    builder.Services.PostConfigure<FormsLetters.Config.FileStorageOptions>(opts =>
    {
        if (string.IsNullOrWhiteSpace(opts.TemplatesPath))
        {
            opts.TemplatesPath = path;
        }
    });
}
builder.Services.AddHttpClient("graph");
builder.Services.AddScoped<FormsLetters.Services.Interfaces.IOdooService, FormsLetters.Services.OdooService>();

if (useLocal)
{
    builder.Services.Configure<FormsLetters.Config.LocalFileOptions>(builder.Configuration.GetSection("LocalStorage"));
    builder.Services.AddScoped<FormsLetters.Services.Interfaces.ISharePointService, FormsLetters.Services.LocalFileStorageService>();
}
else
{
    builder.Services.AddScoped<FormsLetters.Services.Interfaces.ISharePointService, FormsLetters.Services.SharePointService>();
}
builder.Services.AddScoped<FormsLetters.Services.Interfaces.IDocuSignService, FormsLetters.Services.DocuSignService>();
builder.Services.AddScoped<FormsLetters.Services.Interfaces.ICognitoFormsService, FormsLetters.Services.CognitoFormsService>();
builder.Services.AddScoped<IDocumentGenerationService, DocumentGenerationService>();
// Letter generation pipeline
builder.Services.AddSingleton<FormsLetters.Services.Odoo.IOdooDataService, FormsLetters.Services.Odoo.OdooDataService>();
// Client info JSON service
builder.Services.AddSingleton<FormsLetters.Services.IClientInfoService, FormsLetters.Services.ClientInfoService>();
builder.Services.AddTransient<FormsLetters.Services.Letter.HeaderBuilder>();
builder.Services.AddTransient<FormsLetters.Services.Letter.EndingBuilder>();
builder.Services.AddTransient<FormsLetters.Services.Letter.ILetterAssemblerService, FormsLetters.Services.Letter.LetterAssemblerService>();
builder.Services.AddScoped<FormsLetters.Services.Letter.ILetterGenerationService, FormsLetters.Services.Letter.LetterGenerationService>();
builder.Services.AddScoped<ISharePointService, LocalFileStorageService>();
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://10.2.0.2:3000", "http://localhost:3000", "http://localhost:3001", "http://localhost:9980")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();

    });
});
// Email service
if (useLocal)
{
    builder.Services.AddScoped<FormsLetters.Services.Interfaces.IEmailService, FormsLetters.Services.LocalEmailService>();
}

builder.Services.AddScoped<FormsLetters.Services.Interfaces.ITemplateParsingService, FormsLetters.Services.TemplateParsingService>();
builder.Services.AddScoped<FormsLetters.Services.Interfaces.IDocumentGenerationService, FormsLetters.Services.DocumentGenerationService>();
builder.Services.AddScoped<FormsLetters.Services.Interfaces.ITemplateMetadataService, FormsLetters.Services.TemplateMetadataService>();

// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<FormsLetters.Middleware.ErrorHandlingMiddleware>();

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthorization();

app.MapControllers();

app.Run();
