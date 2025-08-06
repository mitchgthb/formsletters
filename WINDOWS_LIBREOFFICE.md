# 🖥️ Windows-Native LibreOffice Integration Guide

## ✅ Why This Solution Works Better for Windows

Since LibreOffice Online requires Linux containers (which you don't have configured), I've created a **Windows-native LibreOffice integration** that:

- ✅ **Uses your existing LibreOffice installation**
- ✅ **No Docker required**
- ✅ **Native Windows compatibility**
- ✅ **Full document editing capabilities**
- ✅ **PDF generation**
- ✅ **HTML conversion for web editing**

## 🎯 How It Works

### Architecture:
```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Frontend      │    │    Backend       │    │   LibreOffice   │
│   (Next.js)     │───▶│   (.NET Core)    │───▶│   Windows       │
│   Port: 3000    │    │   Port: 5120     │    │   Native        │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

### Workflow:
1. **Template Selection** → Client data binding
2. **Document Preparation** → Template populated with client data
3. **Native LibreOffice Editing** → Opens in full LibreOffice application
4. **Web-based Editing** → HTML conversion for browser editing
5. **PDF Generation** → Native LibreOffice PDF export

## 🚀 New Features Available

### WindowsLibreOfficeController Endpoints:

| Endpoint | Purpose | Method |
|----------|---------|--------|
| `/api/windowslibreoffice/prepare-document` | Prepare template with client data | POST |
| `/api/windowslibreoffice/edit/{fileId}` | Open in native LibreOffice | GET |
| `/api/windowslibreoffice/document/{fileId}` | Get HTML for web editing | GET |
| `/api/windowslibreoffice/save-document/{fileId}` | Save from web editor | POST |
| `/api/windowslibreoffice/generate-pdf/{fileId}` | Generate PDF | POST |
| `/api/windowslibreoffice/pdf/{fileName}` | Serve PDF files | GET |
| `/api/windowslibreoffice/status` | Check LibreOffice status | GET |

## 🔧 Configuration

### Current Setup:
```json
// appsettings.Development.json
{
  "LibreOffice": {
    "ExecutablePath": "C:\\Program Files\\LibreOffice\\program\\soffice.exe"
  }
}
```

### Frontend Integration:
```bash
# .env.local
NEXT_PUBLIC_API_BASE_URL=http://localhost:5120
NEXT_PUBLIC_LIBREOFFICE_URL=http://localhost:5120/mock-libreoffice
NEXT_PUBLIC_USE_MOCK_LIBREOFFICE=true
NEXT_PUBLIC_USE_WINDOWS_LIBREOFFICE=true
```

## 📋 Usage Examples

### 1. Test LibreOffice Status
```bash
curl http://localhost:5120/api/windowslibreoffice/status
```

### 2. Prepare Document for Editing
```bash
curl -X POST http://localhost:5120/api/windowslibreoffice/prepare-document \
  -H "Content-Type: application/json" \
  -d '{"templateName":"template.docx","clientId":1}'
```

### 3. Open in Native LibreOffice
```bash
curl http://localhost:5120/api/windowslibreoffice/edit/12345678-1234-5678-9abc-123456789abc
```

### 4. Generate PDF
```bash
curl -X POST http://localhost:5120/api/windowslibreoffice/generate-pdf/12345678-1234-5678-9abc-123456789abc
```

## 🎉 Benefits Over Docker LibreOffice Online

| Feature | Docker LibreOffice Online | Windows Native |
|---------|--------------------------|----------------|
| **Setup Complexity** | ❌ Requires Linux containers | ✅ Uses existing LibreOffice |
| **Performance** | ⚠️ Container overhead | ✅ Native performance |
| **File Access** | ⚠️ Volume mounting required | ✅ Direct file system access |
| **Integration** | ⚠️ WOPI protocol complexity | ✅ Simple API calls |
| **Debugging** | ❌ Container logs | ✅ Native Windows debugging |
| **Offline Capability** | ❌ Requires server | ✅ Works offline |
| **User Experience** | ⚠️ Web-based only | ✅ Native app + Web options |

## 🔄 Migration from Mock to Windows Native

To switch from the current mock system to Windows native:

### Step 1: Update Frontend Configuration
```typescript
// Update LibreOfficeEditor.tsx to use Windows endpoints
const prepareResponse = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/api/windowslibreoffice/prepare-document`, {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ templateName, clientId, clientData })
});
```

### Step 2: Update Environment Variables
```bash
# .env.local
NEXT_PUBLIC_API_BASE_URL=http://localhost:5120
NEXT_PUBLIC_USE_WINDOWS_LIBREOFFICE=true
NEXT_PUBLIC_USE_MOCK_LIBREOFFICE=false
```

### Step 3: Test Integration
1. **Check LibreOffice Status**: `curl http://localhost:5120/api/windowslibreoffice/status`
2. **Prepare a Document**: Test document preparation endpoint
3. **Open in LibreOffice**: Test native application launching
4. **Generate PDF**: Test PDF generation workflow

## 🛠️ Troubleshooting

### Common Issues:

#### LibreOffice Not Found
- **Check installation**: Verify LibreOffice is installed
- **Update path**: Configure correct path in appsettings.json
- **Test manually**: Try running `soffice.exe` from command line

#### File Permission Issues
- **Check write permissions**: Ensure DocumentsTest folders are writable
- **Antivirus interference**: Add exception for working directory
- **File locks**: Close any open LibreOffice instances

#### PDF Generation Fails
- **LibreOffice path**: Verify soffice.exe path is correct
- **Temp directory**: Check temp folder permissions
- **Process conflicts**: Ensure no LibreOffice processes are hung

## 📈 Next Steps

1. **Complete the frontend integration** with Windows LibreOffice endpoints
2. **Test the native LibreOffice opening** functionality
3. **Implement hybrid editing** (native + web-based)
4. **Add real-time PDF preview** updates
5. **Consider background document processing** for large files

---

🎊 **This Windows-native approach provides a much better experience than trying to force LibreOffice Online to work with Windows containers!**
