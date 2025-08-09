"use client"

import React, { useState, useEffect } from 'react'
import { Loader2, Download, AlertCircle, FileText, Settings } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Checkbox } from '@/components/ui/checkbox'
import { generatePdfFromHtml, getPdfUrl, buildCompleteLetterHtml, fetchClientTemplateData } from '@/services/letterService'

interface PdfPreviewProps {
  templateName?: string
  clientId?: number
  className?: string
  // Function to get the current letter body HTML from TemplatePreview
  getCurrentLetterHtml?: () => string
 html?: string | null; // Optional prop to pass letter body
}

export function PdfPreview({ templateName, clientId, className = "", getCurrentLetterHtml, html }: PdfPreviewProps) {
  const [isGenerating, setIsGenerating] = useState(false)
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [pdfUrl, setPdfUrl] = useState<string | null>(null)
  const [pdfFileName, setPdfFileName] = useState<string | null>(null)
  const [autoDownload, setAutoDownload] = useState(true)
  const [downloadNotification, setDownloadNotification] = useState<string | null>(null)

  const generatePdf = async () => {
    if (!templateName || !clientId) {
      setError('Template name and client ID are required')
      return
    }

    setIsGenerating(true)
    setError(null)

    try {
      // Get the current letter HTML from TemplatePreview
      const letterBodyHtml = getCurrentLetterHtml ? getCurrentLetterHtml() : ''
      
      if (!letterBodyHtml) {
        setError('No letter content available. Please ensure the template is loaded.')
        return
      }

      // Fetch client data to build complete letter
      const clientData = await fetchClientTemplateData(clientId, templateName)
      
      // Build complete letter HTML with header, body, and signature
      const completeHtml = buildCompleteLetterHtml(letterBodyHtml, {
        id: clientId,
        name: clientData.name || clientData.contact_name || 'Unknown Client',
        address: clientData.contact_address_inline || clientData.address || 'No address provided',
        taxId: clientData.tax_id || clientData.taxId || 'N/A',
        email: clientData.email || ''
      })

      // Generate PDF from complete HTML
      const result = await generatePdfFromHtml(templateName, clientId, completeHtml)
      
      // Extract filename from the path
      const fileName = result.pdfPath.split(/[/\\]/).pop() ?? 'document.pdf'
      setPdfFileName(fileName)
      
      // Create URL for PDF preview
      const pdfPreviewUrl = getPdfUrl(fileName)
      setPdfUrl(pdfPreviewUrl)

      // Automatically download the PDF if enabled
      if (autoDownload) {
        setTimeout(() => {
          const link = document.createElement('a')
          link.href = pdfPreviewUrl
          link.download = fileName
          link.target = '_blank' // Open in new tab as fallback
          document.body.appendChild(link)
          link.click()
          document.body.removeChild(link)
          
          // Show download notification
          setDownloadNotification(`PDF "${fileName}" downloaded automatically`)
          setTimeout(() => setDownloadNotification(null), 3000)
        }, 500) // Small delay to ensure PDF is ready
      }

    } catch (err) {
      console.error('Error generating PDF:', err)
      setError(err instanceof Error ? err.message : 'Failed to generate PDF')
    } finally {
      setIsGenerating(false)
    }
  }

  const downloadPdf = () => {
    if (pdfUrl && pdfFileName) {
      const link = document.createElement('a')
      link.href = pdfUrl
      link.download = pdfFileName
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
    }
  }

  const handlePdfLoad = () => {
    setIsLoading(false)
  }

  const handlePdfLoadStart = () => {
    setIsLoading(true)
  }

  return (
    <div className={`space-y-4 ${className}`}>
      {/* Controls */}
      <div className="flex items-center gap-2 justify-between">
        <div className="flex items-center gap-2">
          <Button 
            onClick={generatePdf} 
            disabled={isGenerating || !templateName || !clientId}
            className="flex items-center gap-2"
          >
            {isGenerating ? (
              <>
                <Loader2 className="h-4 w-4 animate-spin" />
                Generating...
              </>
            ) : (
              <>
                <FileText className="h-4 w-4" />
                Generate PDF Preview
              </>
            )}
          </Button>
          
          {pdfUrl && (
            <Button 
              variant="outline" 
              onClick={downloadPdf}
              className="flex items-center gap-2"
            >
              <Download className="h-4 w-4" />
              Download
            </Button>
          )}

          {/* Auto-download option */}
          <div className="flex items-center gap-2 ml-4">
            <Checkbox
              id="auto-download"
              checked={autoDownload}
              onCheckedChange={(checked) => setAutoDownload(checked === true)}
            />
            <label 
              htmlFor="auto-download" 
              className="text-sm text-gray-600 cursor-pointer"
            >
              Auto-download
            </label>
          </div>
        </div>

        {templateName && clientId && (
          <div className="text-sm text-gray-600">
            Template: {templateName.split(/[/\\]/).pop()} | Client ID: {clientId}
          </div>
        )}
      </div>

      {/* Error Display */}
      {error && (
        <Alert variant="destructive">
          <AlertCircle className="h-4 w-4" />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
      )}

      {/* Download Notification */}
      {downloadNotification && (
        <Alert className="border-green-200 bg-green-50">
          <Download className="h-4 w-4" />
          <AlertDescription className="text-green-800">{downloadNotification}</AlertDescription>
        </Alert>
      )}

      {/* PDF Preview */}
      {pdfUrl && (
        <div className="border border-gray-200 rounded-lg overflow-hidden bg-white">
          <div className="bg-gray-50 px-4 py-2 border-b flex items-center justify-between">
            <span className="text-sm font-medium">PDF Preview</span>
            {isLoading && (
              <div className="flex items-center gap-2 text-sm text-gray-600">
                <Loader2 className="h-4 w-4 animate-spin" />
                Loading...
              </div>
            )}
          </div>
          
          <div className="relative" style={{ height: '600px' }}>
            <iframe
              src={pdfUrl}
              className="w-full h-full"
              title="PDF Preview"
              onLoad={handlePdfLoad}
              onLoadStart={handlePdfLoadStart}
              style={{ border: 'none' }}
            />
            
            {isLoading && (
              <div className="absolute inset-0 bg-gray-50 flex items-center justify-center">
                <div className="flex flex-col items-center gap-2">
                  <Loader2 className="h-8 w-8 animate-spin text-blue-600" />
                  <span className="text-sm text-gray-600">Loading PDF...</span>
                </div>
              </div>
            )}
          </div>
        </div>
      )}

      {/* Instructions
      {!pdfUrl && !isGenerating && !error && (
        <div className="text-center py-8 text-gray-500">
          <FileText className="h-12 w-12 mx-auto mb-4 text-gray-300" />
          <p className="text-sm">Click "Generate PDF Preview" to create and preview the document</p>
          <p className="text-xs mt-1">Select a template and client first</p>
        </div>
      )} */}

      <div className="p-12 border max-w-xl min-h-[800px] mx-auto bg-white rounded-lg text-sm leading-7">
       {/* template */}
      </div>
    </div>
  )
}
