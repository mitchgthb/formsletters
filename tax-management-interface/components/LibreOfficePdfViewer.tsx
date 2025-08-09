"use client"

import React, { useState, useEffect, useRef } from 'react'
import { Loader2, Download, ZoomIn, ZoomOut, RotateCw, Eye, FileText } from 'lucide-react'
import { Button } from '@/components/ui/button'
import { Alert, AlertDescription } from '@/components/ui/alert'
import { Card, CardContent, CardHeader, CardTitle } from '@/components/ui/card'
import { Slider } from '@/components/ui/slider'

interface LibreOfficePdfViewerProps {
  pdfUrl?: string
  templateName?: string
  clientName?: string
  onDownload?: () => void
  className?: string
  autoLoad?: boolean
}

export function LibreOfficePdfViewer({ 
  pdfUrl, 
  templateName, 
  clientName,
  onDownload,
  className = "",
  autoLoad = true
}: LibreOfficePdfViewerProps) {
  const [isLoading, setIsLoading] = useState(false)
  const [error, setError] = useState<string | null>(null)
  const [zoom, setZoom] = useState([100])
  const [rotation, setRotation] = useState(0)
  const [currentPage, setCurrentPage] = useState(1)
  const [totalPages, setTotalPages] = useState(0)
  const iframeRef = useRef<HTMLIFrameElement>(null)

  // Load PDF in viewer
  const loadPdf = async (url: string) => {
    try {
      setIsLoading(true)
      setError(null)

      if (iframeRef.current) {
        // Use PDF.js viewer integrated with LibreOffice Online
        const viewerUrl = new URL(`${process.env.NEXT_PUBLIC_LIBREOFFICE_URL || 'http://localhost:9980'}/browser/dist/pdfjs/web/viewer.html`)
        viewerUrl.searchParams.set('file', encodeURIComponent(url))
        
        iframeRef.current.src = viewerUrl.toString()
      }

    } catch (err) {
      console.error('Error loading PDF:', err)
      setError(err instanceof Error ? err.message : 'Failed to load PDF')
    } finally {
      setIsLoading(false)
    }
  }

  // Handle zoom changes
  const handleZoomChange = (newZoom: number[]) => {
    setZoom(newZoom)
    if (iframeRef.current) {
      iframeRef.current.contentWindow?.postMessage({
        type: 'zoom',
        value: newZoom[0]
      }, '*')
    }
  }

  // Handle rotation
  const handleRotate = () => {
    const newRotation = (rotation + 90) % 360
    setRotation(newRotation)
    if (iframeRef.current) {
      iframeRef.current.contentWindow?.postMessage({
        type: 'rotate',
        value: newRotation
      }, '*')
    }
  }

  // Handle page navigation
  const goToPage = (page: number) => {
    if (page >= 1 && page <= totalPages) {
      setCurrentPage(page)
      if (iframeRef.current) {
        iframeRef.current.contentWindow?.postMessage({
          type: 'goToPage',
          value: page
        }, '*')
      }
    }
  }

  // Handle download
  const handleDownload = () => {
    if (pdfUrl) {
      const link = document.createElement('a')
      link.href = pdfUrl
      link.download = `${templateName || 'document'}_${clientName || 'client'}.pdf`
      link.target = '_blank'
      document.body.appendChild(link)
      link.click()
      document.body.removeChild(link)
      
      if (onDownload) {
        onDownload()
      }
    }
  }

  // Listen for messages from PDF viewer
  useEffect(() => {
    const handleMessage = (event: MessageEvent) => {
      if (event.data.type === 'pdfInfo') {
        setTotalPages(event.data.totalPages)
        setCurrentPage(event.data.currentPage)
      }
    }

    window.addEventListener('message', handleMessage)
    return () => window.removeEventListener('message', handleMessage)
  }, [])

  // Auto-load PDF when URL changes
  useEffect(() => {
    if (pdfUrl && autoLoad) {
      loadPdf(pdfUrl)
    }
  }, [pdfUrl, autoLoad])

  if (error) {
    return (
      <Card className={className}>
        <CardContent className="p-6">
          <Alert variant="destructive">
            <AlertDescription>
              <div className="space-y-2">
                <p>{error}</p>
                <Button onClick={() => pdfUrl && loadPdf(pdfUrl)} variant="outline" size="sm">
                  Retry
                </Button>
              </div>
            </AlertDescription>
          </Alert>
        </CardContent>
      </Card>
    )
  }

  if (!pdfUrl) {
    return (
      <Card className={className}>
        <CardContent className="p-6">
          <div className="text-center text-gray-500 py-12">
            <FileText className="h-12 w-12 mx-auto mb-4 opacity-50" />
            <p>No PDF to display</p>
            <p className="text-sm">Generate a PDF to view it here</p>
          </div>
        </CardContent>
      </Card>
    )
  }

  return (
    <Card className={className}>
      <CardHeader>
        <CardTitle className="flex items-center justify-between">
          <div className="flex items-center gap-2">
            <Eye className="h-5 w-5" />
            PDF Viewer - {clientName || 'Client'}
          </div>
          <div className="flex items-center gap-2">
            {/* Page Navigation */}
            {totalPages > 0 && (
              <div className="flex items-center gap-2 text-sm">
                <Button
                  onClick={() => goToPage(currentPage - 1)}
                  disabled={currentPage <= 1}
                  variant="outline"
                  size="sm"
                >
                  ←
                </Button>
                <span>
                  {currentPage} / {totalPages}
                </span>
                <Button
                  onClick={() => goToPage(currentPage + 1)}
                  disabled={currentPage >= totalPages}
                  variant="outline"
                  size="sm"
                >
                  →
                </Button>
              </div>
            )}
            
            {/* Zoom Controls */}
            <div className="flex items-center gap-2">
              <Button
                onClick={() => handleZoomChange([Math.max(25, zoom[0] - 25)])}
                variant="outline"
                size="sm"
              >
                <ZoomOut className="h-4 w-4" />
              </Button>
              <div className="w-20">
                <Slider
                  value={zoom}
                  onValueChange={handleZoomChange}
                  min={25}
                  max={200}
                  step={25}
                  className="w-full"
                />
              </div>
              <span className="text-sm w-12">{zoom[0]}%</span>
              <Button
                onClick={() => handleZoomChange([Math.min(200, zoom[0] + 25)])}
                variant="outline"
                size="sm"
              >
                <ZoomIn className="h-4 w-4" />
              </Button>
            </div>

            {/* Rotate */}
            <Button onClick={handleRotate} variant="outline" size="sm">
              <RotateCw className="h-4 w-4" />
            </Button>

            {/* Download */}
            <Button onClick={handleDownload} size="sm">
              <Download className="h-4 w-4 mr-2" />
              Download
            </Button>
          </div>
        </CardTitle>
      </CardHeader>
      <CardContent className="p-0">
        <div className="relative" style={{ height: '700px' }}>
          {isLoading && (
            <div className="absolute inset-0 bg-white bg-opacity-90 flex items-center justify-center z-10">
              <div className="flex items-center gap-2">
                <Loader2 className="h-6 w-6 animate-spin" />
                <span>Loading PDF...</span>
              </div>
            </div>
          )}
          
          <iframe
            ref={iframeRef}
            className="w-full h-full border-0"
            title={`PDF Viewer - ${templateName || 'Document'}`}
            onLoad={() => setIsLoading(false)}
          />
        </div>
      </CardContent>
    </Card>
  )
}
