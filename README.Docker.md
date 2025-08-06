# 🐳 Docker Configuration for LibreOffice PDF Generation

This document explains how to run the FormsLetters application with LibreOffice PDF generation in Docker.

## Quick Start

### Option 1: Docker Compose (Recommended)
```bash
docker compose up --build
```

### Option 2: Manual Docker Build
```bash
docker build -t formsletters .
docker run -p 8080:8080 -p 8081:8081 formsletters
```

Your application will be available at http://localhost:8080.

## LibreOffice Integration

The application automatically detects when running in Docker and configures LibreOffice accordingly:

### Docker Environment Features:
- ✅ **LibreOffice Installed**: Alpine Linux with LibreOffice package
- ✅ **Font Support**: Liberation and DejaVu fonts for better PDF rendering  
- ✅ **Java Runtime**: OpenJDK 11 for LibreOffice functionality
- ✅ **Temp Directory**: Optimized `/tmp/libreoffice` directory
- ✅ **Environment Detection**: Auto-configures paths and settings

### Configuration

The application uses different LibreOffice paths based on environment:

| Environment | LibreOffice Path |
|-------------|------------------|
| Docker/Linux | `soffice` (in PATH) |
| Windows Local | `C:\Program Files\LibreOffice\program\soffice.exe` |

### Troubleshooting

#### LibreOffice Not Found
If you see errors like "LibreOffice executable not found":

1. **Rebuild the container**: `docker compose up --build`
2. **Check the logs**: `docker compose logs`
3. **Verify installation**: 
   ```bash
   docker exec -it <container> soffice --version
   ```

#### PDF Generation Fails
1. **Check permissions**: Ensure temp directories are writable
2. **Memory limits**: LibreOffice needs sufficient memory (>512MB recommended)
3. **Font issues**: Verify fonts are installed in container

## Local Development

### Running Locally (Without Docker)
1. **Install LibreOffice**: Download from [libreoffice.org](https://www.libreoffice.org/download/download/)
2. **Configure Path**: Update `appsettings.Development.json`:
   ```json
   {
     "LibreOffice": {
       "ExecutablePath": "C:\\Program Files\\LibreOffice\\program\\soffice.exe"
     }
   }
   ```
3. **Run Application**: `dotnet run`

### Mixed Environment
- **Frontend**: Running locally (Node.js/Next.js)  
- **Backend**: Running in Docker

Update `.env.local` in the frontend:
```bash
NEXT_PUBLIC_API_BASE_URL=http://localhost:8080
```

### Deploying your application to the cloud

First, build your image, e.g.: `docker build -t myapp .`.
If your cloud uses a different CPU architecture than your development
machine (e.g., you are on a Mac M1 and your cloud provider is amd64),
you'll want to build the image for that platform, e.g.:
`docker build --platform=linux/amd64 -t myapp .`.

Then, push it to your registry, e.g. `docker push myregistry.com/myapp`.

Consult Docker's [getting started](https://docs.docker.com/go/get-started-sharing/)
docs for more detail on building and pushing.

---

📝 **Note**: This configuration supports both local development and containerized deployment with automatic environment detection.

### References
* [Docker's .NET guide](https://docs.docker.com/language/dotnet/)
* The [dotnet-docker](https://github.com/dotnet/dotnet-docker/tree/main/samples)
  repository has many relevant samples and docs.