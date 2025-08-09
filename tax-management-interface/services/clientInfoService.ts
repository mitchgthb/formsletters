const API_BASE = process.env.NEXT_PUBLIC_API_BASE_URL ?? "https://localhost:5001";

export interface ClientInfo {
  id: number;
  name: string;
  address: string;
  taxNumber: string;
  email: string;
}

export async function fetchClients(): Promise<ClientInfo[]> {
  const res = await fetch(`${API_BASE}/client-info`);
  if (!res.ok) throw new Error(`Failed to fetch clients: ${res.status}`);
  return (await res.json()) as ClientInfo[];
}

export async function fetchClient(id: number): Promise<ClientInfo | null> {
  const res = await fetch(`${API_BASE}/client-info/${id}`);
  if (res.status === 404) return null;
  if (!res.ok) throw new Error(`Failed to fetch client: ${res.status}`);
  return (await res.json()) as ClientInfo;
}

export async function fetchClientData(id: number, templateName: string): Promise<Record<string, any>> {
  const res = await fetch(`${API_BASE}/client-info/${id}/data?template=${encodeURIComponent(templateName)}`);
  if (!res.ok) throw new Error(`Failed to fetch client data: ${res.status}`);
  const response = await res.json();
  return response.data || {};
}
