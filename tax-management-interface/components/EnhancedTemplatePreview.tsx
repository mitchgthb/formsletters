"use client"

import React, { useState, useEffect } from 'react'
import { Tabs, TabsContent, TabsList, TabsTrigger } from '@/components/ui/tabs'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Button } from '@/components/ui/button'
import { FileText, Eye, Settings } from 'lucide-react'
import { LibreOfficeEditor } from './LibreOfficeEditor'
import { LibreOfficePdfViewer } from './LibreOfficePdfViewer'
import { TemplatePreview } from './TemplatePreview'

interface EnhancedTemplatePreviewProps {
  templates: any[]
  selectedTemplate: string | null
  letterPreviews: Record<number, string>
  templateHtml: string | null
  client: any
  loading: boolean
  error: string | null
  onChange?: (value: string) => void
  onContentReady?: (getHtml: () => string) => void
  html?: string
}

export function EnhancedTemplatePreview({
  templates,
  selectedTemplate,
  letterPreviews,
  templateHtml,
  client,
  loading,
  error,
  onChange,
  onContentReady,
  html
}: EnhancedTemplatePreviewProps) {
  const [activeMode, setActiveMode] = useState<'html' | 'libreoffice' | 'pdf'>('libreoffice')
  const [pdfUrl, setPdfUrl] = useState<string | null>(null)
  const [isGeneratingPdf, setIsGeneratingPdf] = useState(false)

  // Get template name for LibreOffice
  const templateName = selectedTemplate ? selectedTemplate.split(/[/\\]/).pop() : null

  // Handle document changes from LibreOffice editor
  const handleDocumentChange = (content: string) => {
    if (onChange) {
      onChange(content)
    }
  }

  // Handle save from LibreOffice editor
  const handleDocumentSave = async (documentData: any) => {
    console.log('Document saved:', documentData)
    // Optionally trigger PDF generation after save
    if (activeMode === 'libreoffice') {
      await generatePdf()
    }
  }

  // Generate PDF from current document state
  const generatePdf = async () => {
    if (!templateName || !client) return

    try {
      setIsGeneratingPdf(true)

      const response = await fetch(`${process.env.NEXT_PUBLIC_API_BASE_URL}/letters/generate-from-html`, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify({
          templateName,
          clientId: client.id || client.Id,
          // Use current HTML content or get from LibreOffice
          updatedHtml: html || templateHtml || ''
        })
      })

      if (!response.ok) {
        throw new Error('Failed to generate PDF')
      }

      const result = await response.json()
      const fileName = result.pdfPath.split(/[/\\]/).pop()
      const pdfPreviewUrl = `${process.env.NEXT_PUBLIC_API_BASE_URL}/letters/pdf/${encodeURIComponent(fileName)}`
      
      setPdfUrl(pdfPreviewUrl)

    } catch (err) {
      console.error('Error generating PDF:', err)
    } finally {
      setIsGeneratingPdf(false)
    }
  }

  // Auto-generate PDF when switching to PDF tab
  useEffect(() => {
    if (activeMode === 'pdf' && !pdfUrl && templateName && client) {
      generatePdf()
    }
  }, [activeMode, templateName, client])

  if (loading) {
    return (
      <Card>
        <CardContent className="p-6 text-center">
          <FileText className="h-8 w-8 mx-auto mb-2 opacity-50" />
          <p>Loading template...</p>
        </CardContent>
      </Card>
    )
  }

  if (error) {
    return (
      <Card>
        <CardContent className="p-6">
          <div className="text-center text-red-600">
            <p>Error loading template: {error}</p>
          </div>
        </CardContent>
      </Card>
    )
  }

  if (!selectedTemplate || !client) {
    return (
      <Card>
        <CardContent className="p-6 text-center text-gray-500">
          <FileText className="h-12 w-12 mx-auto mb-4 opacity-50" />
          <p>Select a template and client to begin editing</p>
        </CardContent>
      </Card>
    )
  }

  return (
    <div className="space-y-4">
      {/* Mode Selection */}
      <Card>
        <CardHeader>
          <CardTitle className="flex flex-col sm:flex-row items-start sm:items-center justify-between gap-4">
            <span className="text-base sm:text-lg">Document Editor & Viewer</span>
            <div className="flex items-center gap-2 w-full sm:w-auto">
              <Button
                onClick={generatePdf}
                disabled={isGeneratingPdf}
                variant="outline"
                size="sm"
                className="w-full sm:w-auto"
              >
                <Eye className="h-4 w-4 mr-2" />
                {isGeneratingPdf ? 'Generating...' : 'Generate PDF'}
              </Button>
            </div>
          </CardTitle>
        </CardHeader>
        <CardContent>
          <Tabs value={activeMode} onValueChange={(value) => setActiveMode(value as any)}>
            <TabsList className="grid w-full grid-cols-1 sm:grid-cols-3 gap-1">
              <TabsTrigger value="libreoffice" className="flex items-center gap-2 text-xs sm:text-sm">
                <FileText className="h-4 w-4" />
                <span className="hidden sm:inline">LibreOffice Editor</span>
                <span className="sm:hidden">Editor</span>
              </TabsTrigger>
              <TabsTrigger value="html" className="flex items-center gap-2 text-xs sm:text-sm">
                <Settings className="h-4 w-4" />
                <span className="hidden sm:inline">HTML Editor</span>
                <span className="sm:hidden">HTML</span>
              </TabsTrigger>
              <TabsTrigger value="pdf" className="flex items-center gap-2 text-xs sm:text-sm">
                <Eye className="h-4 w-4" />
                <span className="hidden sm:inline">PDF Preview</span>
                <span className="sm:hidden">PDF</span>
              </TabsTrigger>
            </TabsList>

            <TabsContent value="libreoffice" className="mt-4">
              <LibreOfficeEditor
                templateName={templateName!}
                clientId={client.id || client.Id}
                clientData={{
                  name: client.name || client.Name,
                  address: client.address || client.Address || 'No address provided',
                  taxId: client.taxId || client.TaxId,
                  email: client.email || client.Email
                }}
                onDocumentChange={handleDocumentChange}
                onSave={handleDocumentSave}
                className="w-full"
              />
            </TabsContent>

            <TabsContent value="html" className="mt-4">
              <TemplatePreview
                templates={templates}
                selectedTemplate={selectedTemplate}
                letterPreviews={letterPreviews}
                templateHtml={templateHtml}
                client={client}
                loading={loading}
                error={error}
                onChange={onChange}
                onContentReady={onContentReady}
                html={html}
              />
            </TabsContent>

            <TabsContent value="pdf" className="mt-4">
              <LibreOfficePdfViewer
                pdfUrl={pdfUrl || undefined}
                templateName={templateName || undefined}
                clientName={client.name || client.Name}
                className="w-full"
                autoLoad={true}
              />
            </TabsContent>
          </Tabs>
        </CardContent>
      </Card>
    </div>
  )
}
