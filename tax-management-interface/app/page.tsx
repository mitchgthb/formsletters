"use client"

import React, { useState, useEffect } from "react"
import { Search, FileText, Download, Mail, Eye, Upload, ExternalLink } from "lucide-react"
import { Button } from "@/components/ui/button"
import { Input } from "@/components/ui/input"

import { Card, CardContent, CardHeader, CardTitle } from "@/components/ui/card"
import { Badge } from "@/components/ui/badge"
import { Checkbox } from "@/components/ui/checkbox"
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select"
import { Dialog, DialogContent, DialogHeader, DialogTitle, DialogTrigger } from "@/components/ui/dialog"
import { Alert, AlertDescription } from "@/components/ui/alert"
import { Progress } from "@/components/ui/progress"
import {
  Sidebar,
  SidebarContent,
  SidebarHeader,
  SidebarProvider,
  SidebarTrigger,
  SidebarInset,
  SidebarGroup,
  SidebarGroupContent,
  SidebarGroupLabel,
} from "@/components/ui/sidebar"
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs"
import { TemplateSelector } from "@/components/TemplateSelector"
import { FormSelector } from "@/components/FormSelector"
import { EnhancedTemplatePreview } from "@/components/EnhancedTemplatePreview"
import { PdfPreview } from "@/components/PdfPreview"
import { buildPreviewHtml } from "@/services/letterService"
import { get } from "http"


// API data fetching and state

export default function TaxManagementInterface() { 
  // Upload Template Dialog as a function
  // State for upload dialog
  const [uploading, setUploading] = useState(false);
  const [uploadError, setUploadError] = useState<string | null>(null);
  const [uploadSuccess, setUploadSuccess] = useState<string | null>(null);
  const [selectedFile, setSelectedFile] = useState<File | null>(null);
  const fileInputRef = React.useRef<HTMLInputElement>(null);
  // Ensure no trailing slash so we don't end up with double slashes when concatenating paths
  const apiBase = (process.env.NEXT_PUBLIC_API_BASE_URL ?? '').replace(/\/+$/, '');
  console.log("API Base URL:", apiBase);
  // Global loading and error state for API fetch calls
  const [loading, setLoading] = useState<{ clients: boolean; templates: boolean; forms: boolean }>({
    clients: false,
    templates: false,
    forms: false,
  });
  const [error, setError] = useState<{ clients?: string; templates?: string; forms?: string }>({});

  // Handle file selection
  function handleFileChange(e: React.ChangeEvent<HTMLInputElement>) {
    if (e.target.files && e.target.files.length > 0) {
      setSelectedFile(e.target.files[0]);
      setUploadError(null);
      setUploadSuccess(null);
    }
  }

  const fetchTemplates = async () => {
    try {
      const res = await fetch(`${apiBase}/templates`)
      if (!res.ok) throw new Error(`Error ${res.status}: ${res.statusText}`)
      const data = await res.json()
      setTemplates(data)
    } catch (err) {
      console.error('Error fetching templates:', err)
      setError(prev => ({ ...prev, templates: err instanceof Error ? err.message : 'Failed to fetch templates' }))
    } finally {
      setLoading(prev => ({ ...prev, templates: false }))
    }
  }

  // Handle upload
  async function handleUpload() {
    if (!selectedFile) return;
    setUploading(true);
    setUploadError(null);
    setUploadSuccess(null);
    try {
      const formData = new FormData();
      formData.append('file', selectedFile);
      const res = await fetch(`${apiBase}/templates/upload`, {
        method: 'POST',
        body: formData,
      });
      if (!res.ok) throw new Error(`Upload failed (${res.status})`);
      setUploadSuccess('Template uploaded successfully!');
      setSelectedFile(null);
      if (fileInputRef.current) fileInputRef.current.value = '';
      await fetchTemplates(); // Refresh templates after upload
    } catch (err: any) {
      setUploadError(err.message || 'Upload failed');
    } finally {
      setUploading(false);
    }
  }

  function renderUploadTemplateDialog() {
    return (
      <Dialog>
        <DialogTrigger asChild>
          <Button variant="outline" size="sm" className="w-full sm:w-auto">
            <Upload className="h-4 w-4 mr-1 sm:mr-2" />
            <span className="text-sm">Upload Template</span>
          </Button>
        </DialogTrigger>
        <DialogContent className="w-full mx-4">
          <DialogHeader>
            <DialogTitle className="text-lg sm:text-xl">Upload Word Template</DialogTitle>
          </DialogHeader>
          <div className="space-y-4">
            <div className="border-2 border-dashed border-gray-300 rounded-lg p-4 sm:p-6 text-center">
              <Upload className="h-6 w-6 sm:h-8 sm:w-8 mx-auto text-gray-400 mb-2" />
              <p className="text-xs sm:text-sm text-gray-600">Drop your Word template here or click to browse</p>
              <input
                type="file"
                accept=".docx,application/vnd.openxmlformats-officedocument.wordprocessingml.document"
                ref={fileInputRef}
                style={{ display: 'none' }}
                onChange={handleFileChange}
              />
              <Button
                variant="outline"
                size="sm"
                className="mt-2 bg-transparent w-full sm:w-auto"
                onClick={() => fileInputRef.current?.click()}
                disabled={uploading}
              >
                <span className="text-sm">Browse Files</span>
              </Button>
              {selectedFile && (
                <div className="mt-2 text-xs sm:text-sm text-gray-700 break-all">Selected: {selectedFile.name}</div>
              )}
              <Button
                variant="default"
                size="sm"
                className="mt-4 w-full sm:w-auto"
                onClick={handleUpload}
                disabled={!selectedFile || uploading}
              >
                <span className="text-sm">{uploading ? 'Uploading...' : 'Upload'}</span>
              </Button>
              {uploadSuccess && <div className="mt-2 text-xs sm:text-sm text-green-600">{uploadSuccess}</div>}
              {uploadError && <div className="mt-2 text-xs sm:text-sm text-red-600">{uploadError}</div>}
            </div>
          </div>
        </DialogContent>
      </Dialog>
    );
  }

    
  const [clients, setClients] = useState<any[]>([])
  const [templates, setTemplates] = useState<any[]>([])
  const [templateHtml, setTemplateHtml] = useState<string | null>(null)
  const [templatePlaceholders, setTemplatePlaceholders] = useState<string[]>([])

  const [loadingTemplateHtml, setLoadingTemplateHtml] = useState(false)
  const [errorTemplateHtml, setErrorTemplateHtml] = useState<string | null>(null)
  const [forms, setForms] = useState<any[]>([])
  // Map clientId -> populated preview HTML
  const [letterPreviews, setLetterPreviews] = useState<Record<number, string>>({})
  // Template and form selection state
  const [selectedTemplate, setSelectedTemplate] = useState<string | null>(null)
  const [selectedForm, setSelectedForm] = useState<number | null>(null)
  // Load populated previews when template or clients change
  useEffect(() => {
    if (!selectedTemplate || clients.length === 0) return;

    const load = async () => {
      const map: Record<number, string> = {};
      await Promise.all(clients.map(async (c: any) => {
        try {
          const html = await buildPreviewHtml(selectedTemplate.split(/[/\\]/).pop() ?? '', c.id);
          map[c.id] = html;
        } catch (err) {
          console.error('Preview build failed', err);
        }
      }));
      setLetterPreviews(map);
    };
    load();
  }, [selectedTemplate, clients]);
  // (Removed duplicate loading and error state declarations)

  // Fetch data from backend
  // Fetch clients, templates, forms on mount
  React.useEffect(() => {
    const fetchClients = async () => {
      try {
        const res = await fetch(`${apiBase}/client-info`)
        if (!res.ok) throw new Error(`Error ${res.status}: ${res.statusText}`)
        const data = await res.json()
        // alias taxNumber to taxId for existing UI code
        const mapped = data.map((c: any) => ({ ...c, taxId: c.taxNumber }))
        setClients(mapped)
      } catch (err) {
        console.error('Error fetching clients:', err)
        setError(prev => ({ ...prev, clients: err instanceof Error ? err.message : 'Failed to fetch clients' }))
      } finally {
        setLoading(prev => ({ ...prev, clients: false }))
      }
    }

    const fetchTemplates = async () => {
      try {
        const res = await fetch(`${apiBase}/templates`)
        if (!res.ok) throw new Error(`Error ${res.status}: ${res.statusText}`)
        const data = await res.json()
        setTemplates(data)
      } catch (err) {
        console.error('Error fetching templates:', err)
        setError(prev => ({ ...prev, templates: err instanceof Error ? err.message : 'Failed to fetch templates' }))
      } finally {
        setLoading(prev => ({ ...prev, templates: false }))
      }
    }

    // Fetch forms from the backend API
    const fetchForms = async () => {
      try {
        const res = await fetch(`${apiBase}/forms`)
        if (!res.ok) throw new Error(`Error ${res.status}: ${res.statusText}`)
        const data = await res.json()
        setForms(data)
        setLoading(prev => ({ ...prev, forms: false }))
      } catch (err) {
        console.error('Error fetching forms:', err)
        setError(prev => ({ ...prev, forms: err instanceof Error ? err.message : 'Failed to fetch forms' }))
        setLoading(prev => ({ ...prev, forms: false }))
      }
    }

    fetchClients()
    fetchTemplates()
    fetchForms()
  }, [])

  // Fetch template HTML whenever a new template path is selected
  React.useEffect(() => {
    if (!selectedTemplate) return;
    const fetchTemplateHtml = async () => {
      setLoadingTemplateHtml(true);
      setErrorTemplateHtml(null);
      try {
        const templatePath = selectedTemplate ? selectedTemplate.split(/[/\\]/).pop() : '';
        const res = await fetch(`${apiBase}/templates/parse-template`, {
          method: 'POST',
          headers: { 'Content-Type': 'application/json' },
          body: JSON.stringify({ templatePath })
        });
        if (!res.ok) throw new Error(`Error ${res.status}: ${res.statusText}`);
        const data = await res.json();
        setTemplateHtml(data.editableHtml ?? null);
        setLetterBody(data.editableHtml ?? '');
        setTemplatePlaceholders(data.placeholders ?? []);
      } catch (err) {
        console.error('Error parsing template:', err);
        setErrorTemplateHtml(err instanceof Error ? err.message : 'Failed to parse template');
      } finally {
        setLoadingTemplateHtml(false);
      }
    };
    fetchTemplateHtml();
  }, [selectedTemplate])





  const [selectedClients, setSelectedClients] = useState<number[]>([])
  const [searchTerm, setSearchTerm] = useState("")
  const [letterBody, setLetterBody] = useState<string>("")
  const [getCurrentLetterHtml, setGetCurrentLetterHtml] = useState<(() => string) | null>(null)

  // Populate letter body with fully merged preview when template and a single client are selected
  React.useEffect(() => {
    if (selectedTemplate && selectedClients.length === 1) {
      const clientId = selectedClients[0];
      const templateName = selectedTemplate.split(/[/\\]/).pop() ?? '';
      buildPreviewHtml(templateName, clientId)
        .then((html) => {
          setLetterBody(html || "");
        })
        .catch((err) => console.error("Error building preview html", err));
    }
  }, [selectedTemplate, selectedClients])

  // Fallback: if templateHtml loaded and letterBody is empty or multi-client selection, use templateHtml
  React.useEffect(() => {
    if (templateHtml && (letterBody === "" || selectedClients.length !== 1)) {
      setLetterBody(templateHtml);
    }
  }, [templateHtml, selectedClients])

  const [status, setStatus] = useState<{ type: "idle" | "loading" | "success" | "error"; message: string }>({
    type: "idle",
    message: "",
  })
  const [progress, setProgress] = useState(0)
  const [activeTab, setActiveTab] = useState<"letters" | "forms">("letters")
  
  const [formSubmissionStatus, setFormSubmissionStatus] = useState<{
    [key: string]: { submitted: boolean; date?: string }
  }>({
    "1_1": { submitted: false },
    "1_2": { submitted: true, date: "29/07/2025" },
    "2_1": { submitted: false },
  })

  const filteredClients = clients.filter(
    (client) =>
      (client.name || client.Name || '').toLowerCase().includes(searchTerm.toLowerCase()) ||
      (client.email || client.Email || '').toLowerCase().includes(searchTerm.toLowerCase()),
  )

  const selectedTemplate_obj = selectedTemplate !== null ? templates.find((t) => (t.Name ?? t.name) === selectedTemplate) : null
  const selectedClientsData = clients.filter((c) => selectedClients.includes(c.id || c.Id))

  const handleClientToggle = (clientId: number) => {
    setSelectedClients((prev) => (prev.includes(clientId) ? prev.filter((id) => id !== clientId) : [...prev, clientId]))
  }

  const handleSelectAll = () => {
    if (selectedClients.length === filteredClients.length) {
      setSelectedClients([])
    } else {
      setSelectedClients(clients.map((c) => c.id || c.Id))
    }
  }

  const simulateProgress = (callback: () => void) => {
    setProgress(0)
    const interval = setInterval(() => {
      setProgress((prev) => {
        if (prev >= 100) {
          clearInterval(interval)
          callback()
          return 100
        }
        return prev + 10
      })
    }, 200)
  }

  const handleGeneratePDF = async () => {
    if (selectedClients.length === 0) {
      setStatus({ type: "error", message: "Please select at least one client" })
      return
    }

    if (!selectedTemplate) {
      setStatus({ type: "error", message: "Please select a template" })
      return
    }

    setStatus({ type: "loading", message: "Generating PDF letters..." })
    setProgress(0)

    try {
      const templateFileName = selectedTemplate.split(/[/\\]/).pop() ?? selectedTemplate
      const results = []

      for (let i = 0; i < selectedClients.length; i++) {
        const clientId = selectedClients[i]
        const client = selectedClientsData.find(c => (c.id || c.Id) === clientId)
        
        if (!client) continue

        // For multiple clients, use the base template HTML for each client
        // For single client, try to get the edited content first, then fall back to letterBody
        let letterBodyHtml = ''
        
        if (selectedClients.length === 1 && getCurrentLetterHtml && typeof getCurrentLetterHtml === 'function') {
          try {
            letterBodyHtml = getCurrentLetterHtml()
          } catch (error) {
            console.warn('Error getting current letter HTML:', error)
            letterBodyHtml = letterBody || templateHtml || ''
          }
        } else {
          // For multiple clients, use the populated preview for each client or fall back to template
          letterBodyHtml = letterPreviews[clientId] || letterBody || templateHtml || ''
        }
        
        if (!letterBodyHtml) {
          console.warn(`No letter content available for client ${clientId}`)
          continue
        }

        // Build complete letter HTML with header, body, and signature
        const { generatePdfFromHtml, buildCompleteLetterHtml, fetchClientTemplateData } = await import('@/services/letterService')
        
        // Fetch client data for building complete letter
        const clientData = await fetchClientTemplateData(clientId, templateFileName)
        
        // Build complete letter HTML
        const completeHtml = buildCompleteLetterHtml(letterBodyHtml, {
          id: clientId,
          name: client.name || client.Name || 'Unknown Client',
          address: client.address || client.Address || 'No address provided',
          taxId: client.taxId || client.TaxId || 'N/A',
          email: client.email || client.Email || ''
        })

        // Generate PDF from complete HTML
        const result = await generatePdfFromHtml(templateFileName, clientId, completeHtml)
        results.push(result)

        // Update progress
        setProgress(((i + 1) / selectedClients.length) * 100)
      }

      setStatus({ 
        type: "success", 
        message: `Successfully generated ${results.length} PDF letter(s)` 
      })
      setTimeout(() => setStatus({ type: "idle", message: "" }), 3000)

    } catch (error) {
      console.error('Error generating PDFs:', error)
      setStatus({ 
        type: "error", 
        message: error instanceof Error ? error.message : 'Failed to generate PDFs' 
      })
      setTimeout(() => setStatus({ type: "idle", message: "" }), 5000)
    }
  }

  const handleSendEmail = async () => {
    if (selectedClients.length === 0) {
      setStatus({ type: "error", message: "Please select at least one client" })
      return
    }

    setStatus({ type: "loading", message: "Sending emails..." })

    simulateProgress(() => {
      setStatus({ type: "success", message: `Successfully sent emails to ${selectedClients.length} client(s)` })
      setTimeout(() => setStatus({ type: "idle", message: "" }), 3000)
    })
  }

  const handleImportToOdoo = async (clientId: number) => {
    setStatus({ type: "loading", message: "Importing form data to Odoo..." })

    simulateProgress(() => {
      setStatus({ type: "success", message: "Successfully imported form data to Odoo" })
      setTimeout(() => setStatus({ type: "idle", message: "" }), 3000)
    })
  }
  return (
    <SidebarProvider defaultOpen={true}>
      <div className="flex h-screen w-screen bg-gray-50 overflow-hidden">
        <Sidebar className="border-r border-gray-200 flex-shrink-0">
          <SidebarHeader className="p-3 sm:p-4 border-b border-gray-200">
            <div className="flex items-center gap-2">
              <FileText className="h-5 w-5 sm:h-6 sm:w-6 text-blue-600" />
              <h1 className="font-semibold text-base sm:text-lg">Tax Manager</h1>
            </div>
          </SidebarHeader>

          <SidebarContent className="p-4 overflow-hidden">
            <SidebarGroup className="h-full flex flex-col">
              <SidebarGroupLabel className="flex items-center justify-between flex-shrink-0">
                <span>Clients ({filteredClients.length})</span>
                <Button variant="ghost" size="sm" onClick={handleSelectAll} className="h-6 px-2 text-xs">
                  {selectedClients.length === filteredClients.length ? "Deselect All" : "Select All"}
                </Button>
              </SidebarGroupLabel>

              <div className="mb-3 flex-shrink-0">
                <div className="relative">
                  <Search className="absolute left-3 top-1/2 transform -translate-y-1/2 h-4 w-4 text-gray-400" />
                  <Input
                    placeholder="Search clients..."
                    value={searchTerm}
                    onChange={(e) => setSearchTerm(e.target.value)}
                    className="pl-10"
                  />
                </div>
              </div>

              <SidebarGroupContent className="flex-1 overflow-hidden">
                <div className="space-y-2 h-full overflow-y-auto">
                  {loading.clients ? (
              <div className="p-4 text-center">
                <Progress value={30} className="w-[60%] mx-auto" />
                <p className="text-sm text-muted-foreground mt-2">Loading clients...</p>
              </div>
            ) : error.clients ? (
              <Alert variant="destructive" className="my-4">
                <AlertDescription>{error.clients}</AlertDescription>
              </Alert>
            ) : filteredClients.length === 0 ? (
              <div className="p-4 text-center">
                <p className="text-muted-foreground">No clients found</p>
              </div>
            ) : filteredClients.map((client) => (
                    <div
                      key={client.id || client.Id}
                      className={`p-3 rounded-lg border cursor-pointer transition-colors ${
                        selectedClients.includes(client.id || client.Id)
                          ? "bg-blue-50 border-blue-200"
                          : "bg-white border-gray-200 hover:bg-gray-50"
                      }`}
                      onClick={() => handleClientToggle(client.id || client.Id)}
                    >
                      <div className="flex items-start gap-3">
                        <Checkbox
                          id={`client-${client.id || client.Id}`}
                          checked={selectedClients.includes(client.id || client.Id)}
                          onChange={() => handleClientToggle(client.id || client.Id)}
                          className="mt-1 flex-shrink-0"
                        />
                        <div className="flex-1 min-w-0">
                          <p className="font-medium text-sm truncate">{client.name}</p>
                          <p className="text-xs text-gray-500 truncate">{client.email || client.Email}</p>
                          <p className="text-xs text-gray-500 truncate">Tax ID: {client.taxId || client.TaxId}</p>
                        </div>
                      </div>
                    </div>
                  ))}
                </div>
              </SidebarGroupContent>
            </SidebarGroup>

            {selectedClients.length > 0 && (
              <div className="mt-4 p-3 bg-blue-50 rounded-lg flex-shrink-0">
                <p className="text-sm font-medium text-blue-900">{selectedClients.length} client(s) selected</p>
                <div className="flex flex-wrap gap-1 mt-2">
                  {selectedClientsData.slice(0, 3).map((client) => (
                    <Badge key={client.id || client.Id} variant="secondary" className="text-xs">
                      {client.name}
                    </Badge>
                  ))}
                  {selectedClients.length > 3 && (
                    <Badge variant="secondary" className="text-xs">
                      +{selectedClients.length - 3} more
                    </Badge>
                  )}
                </div>
              </div>
            )}
          </SidebarContent>
        </Sidebar>

        <SidebarInset className="flex-1 w-full overflow-hidden">
          <div className="flex flex-col h-full w-full">
            {/* Header */}
            <div className="flex flex-col sm:flex-row items-start sm:items-center justify-between p-4 border-b border-gray-200 bg-white flex-shrink-0 gap-4 w-full">
              <div className="flex items-center gap-4">
                <SidebarTrigger />
                <div>
                  <h2 className="text-xl font-semibold">Letter Generator</h2>
                  <p className="text-sm text-gray-500">Create and send tax letters to clients</p>
                </div>
              </div>

              <div className="flex flex-col sm:flex-row items-stretch sm:items-center gap-2 w-full sm:w-auto">
                {renderUploadTemplateDialog()}
                <Button variant="outline" size="sm" className="w-full sm:w-auto">
                  <ExternalLink className="h-4 w-4 mr-2" />
                  Open in Word
                </Button>
              </div>
            </div>

            {/* Status Alert */}
            {status.type !== "idle" && (
              <div className="p-4 border-b border-gray-200">
                <Alert
                  className={
                    status.type === "error"
                      ? "border-red-200 bg-red-50"
                      : status.type === "success"
                        ? "border-green-200 bg-green-50"
                        : "border-blue-200 bg-blue-50"
                  }
                >
                  <AlertDescription className="flex items-center gap-2">
                    {status.type === "loading" && (
                      <div className="animate-spin h-4 w-4 border-2 border-blue-600 border-t-transparent rounded-full" />
                    )}
                    {status.message}
                  </AlertDescription>
                </Alert>
                {status.type === "loading" && <Progress value={progress} className="mt-2" />}
              </div>
            )}

            {/* Main Content */}
            <div className="flex-1 p-4 lg:p-6 overflow-auto w-full">
              <div className="w-full space-y-4 lg:space-y-6">
                {/* Template/Form Selection */}
                <Card>
                  <CardHeader>
                    <CardTitle className="flex flex-col sm:flex-row items-start sm:items-center gap-2">
                      <FileText className="h-5 w-5 flex-shrink-0" />
                      <span>Template Selection</span>
                    </CardTitle>
                  </CardHeader>
                  <CardContent className="space-y-4">
                    <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as "letters" | "forms")}>
                      <TabsList className="grid w-full grid-cols-2">
                        <TabsTrigger value="letters">Letters</TabsTrigger>
                        <TabsTrigger value="forms">Forms</TabsTrigger>
                      </TabsList>

                      <TabsContent value="letters">
                        <TemplateSelector
                          templates={templates}
                          selectedTemplate={selectedTemplate}
                          loading={loading.templates}
                          onChange={(value) => {
                            setSelectedTemplate(value);
                            setTemplateHtml(null);
                          }}
                        />
                      </TabsContent>

                      <TabsContent value="forms">
                        <FormSelector
                          forms={forms}
                          selectedForm={selectedForm}
                          loading={loading.forms}
                          onChange={(val) => setSelectedForm(val)}
                        />
                      </TabsContent>
                    </Tabs>
                  </CardContent>
                </Card>

             
                {/* Letter/Form Preview & Editor */}
                {selectedClientsData.length > 0 && (
                  <div className="space-y-6">
                    {selectedClientsData.map((client) => (
                      <div key={client.id || client.Id} className="space-y-4">
                        <div className="flex items-center justify-between p-4 border-b border-gray-200">
                          <span className="flex items-center gap-2 text-lg font-semibold">
                            <Eye className="h-5 w-5" />
                            {activeTab === "letters" ? "Letter" : "Form"} Preview & Editor for {client.name}
                          </span>
                          <Badge variant="outline">
                            {activeTab === "letters"
                              ? (selectedTemplate !== null ? selectedTemplate_obj?.name : "No template")
                              : forms.find((f) => f.id === selectedForm || f.Id === selectedForm)?.name}
                          </Badge>
                        </div>
                        <EnhancedTemplatePreview
                          templates={templates}
                          selectedTemplate={selectedTemplate}
                          letterPreviews={letterPreviews}
                          templateHtml={templateHtml}
                          client={client}
                          loading={loadingTemplateHtml}
                          error={errorTemplateHtml}
                          onChange={setLetterBody}
                          onContentReady={setGetCurrentLetterHtml}
                          html={letterBody}
                        />
                      </div>
                    ))}
                  </div>
                )}

                {/* Action Buttons */}
                <div className="flex flex-col sm:flex-row gap-3 sm:gap-4 justify-center px-4 sm:px-0">
                  {activeTab === "letters" ? (
                    <>
                      <Button
                        onClick={handleGeneratePDF}
                        disabled={selectedClients.length === 0 || status.type === "loading"}
                        className="flex items-center justify-center gap-2 w-full sm:w-auto"
                        size="lg"
                      >
                        <Download className="h-4 w-4 sm:h-5 sm:w-5" />
                        <span className="text-sm sm:text-base">Generate PDF ({selectedClients.length})</span>
                      </Button>

                      <Button
                        onClick={handleSendEmail}
                        disabled={selectedClients.length === 0 || status.type === "loading"}
                        variant="outline"
                        className="flex items-center justify-center gap-2 bg-transparent w-full sm:w-auto"
                        size="lg"
                      >
                        <Mail className="h-4 w-4 sm:h-5 sm:w-5" />
                        <span className="text-sm sm:text-base">Send Email ({selectedClients.length})</span>
                      </Button>
                    </>
                  ) : (
                    <>
                      <Button
                        onClick={handleGeneratePDF}
                        disabled={selectedClients.length === 0 || status.type === "loading"}
                        className="flex items-center justify-center gap-2 w-full sm:w-auto"
                        size="lg"
                      >
                        <Download className="h-4 w-4 sm:h-5 sm:w-5" />
                        <span className="text-sm sm:text-base">Generate PDF ({selectedClients.length})</span>
                      </Button>

                      <Button
                        onClick={handleSendEmail}
                        disabled={selectedClients.length === 0 || status.type === "loading"}
                        variant="outline"
                        className="flex items-center justify-center gap-2 bg-transparent w-full sm:w-auto px-2 sm:px-4"
                        size="lg"
                      >
                        <Mail className="h-4 w-4 sm:h-5 sm:w-5" />
                        <span className="text-sm sm:text-base">Send for Signature ({selectedClients.length})</span>
                      </Button>

                      <Button
                        onClick={() => {
                          const submittedForms = selectedClientsData.filter((client) => {
                            const statusKey = `${selectedForm}_${client.id}`
                            return formSubmissionStatus[statusKey]?.submitted
                          })
                          if (submittedForms.length > 0) {
                            handleImportToOdoo(submittedForms[0].id)
                          } else {
                            setStatus({ type: "error", message: "No submitted forms to import" })
                          }
                        }}
                        disabled={selectedClients.length === 0 || status.type === "loading"}
                        variant="secondary"
                        className="flex items-center gap-2 w-full sm:w-auto px-2 sm:px-4"
                        size="lg"
                      >
                        <ExternalLink className="h-4 w-4 sm:h-5 sm:w-5" />
                        <span className="text-sm sm:text-base">Import to Odoo</span>
                      </Button>
                    </>
                  )}

                  <Dialog>
                    <DialogTrigger asChild>
                      <Button variant="outline" size="lg" className="w-full sm:w-auto px-2 sm:px-4">
                        <Eye className="h-4 w-4 sm:h-5 sm:w-5 mr-1 sm:mr-2" />
                        <span className="text-sm sm:text-base">Preview {activeTab === "letters" ? "PDF" : "Form"}</span>
                      </Button>
                    </DialogTrigger>
                    <DialogContent className="max-w-6xl max-h-[90vh] overflow-auto w-full mx-4">
                      <DialogHeader>
                        <DialogTitle className="text-lg sm:text-xl">{activeTab === "letters" ? "PDF" : "Form"} Preview</DialogTitle>
                      </DialogHeader>
                      {activeTab === "letters" ? (
                        <PdfPreview 
                          templateName={selectedTemplate || undefined}
                          clientId={selectedClients.length === 1 ? selectedClients[0] : undefined}
                          className="min-h-[600px]"
                          getCurrentLetterHtml={getCurrentLetterHtml || undefined}
                          html={letterBody}
                        />
                      ) : (
                        <div className="bg-gray-100 p-4 rounded">
                          <p className="text-center text-gray-600">
                            Form preview would appear here
                          </p>
                          <p className="text-center text-sm text-gray-500 mt-2">
                            This would show the form with pre-filled client data
                          </p>
                        </div>
                      )}
                    </DialogContent>
                  </Dialog>
                </div>

                {/* Merge Fields Reference - Only show for letters */}
                {activeTab === "letters" && (
                  <Card>
                    <CardHeader>
                      <CardTitle className="text-sm sm:text-base">Available Merge Fields</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <div className="grid grid-cols-1 sm:grid-cols-2 md:grid-cols-4 gap-2 text-xs sm:text-sm">
                        <Badge variant="outline" className="text-xs">{"{{ClientName}}"}</Badge>
                        <Badge variant="outline" className="text-xs">{"{{ClientAddress}}"}</Badge>
                        <Badge variant="outline" className="text-xs">{"{{ClientTaxId}}"}</Badge>
                        <Badge variant="outline" className="text-xs">{"{{ClientEmail}}"}</Badge>
                        <Badge variant="outline" className="text-xs">{"{{AssessmentDate}}"}</Badge>
                        <Badge variant="outline" className="text-xs">{"{{TotalTaxDue}}"}</Badge>
                        <Badge variant="outline" className="text-xs">{"{{PaymentDueDate}}"}</Badge>
                        <Badge variant="outline" className="text-xs">{"{{ClientId}}"}</Badge>
                      </div>
                    </CardContent>
                  </Card>
                )}

                {/* Form Integration Info - Only show for forms */}
                {activeTab === "forms" && (
                  <Card>
                    <CardHeader>
                      <CardTitle className="text-sm sm:text-base">Form Integration Features</CardTitle>
                    </CardHeader>
                    <CardContent>
                      <div className="grid grid-cols-1 lg:grid-cols-2 gap-4 text-xs sm:text-sm">
                        <div className="space-y-2">
                          <h4 className="font-medium text-sm sm:text-base">Automatic Data Pre-filling:</h4>
                          <ul className="text-gray-600 space-y-1">
                            <li>• Client name and contact information</li>
                            <li>• Tax ID and business details</li>
                            <li>• Previous year's tax data</li>
                          </ul>
                        </div>
                        <div className="space-y-2">
                          <h4 className="font-medium text-sm sm:text-base">Webhook Integration:</h4>
                          <ul className="text-gray-600 space-y-1">
                            <li>• Real-time submission notifications</li>
                            <li>• Automatic Odoo record updates</li>
                            <li>• Digital signature verification</li>
                          </ul>
                        </div>
                      </div>
                    </CardContent>
                  </Card>
                )}
              </div>
            </div>
          </div>
        </SidebarInset>
      </div>
    </SidebarProvider>
  );
}