# 📝 LibreOffice Online Integration Setup

This document explains how to set up the complete LibreOffice Online integration for WYSIWYG document editing and PDF generation.

## 🏗️ Architecture Overview

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│   Frontend      │    │    Backend       │    │ LibreOffice     │    │   LibreOffice   │
│   (Next.js)     │───▶│   (.NET Core)    │───▶│   Local         │    │   Online        │
│                 │    │                  │    │   (PDF Gen)     │    │   (Editor)      │
│   Components:   │    │   Controllers:   │    │                 │    │                 │
│   - Editor      │    │   - LibreOffice  │    │   soffice.exe   │    │   WOPI Server   │
│   - PDF Viewer  │    │   - Letters      │    │                 │    │   Port: 9980    │
│   - Enhanced    │    │   - Templates    │    │                 │    │                 │
│     Preview     │    │                  │    │                 │    │                 │
└─────────────────┘    └──────────────────┘    └─────────────────┘    └─────────────────┘
```

## 🚀 Quick Start

### Prerequisites
1. **LibreOffice** installed locally for PDF generation
2. **LibreOffice Online** for web-based editing
3. **Docker** (optional, for containerized setup)

### Installation Steps

#### 1. Install LibreOffice Online (Docker Method)
```bash
# Pull and run LibreOffice Online
docker run -t -d -p 9980:9980 -e "domain=localhost:3000" \
  -e "username=admin" -e "password=admin" \
  --name=libreoffice-online \
  libreoffice/online:master
```

#### 2. Configure Backend
Update `appsettings.Development.json`:
```json
{
  "LibreOfficeOnline": {
    "BaseUrl": "http://localhost:9980",
    "WopiBaseUrl": "https://localhost:65447/api/libreoffice/wopi",
    "DocumentStoragePath": "./DocumentsTest/WorkingDocuments"
  }
}
```

#### 3. Configure Frontend
Update `.env.local`:
```bash
NEXT_PUBLIC_API_BASE_URL=https://localhost:65447
NEXT_PUBLIC_LIBREOFFICE_URL=http://localhost:9980
```

## 📋 Component Features

### 1. LibreOfficeEditor Component
- **Full WYSIWYG Editing**: Real LibreOffice interface in browser
- **Template Pre-population**: Automatically fills client data
- **Real-time Sync**: Changes sync with backend
- **Save Integration**: Manual and auto-save capabilities

### 2. LibreOfficePdfViewer Component
- **Native PDF Viewing**: Using PDF.js integration
- **Zoom Controls**: 25% to 200% zoom levels
- **Page Navigation**: Multi-page document support
- **Rotation**: 90-degree rotation controls
- **Download**: Direct PDF download functionality

### 3. EnhancedTemplatePreview Component
- **Three Modes**: LibreOffice Editor, HTML Editor, PDF Preview
- **Seamless Switching**: Easy mode transitions
- **Auto PDF Generation**: Generates PDF when switching to preview
- **Client-Specific**: Per-client document instances

## 🔧 Configuration Options

### LibreOffice Online Settings
```json
{
  "LibreOfficeOnline": {
    "BaseUrl": "http://localhost:9980",           // LibreOffice Online URL
    "WopiBaseUrl": "https://localhost:65447/api/libreoffice/wopi",  // WOPI endpoint
    "DocumentStoragePath": "./WorkingDocuments",  // Document storage
    "MaxDocumentSizeMB": 50,                     // Max document size
    "SessionTimeoutMinutes": 30,                 // Session timeout
    "AllowedDomains": ["localhost:3000"]         // Allowed frontend domains
  }
}
```

### Frontend Environment Variables
```bash
# Required
NEXT_PUBLIC_API_BASE_URL=https://localhost:65447
NEXT_PUBLIC_LIBREOFFICE_URL=http://localhost:9980

# Optional
NEXT_PUBLIC_MAX_DOCUMENT_SIZE=52428800  # 50MB in bytes
NEXT_PUBLIC_EDITOR_TIMEOUT=1800000      # 30 minutes in milliseconds
```

## 🛠️ API Endpoints

### LibreOffice Integration
- `POST /api/libreoffice/prepare-document` - Prepare document for editing
- `POST /api/libreoffice/generate-pdf` - Generate PDF from editor
- `GET /api/libreoffice/wopi/files/{fileId}` - WOPI file info
- `GET /api/libreoffice/wopi/files/{fileId}/contents` - WOPI file contents
- `POST /api/libreoffice/wopi/files/{fileId}/contents` - WOPI file update

### Existing PDF Generation
- `POST /letters/generate-from-html` - Generate PDF from HTML
- `GET /letters/pdf/{fileName}` - Serve generated PDF

## 🔄 Workflow

### 1. Document Editing Flow
```
1. User selects template + client
2. EnhancedTemplatePreview loads
3. LibreOfficeEditor prepares document with client data
4. User edits in LibreOffice interface
5. Changes auto-save to backend
6. User can switch to PDF preview anytime
```

### 2. PDF Generation Flow
```
1. User clicks "Generate PDF" or switches to PDF tab
2. Current document state sent to backend
3. Backend uses LibreOffice CLI for PDF conversion
4. PDF URL returned to frontend
5. LibreOfficePdfViewer displays PDF
```

## 🐳 Docker Integration

### Complete Docker Setup
```yaml
version: '3.8'
services:
  libreoffice-online:
    image: libreoffice/online:master
    ports:
      - "9980:9980"
    environment:
      - domain=localhost:3000
      - username=admin
      - password=admin
    volumes:
      - ./documents:/opt/libreoffice/systemplate

  formsletters-backend:
    build: .
    ports:
      - "8080:8080"
    environment:
      - LibreOfficeOnline__BaseUrl=http://libreoffice-online:9980
    depends_on:
      - libreoffice-online
```

## 🧪 Testing

### 1. Test LibreOffice Online
```bash
# Check if LibreOffice Online is running
curl http://localhost:9980/hosting/capabilities

# Expected response: JSON with capabilities
```

### 2. Test WOPI Endpoints
```bash
# Test file info endpoint
curl https://localhost:65447/api/libreoffice/wopi/files/test123

# Test document preparation
curl -X POST https://localhost:65447/api/libreoffice/prepare-document \
  -H "Content-Type: application/json" \
  -d '{"templateName":"test.docx","clientId":1}'
```

### 3. Test Frontend Integration
1. Navigate to the application
2. Select a template and client
3. Switch to "LibreOffice Editor" tab
4. Verify editor loads with template content
5. Make edits and save
6. Switch to "PDF Preview" tab
7. Verify PDF generates and displays

## 🚨 Troubleshooting

### Common Issues

#### LibreOffice Online Not Loading
- **Check Docker container**: `docker ps | grep libreoffice`
- **Check domain configuration**: Ensure domain matches frontend URL
- **Check CORS settings**: LibreOffice Online must allow frontend domain

#### WOPI Errors
- **Verify WOPI URLs**: Check WopiBaseUrl configuration
- **Check file permissions**: Ensure document storage directory is writable
- **Validate JSON responses**: WOPI requires specific JSON format

#### PDF Generation Fails
- **Check LibreOffice CLI**: Ensure soffice.exe is accessible
- **Verify temp directories**: Check write permissions
- **Review logs**: Check backend logs for LibreOffice errors

### Debug Mode
Enable detailed logging in `appsettings.Development.json`:
```json
{
  "Logging": {
    "LogLevel": {
      "FormsLetters.Controllers.LibreOfficeController": "Debug",
      "FormsLetters.Services.DocumentGenerationService": "Debug"
    }
  }
}
```

## 📈 Performance Optimization

### 1. Document Caching
- Cache prepared documents for faster loading
- Implement document versioning
- Use Redis for session storage

### 2. PDF Generation
- Queue PDF generation for large documents
- Cache frequently requested PDFs
- Use background services for heavy operations

### 3. Frontend Optimization
- Lazy load LibreOffice components
- Implement component virtualization
- Cache PDF viewer state

## 🔒 Security Considerations

### 1. WOPI Security
- Implement proper authentication
- Validate file access permissions
- Use HTTPS for production

### 2. Document Storage
- Secure document storage location
- Implement access controls
- Regular cleanup of temporary files

### 3. Frontend Security
- Validate user inputs
- Sanitize document content
- Implement CSP headers

---

📝 **Note**: This integration provides a complete document editing and PDF generation workflow using LibreOffice's powerful editing capabilities combined with your existing template system.
