'use client'

import React, { useState, useEffect, useRef } from 'react'
import { Button } from '@/components/ui/button'
import { Loader2, AlertCircle } from 'lucide-react'
import { Alert, AlertDescription } from '@/components/ui/alert'

interface LibreOfficeEditorProps {
  templateId: string
  clientId: string
  onSave?: (content: string) => void
  onClose?: () => void
}

interface WopiSession {
  sessionId: string
  wopiSrc: string
  accessToken: string
  documentUrl: string
}

export function LibreOfficeEditor({ templateId, clientId, onSave, onClose }: LibreOfficeEditorProps) {
  const [isLoading, setIsLoading] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [wopiSession, setWopiSession] = useState<WopiSession | null>(null)
  const iframeRef = useRef<HTMLIFrameElement>(null)

  const API_BASE_URL = process.env.NEXT_PUBLIC_API_BASE_URL || 'https://localhost:7069/'
  const LIBREOFFICE_URL = process.env.NEXT_PUBLIC_LIBREOFFICE_URL || 'http://localhost:9980'

  useEffect(() => {
    prepareDocument()
  }, [templateId, clientId])

  const prepareDocument = async () => {
    setIsLoading(true)
    setError(null)

    try {
      console.log('Preparing document for real LibreOffice Online...')
      
      const response = await fetch(`${API_BASE_URL}api/wopi/prepare-document`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify({
          templateName: templateId,
          clientId
        })
      })

      if (!response.ok) {
        throw new Error(`Failed to prepare document: ${response.status} ${response.statusText}`)
      }

      const session: WopiSession = await response.json()
      console.log('WOPI session prepared:', session)
      
      setWopiSession(session)
      
    } catch (error) {
      console.error('Error preparing document:', error)
      setError(error instanceof Error ? error.message : 'Failed to prepare document for editing')
    } finally {
      setIsLoading(false)
    }
  }

  const handleIframeLoad = () => {
    console.log('LibreOffice Online iframe loaded')
    setIsLoading(false)
  }

  const handleIframeError = () => {
    console.error('LibreOffice Online iframe failed to load')
    setError('Failed to load LibreOffice Online. Please ensure the service is running on ' + LIBREOFFICE_URL)
    setIsLoading(false)
  }

  const handleSave = async () => {
    if (!wopiSession) return

    try {
      setIsLoading(true)
      
      // Send a save message to LibreOffice Online
      if (iframeRef.current?.contentWindow) {
        iframeRef.current.contentWindow.postMessage({
          MessageId: 'Host_PostmessageReady',
          Values: {
            PostMessageOrigin: window.location.origin
          }
        }, LIBREOFFICE_URL)

        iframeRef.current.contentWindow.postMessage({
          MessageId: 'Action_Save'
        }, LIBREOFFICE_URL)
      }

      // The actual save will be handled by the WOPI protocol
      // LibreOffice Online will call PutFile on our WOPI endpoint
      
      if (onSave) {
        onSave('Document saved via LibreOffice Online')
      }
      
    } catch (error) {
      console.error('Error saving document:', error)
      setError('Failed to save document')
    } finally {
      setIsLoading(false)
    }
  }

  // Listen for messages from LibreOffice Online
  useEffect(() => {
    const handleMessage = (event: MessageEvent) => {
      if (event.origin !== LIBREOFFICE_URL) return

      console.log('Message from LibreOffice Online:', event.data)

      switch (event.data.MessageId) {
        case 'App_LoadingStatus':
          if (event.data.Values?.Status === 'Document_Loaded') {
            setIsLoading(false)
            console.log('Document loaded in LibreOffice Online')
          }
          break
        case 'Action_Save_Resp':
          console.log('Document saved successfully')
          break
        case 'App_Error':
          setError(`LibreOffice Online error: ${event.data.Values?.ErrorMsg || 'Unknown error'}`)
          break
      }
    }

    window.addEventListener('message', handleMessage)
    return () => window.removeEventListener('message', handleMessage)
  }, [LIBREOFFICE_URL])

  if (error) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[600px] p-6">
        <Alert className="max-w-md mb-4">
          <AlertCircle className="h-4 w-4" />
          <AlertDescription>{error}</AlertDescription>
        </Alert>
        
        <div className="space-y-4 text-center">
          <div className="text-sm text-muted-foreground">
            <p>To use real LibreOffice Online, you need:</p>
            <ol className="list-decimal list-inside mt-2 space-y-1">
              <li>LibreOffice Online running on {LIBREOFFICE_URL}</li>
              <li>Docker with Linux containers (requires WSL2 on Windows)</li>
            </ol>
          </div>
          
          <div className="space-x-2">
            <Button onClick={prepareDocument} variant="outline">
              Retry
            </Button>
            {onClose && (
              <Button onClick={onClose} variant="secondary">
                Close
              </Button>
            )}
          </div>
        </div>
      </div>
    )
  }

  if (isLoading || !wopiSession) {
    return (
      <div className="flex flex-col items-center justify-center min-h-[600px]">
        <Loader2 className="h-8 w-8 animate-spin mb-4" />
        <p className="text-sm text-muted-foreground">
          {wopiSession ? 'Loading LibreOffice Online...' : 'Preparing document...'}
        </p>
        <p className="text-xs text-muted-foreground mt-2">
          Connecting to {LIBREOFFICE_URL}
        </p>
      </div>
    )
  }

  return (
    <div className="flex flex-col h-full">
      {/* Toolbar */}
      <div className="flex justify-between items-center p-4 border-b bg-background">
        <div className="flex items-center space-x-2">
          <h3 className="text-lg font-semibold">LibreOffice Online</h3>
          <span className="text-sm text-muted-foreground">
            Session: {wopiSession.sessionId.substring(0, 8)}...
          </span>
        </div>
        
        <div className="flex space-x-2">
          <Button onClick={handleSave} size="sm">
            Save Document
          </Button>
          {onClose && (
            <Button onClick={onClose} variant="outline" size="sm">
              Close
            </Button>
          )}
        </div>
      </div>

      {/* LibreOffice Online iframe */}
      <div className="flex-1 relative">
        <iframe
          ref={iframeRef}
          src={wopiSession.documentUrl}
          className="w-full h-full border-0"
          onLoad={handleIframeLoad}
          onError={handleIframeError}
          sandbox="allow-same-origin allow-scripts allow-forms allow-popups allow-popups-to-escape-sandbox"
          title="LibreOffice Online Editor"
        />
      </div>
    </div>
  )
}
