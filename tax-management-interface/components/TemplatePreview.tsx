"use client";

import React, { useRef, useEffect } from "react";
import { Alert, AlertDescription } from "@/components/ui/alert";
import { Progress } from "@/components/ui/progress";

export interface TemplatePreviewProps {
  html?: string | null;
  loading: boolean;
  error: string | null;
  onChange?: (value: string) => void;
  onContentReady?: (getHtml: () => string) => void;
  // New props for internal logic
  templates?: any[];
  selectedTemplate?: string | null;
  letterPreviews?: Record<number, string>;
  templateHtml?: string | null;
  client?: any;
  // Header props
  departmentName?: string;
  departmentAddress?: string;
  departmentPhone?: string;
  departmentEmail?: string;
  // Client props
  clientId?: string | number;
  clientName?: string;
  clientAddress?: string;
  clientTaxId?: string;
  // Template props
  templateName?: string;
  // Signature props
  signatureClosing?: string;
  signerName?: string;
  signerTitle?: string;
  signerDepartment?: string;
  // Date
  date?: string;
}

export const TemplatePreview: React.FC<TemplatePreviewProps> = ({ 
  html,
  loading, 
  error, 
  onChange,
  onContentReady,
  // New props for internal logic
  templates = [],
  selectedTemplate = null,
  letterPreviews = {},
  templateHtml = null,
  client = null,
  // Header defaults
  departmentName = "Tax Management Department",
  departmentAddress = "123 Government Plaza, Tax City, TC 12345",
  departmentPhone = "(555) 123-4567",
  departmentEmail = "info@taxdept.gov",
  // Client defaults - will be overridden by client data if provided
  clientId = "",
  clientName = "",
  clientAddress = "",
  clientTaxId = "",
  // Template defaults
  templateName = "Template",
  // Signature defaults
  signatureClosing = "Best regards,",
  signerName = "John Smith",
  signerTitle = "Senior Tax Officer",
  signerDepartment = "Tax Management Department",
  // Date default
  date = new Date().toLocaleDateString()
}) => {

  const editorRef = useRef<HTMLDivElement | null>(null);

  // Function to get current HTML content
  const getCurrentHtml = () => {
    return editorRef.current?.innerHTML || '';
  };

  // Expose the getCurrentHtml function to parent component
  useEffect(() => {
    if (onContentReady) {
      onContentReady(getCurrentHtml);
    }
  }, [onContentReady]);

  // Internal logic moved from renderLetterPreview
  const getTemplateData = () => {
    if (client && templates && selectedTemplate) {
      // Find the selected template
      const template = templates.find(t => (t.Path ?? t.path) === selectedTemplate);
      
      // Get HTML from letterPreviews or fallback to templateHtml
      const resolvedHtml = letterPreviews[client.id] ?? templateHtml ?? '<p>Loading...</p>';
      
      return {
        html: resolvedHtml,
        clientId: client.id,
        clientName: client.name,
        clientAddress: client.address,
        clientTaxId: client.taxId,
        templateName: template ? (template.name || template.Name) : 'Template'
      };
    }
    
    // Fallback to provided props
    return {
      html: html ?? templateHtml ?? '<p>Loading...</p>',
      clientId,
      clientName,
      clientAddress,
      clientTaxId,
      templateName
    };
  };

  const templateData = getTemplateData();

  // Function to replace merge fields with actual client data
  const replaceMergeFields = (htmlContent: string) => {
    if (!htmlContent) return htmlContent;
    
    let processedContent = htmlContent;
    
    // Replace common merge fields with client data
    const mergeFields = {
      '[contact_name]': templateData.clientName || '[contact_name]',
      '[company_name]': templateData.clientName || '[company_name]', // Often company name = client name
      '[email]': client?.email || client?.Email || '[email]',
      '[contact_address_inline]': templateData.clientAddress || '[contact_address_inline]',
      '[employee]': 'Tax Officer', // Default value
      '[project_ids]': client?.projectIds || 'N/A',
      // Add more merge fields as needed
      '{{ClientName}}': templateData.clientName || '{{ClientName}}',
      '{{ClientAddress}}': templateData.clientAddress || '{{ClientAddress}}',
      '{{ClientTaxId}}': templateData.clientTaxId || '{{ClientTaxId}}',
      '{{ClientEmail}}': client?.email || client?.Email || '{{ClientEmail}}',
      '{{ClientId}}': templateData.clientId || '{{ClientId}}'
    };
    
    // Replace all merge fields
    Object.entries(mergeFields).forEach(([field, value]) => {
      const regex = new RegExp(field.replace(/[.*+?^${}()|[\]\\]/g, '\\$&'), 'gi');
      processedContent = processedContent.replace(regex, value);
    });
    
    return processedContent;
  };

  // When html updates, inject into editor with merge fields replaced
  useEffect(() => {
    if (editorRef.current) {
      if (templateData.html && templateData.html !== '<p>Loading...</p>') {
        const processedHtml = replaceMergeFields(templateData.html);
        editorRef.current.innerHTML = processedHtml;
        
        // Trigger onChange to update the parent state with processed content
        if (onChange) {
          onChange(processedHtml);
        }
      } else if (templateData.html === '<p>Loading...</p>' || !templateData.html) {
        // Show placeholder content when no template is loaded
        editorRef.current.innerHTML = '<p style="color: #6b7280; font-style: italic;">Select a template and client to see the letter content here...</p>';
      }
    }
  }, [templateData.html, templateData.clientName, templateData.clientAddress, templateData.clientTaxId]);

  if (loading) {
    return (
      <div className="p-4 text-center">
        <Progress value={30} className="w-[60%] mx-auto" />
        <p className="text-sm text-muted-foreground mt-2">Loading template preview...</p>
      </div>
    );
  }

  if (error) {
    return (
      <Alert variant="destructive" className="my-4">
        <AlertDescription>{error}</AlertDescription>
      </Alert>
    );
  }

  return (

    <div className="bg-white border rounded-lg p-4 sm:p-6 lg:p-8 shadow-sm overflow-auto" style={{ minHeight: "400px", fontFamily: "serif" }}>
      {/* Letter Header */}
      <div className="text-center mb-6 sm:mb-8 pb-4 border-b">
        <h1 className="text-lg sm:text-xl lg:text-2xl font-bold text-gray-800">{departmentName}</h1>
        <p className="text-sm sm:text-base text-gray-600 mt-2">{departmentAddress}</p>
        <p className="text-sm sm:text-base text-gray-600">Phone: {departmentPhone} | Email: {departmentEmail}</p>
      </div>

      {/* Date and Reference */}
      <div className="mb-4 sm:mb-6">
        <p className="text-right text-sm sm:text-base text-gray-600">Date: {date}</p>
        <p className="text-right text-sm sm:text-base text-gray-600">Reference: TAX-2024-{templateData.clientId}</p>
      </div>

      {/* Client Address Block */}
      <div className="mb-6 sm:mb-8">
        <div className="bg-gray-50 p-3 sm:p-4 rounded border-l-4 border-blue-500">
          <p className="font-semibold text-sm sm:text-base">{templateData.clientName}</p>
          <p className="text-sm sm:text-base">{templateData.clientAddress}</p>
          <p className="text-sm sm:text-base">Tax ID: {templateData.clientTaxId}</p>
        </div>
      </div>

      {/* Subject Line */}
      {/* <div className="mb-6">
        <h3 className="text-lg font-medium">{templateData.templateName}</h3>
      </div> */}

      {/* Editable Letter Body */}
      <div className="mb-6 sm:mb-8">
        <div
          ref={editorRef}
          contentEditable
          suppressContentEditableWarning
          onInput={() => onChange && onChange(editorRef.current?.innerHTML || '')}
          className="min-h-64 sm:min-h-96 border-0 p-0 focus:outline-none text-sm sm:text-base leading-relaxed"
          style={{ fontFamily: 'serif' }}
        />
      </div>
      {/* bug: komt soms niet */}

      {/* Signature Block */}
      <div className="mt-8 sm:mt-12">
        <p className="mb-6 sm:mb-8 text-sm sm:text-base">{signatureClosing}</p>
        <div className="border-t border-gray-300 pt-2 w-48 sm:w-64">
          <p className="font-semibold text-sm sm:text-base">{signerName}</p>
          <p className="text-gray-600 text-sm sm:text-base">{signerTitle}</p>
          <p className="text-gray-600 text-sm sm:text-base">{signerDepartment}</p>
        </div>
      </div>
    </div>
  );
};
