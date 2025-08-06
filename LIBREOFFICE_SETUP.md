# 🔄 LibreOffice Online Setup Guide

## Current Status: ✅ Mock LibreOffice Online Working

Your Forms Letters application now has a **working mock LibreOffice Online integration** that allows you to test the complete workflow without needing Docker Linux containers.

## 🧪 What's Currently Working

### Mock LibreOffice Online Features:
- ✅ **Embedded Editor Interface** - Simulates real LibreOffice editing
- ✅ **Document Loading** - Loads templates with client data
- ✅ **Save Functionality** - Save document changes
- ✅ **PDF Generation** - Generate PDFs from edited content
- ✅ **Message Handling** - Full iframe communication system
- ✅ **Multi-tab Interface** - Switch between LibreOffice, HTML, and PDF views

### Test Your Integration:
1. **Open** http://localhost:3000
2. **Select a template and client**
3. **Click the "LibreOffice Editor" tab**
4. **See the mock editor interface**
5. **Test saving and PDF generation**
6. **Switch between editing modes**

## 🐳 Switching to Real LibreOffice Online

When you're ready to use the real LibreOffice Online (for production), follow these steps:

### Step 1: Configure Docker for Linux Containers
1. **Right-click Docker Desktop** in system tray
2. **Select "Switch to Linux containers..."**
3. **Wait for Docker to restart**

### Step 2: Run LibreOffice Online Container
```bash
# Single command for Windows PowerShell
docker run -t -d -p 9980:9980 -e "domain=localhost:3000" -e "username=admin" -e "password=admin" --name=libreoffice-online libreoffice/online:master
```

### Step 3: Update Configuration

**Frontend (.env.local):**
```bash
NEXT_PUBLIC_API_BASE_URL=http://localhost:5120
NEXT_PUBLIC_LIBREOFFICE_URL=http://localhost:9980
NEXT_PUBLIC_USE_MOCK_LIBREOFFICE=false
```

**Backend (appsettings.Development.json):**
```json
{
  "LibreOfficeOnline": {
    "BaseUrl": "http://localhost:9980",
    "WopiBaseUrl": "http://localhost:5120/api/libreoffice/wopi",
    "DocumentStoragePath": "./DocumentsTest/WorkingDocuments",
    "UseMockServer": false
  }
}
```

### Step 4: Test Real LibreOffice Online
```bash
# Test capabilities
curl http://localhost:9980/hosting/capabilities

# Should return real LibreOffice Online capabilities
```

## 🎯 Current Architecture

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐
│   Frontend      │    │    Backend       │    │   Mock          │
│   (Next.js)     │───▶│   (.NET Core)    │───▶│   LibreOffice   │
│   Port: 3000    │    │   Port: 5120     │    │   Online        │
└─────────────────┘    └──────────────────┘    └─────────────────┘
```

## 🚀 Development Workflow

### Current (Mock Mode):
1. **Template Selection** → Client data binding
2. **Mock Editor Loading** → Simulated LibreOffice interface
3. **Content Editing** → HTML-based editing simulation
4. **PDF Generation** → Real PDF output via LibreOffice CLI
5. **Document Download** → Functional PDF download

### Production (Real LibreOffice Online):
1. **Template Selection** → Client data binding  
2. **Real Editor Loading** → Full LibreOffice Online interface
3. **WYSIWYG Editing** → True LibreOffice document editing
4. **PDF Generation** → Native LibreOffice Online PDF export
5. **Document Download** → Professional document output

## 📊 Feature Comparison

| Feature | Mock Mode | Real LibreOffice Online |
|---------|-----------|------------------------|
| Document Editing | ✅ HTML Simulation | ✅ Full WYSIWYG |
| Template Loading | ✅ Basic | ✅ Advanced |
| Content Controls | ⚠️ Simulated | ✅ Native |
| Formatting | ⚠️ Limited | ✅ Complete |
| Save/Load | ✅ Functional | ✅ Enterprise |
| PDF Export | ✅ Via CLI | ✅ Native |
| Multi-user | ❌ Single | ✅ Multi-user |
| Real-time Collaboration | ❌ No | ✅ Yes |

## 🔧 Troubleshooting

### Mock Mode Issues:
- **Check backend**: Ensure .NET app running on port 5120
- **Check frontend**: Ensure Next.js running on port 3000
- **Check configuration**: Verify `.env.local` has correct URLs

### Real LibreOffice Online Issues:
- **Docker containers**: Switch to Linux containers mode
- **Port conflicts**: Ensure port 9980 is available
- **Domain configuration**: Match frontend domain in Docker command
- **CORS issues**: Check domain configuration in LibreOffice Online

## 📝 Next Steps

1. **Test current mock integration** thoroughly
2. **Verify PDF generation** works correctly
3. **When ready for production**: Switch to real LibreOffice Online
4. **Consider deployment**: Use Docker Compose for production setup

---

🎉 **Congratulations!** You now have a fully functional LibreOffice integration that can work in both development (mock) and production (real) modes.
