const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:5001";

// Helper function to sanitize HTML for JSON transmission
function sanitizeHtmlForJson(html: string): string {
  // Replace double quotes in style attributes with single quotes
  return html.replace(/style="([^"]*)"/g, "style='$1'");
}

export async function fetchTemplateHtml(templateName: string): Promise<string> {
  const res = await fetch(`${API_BASE}/templates/parse-template`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify({ templatePath: templateName }),
  });
  if (!res.ok) throw new Error(`Failed to parse template: ${res.status}`);
  const json = await res.json();
  return json.editableHtml as string;
}

export async function fetchClientTemplateData(clientId: number, templateName: string): Promise<Record<string, string>> {
  const res = await fetch(`${API_BASE}/client-info/${clientId}/data?template=${encodeURIComponent(templateName)}`);
  if (!res.ok) throw new Error(`Failed to fetch template data: ${res.status}`);
  const json = await res.json() as { data: Record<string,string> };
  return json.data;
}

export function applyPlaceholders(html: string, data: Record<string, string>): string {
  let populated = html;
  for (const [key, value] of Object.entries(data)) {
    populated = populated.replaceAll(`[${key}]`, value ?? "");
  }
  return populated;
}

export function buildHeaderHtml(data: Record<string, string>): string {
  const {
    dept_name = "Tax Management Department",
    dept_address = "123 Government Plaza, Tax City, TC 12345",
    dept_phone = "(555) 123-4567",
    dept_email = "info@taxdept.gov",
    date = new Date().toLocaleDateString(),
    reference = "TAX-2024-1",
    client_name = "Acme Corporation",
    client_address = "123 Business St, City, State 12345",
    client_tax_id = "12-3456789",
  } = data;

  return `
    <div style="font-family:serif;">
      <div style="text-align:center;margin-bottom:24px;">
        <h2 style="margin:0">${dept_name}</h2>
        <p style="margin:0">${dept_address}</p>
        <p style="margin:0">Phone: ${dept_phone} | Email: ${dept_email}</p>
      </div>
      <hr style="margin:16px 0;">
      <div style="text-align:right;margin-bottom:24px;">
        <p style="margin:0">Date: ${date}</p>
        <p style="margin:0">Reference: ${reference}</p>
      </div>
      <div style="border-left:4px solid #1e3a8a;padding-left:12px;text-align:left;max-width:400px;margin:0 auto;">
        <p style="margin:0;font-weight:bold">${client_name}</p>
        <p style="margin:0">${client_address}</p>
        <p style="margin:0">Tax ID: ${client_tax_id}</p>
      </div>
    </div>
  `;
}

export function buildEndingHtml(data: Record<string, string>): string {
  const {
    sign_off = "Best regards,",
    signer_name = "John Smith",
    signer_title = "Senior Tax Officer",
    signer_department = "Tax Management Department",
  } = data;

  return `
    <div style="margin-top:32px;font-family:serif;">
      <p>${sign_off}</p>
      <div style="border-top:1px solid #d1d5db;width:250px;padding-top:8px;margin-top:24px;">
        <p style="margin:0;font-weight:bold">${signer_name}</p>
        <p style="margin:0;color:#4b5563;">${signer_title}</p>
        <p style="margin:0;color:#4b5563;">${signer_department}</p>
      </div>
    </div>
  `;
}

export async function buildPreviewHtml(templateName: string, clientId: number): Promise<string> {
  const [html, data] = await Promise.all([
    fetchTemplateHtml(templateName),
    fetchClientTemplateData(clientId, templateName),
  ]);
  const populatedBody = applyPlaceholders(html, data);
  // return `${buildHeaderHtml(data)}${populatedBody}${buildEndingHtml(data)}`;
  return `${populatedBody}`;
}

export interface GenerateLetterRequest {
  templateName: string;
  clientId: number;
}

export interface GenerateDocumentResponse {
  pdfPath: string;
}

export async function generatePdfLetter(templateName: string, clientId: number): Promise<GenerateDocumentResponse> {
  const request: GenerateLetterRequest = {
    templateName: templateName.split(/[/\\]/).pop() ?? templateName,
    clientId
  };

  const res = await fetch(`${API_BASE}/letters/generate`, {
    method: "POST",
    headers: { "Content-Type": "application/json" },
    body: JSON.stringify(request),
  });
  
  if (!res.ok) throw new Error(`Failed to generate PDF: ${res.status}`);
  return (await res.json()) as GenerateDocumentResponse;
}

export function getPdfUrl(fileName: string): string {
  return `${API_BASE}/letters/pdf/${encodeURIComponent(fileName)}`;
}

export interface GenerateLetterFromHtmlRequest {
  templateName: string;
  clientId: number;
  updatedHtml: string;
}

export function buildCompleteLetterHtml(
  bodyHtml: string,
  clientData: {
    id: number;
    name: string;
    address: string;
    taxId: string;
    email?: string;
  },
  options: {
    departmentName?: string;
    departmentAddress?: string;
    departmentPhone?: string;
    departmentEmail?: string;
    signerName?: string;
    signerTitle?: string;
    signerDepartment?: string;
    signatureClosing?: string;
    date?: string;
  } = {}
): string {
  const {
    departmentName = "Tax Management Department",
    departmentAddress = "123 Government Plaza, Tax City, TC 12345",
    departmentPhone = "(555) 123-4567",
    departmentEmail = "info@taxdept.gov",
    signerName = "John Smith",
    signerTitle = "Senior Tax Officer",
    signerDepartment = "Tax Management Department",
    signatureClosing = "Best regards,",
    date = new Date().toLocaleDateString()
  } = options;

  return `
    <div style='background: white; padding: 2rem; font-family: serif; min-height: 600px;'>
      <!-- Letter Header -->
      <div style='text-align: center; margin-bottom: 2rem; padding-bottom: 1rem; border-bottom: 1px solid #e5e7eb;'>
        <h1 style='font-size: 1.5rem; font-weight: bold; color: #1f2937; margin: 0;'>${departmentName}</h1>
        <p style='color: #6b7280; margin: 0.5rem 0 0 0;'>${departmentAddress}</p>
        <p style='color: #6b7280; margin: 0;'>Phone: ${departmentPhone} | Email: ${departmentEmail}</p>
      </div>

      <!-- Date and Reference -->
      <div style='margin-bottom: 1.5rem;'>
        <p style='text-align: right; color: #6b7280; margin: 0;'>Date: ${date}</p>
        <p style='text-align: right; color: #6b7280; margin: 0;'>Reference: TAX-2024-${clientData.id}</p>
      </div>

      <!-- Client Address Block -->
      <div style='margin-bottom: 2rem;'>
        <div style='background: #f9fafb; padding: 1rem; border-radius: 0.25rem; border-left: 4px solid #3b82f6;'>
          <p style='font-weight: 600; margin: 0;'>${clientData.name}</p>
          <p style='margin: 0;'>${clientData.address}</p>
          <p style='margin: 0;'>Tax ID: ${clientData.taxId}</p>
        </div>
      </div>

      <!-- Letter Body -->
      <div style='margin-bottom: 2rem;'>
        ${bodyHtml}
      </div>

      <!-- Signature Block -->
      <div style='margin-top: 3rem;'>
        <p style='margin-bottom: 2rem;'>${signatureClosing}</p>
        <div style='border-top: 1px solid #d1d5db; padding-top: 0.5rem; width: 16rem;'>
          <p style='font-weight: 600; margin: 0;'>${signerName}</p>
          <p style='color: #6b7280; margin: 0;'>${signerTitle}</p>
          <p style='color: #6b7280; margin: 0;'>${signerDepartment}</p>
        </div>
      </div>
    </div>
  `;
}

export async function generatePdfFromHtml(templateName: string, clientId: number, completeHtml: string): Promise<GenerateDocumentResponse> {
  console.log("Generated HTML:", completeHtml)

  // Sanitize HTML to prevent JSON parsing issues
  const sanitizedHtml = sanitizeHtmlForJson(completeHtml);

  const request: GenerateLetterFromHtmlRequest = {
    templateName: templateName.split(/[/\\]/).pop() ?? templateName,
    clientId,
    updatedHtml: sanitizedHtml
  };

  console.log("Request payload:", JSON.stringify(request, null, 2))

  try {
    const res = await fetch(`${API_BASE}/letters/generate-from-html`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(request),
    });
    
    if (!res.ok) {
      const errorText = await res.text();
      console.error(`API Error (${res.status}):`, errorText);
      throw new Error(`Failed to generate PDF: ${res.status} - ${errorText}`);
    }
    return (await res.json()) as GenerateDocumentResponse;
  } catch (error) {
    console.error("Error in generatePdfFromHtml:", error);
    throw error;
  }
}
