import React, { useState } from 'react';
import dynamic from 'next/dynamic';

// TipTap editor (dynamic import for SSR)
const EditorContent = dynamic(
  () => import('@tiptap/react').then(mod => mod.EditorContent),
  { ssr: false }
);
const useEditor = dynamic(
  () => import('@tiptap/react').then(mod => mod.useEditor),
  { ssr: false }
);

export default function LetterGenerator() {
  const [templates, setTemplates] = useState([]);
  const [clients, setClients] = useState([]);
  const [templateName, setTemplateName] = useState('');
  const [clientId, setClientId] = useState('');
  const [editor, setEditor] = useState(null);
  const [pdfPath, setPdfPath] = useState('');
  const [recipientEmail, setRecipientEmail] = useState('');
  const [sendStatus, setSendStatus] = useState('');

  // Fetch templates and clients on mount
  React.useEffect(() => {
    const fetchTemplates = async () => {
      const res = await fetch('/templates');
      const data = await res.json();
      setTemplates(data);
      if (data.length > 0) setTemplateName(data[0].name || data[0].Name || '');
    };
    const fetchClients = async () => {
      const res = await fetch('/clients');
      const data = await res.json();
      setClients(data);
      if (data.length > 0) setClientId(data[0].id || data[0].Id || '');
    };
    fetchTemplates();
    fetchClients();
  }, []);

  // Load editable HTML from backend
  const loadTemplate = async () => {
    const res = await fetch('/parse-template', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ templateName }),
    });
    const data = await res.json();
    // Initialize TipTap editor with returned HTML
    const { Editor } = await import('@tiptap/react');
    const { StarterKit } = await import('@tiptap/starter-kit');
    const ed = new Editor({
      extensions: [StarterKit],
      content: data.editableHtml || '',
    });
    setEditor(ed);
  };

  // Generate PDF
  const generateLetter = async () => {
    if (!editor) return;
    const res = await fetch('/generate', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ clientId, templateName, updatedHtml: editor.getHTML() }),
    });
    const data = await res.json();
    setPdfPath(data.pdfPath || '');
  };

  // Send PDF
  const sendLetter = async () => {
    const res = await fetch('/send', {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({
        pdfPath,
        sendViaEmail: true,
        recipientEmail,
        sendViaDocuSign: false,
      }),
    });
    setSendStatus(res.ok ? 'Sent!' : 'Error sending');
  };

  return (
    <div style={{ maxWidth: 800, margin: 'auto' }}>
      <h2>Letter Generator (TipTap)</h2>
      <div>
        <label>Template: </label>
        <select value={templateName} onChange={e => setTemplateName(e.target.value)}>
          {templates.map(t => (
            <option key={t.name || t.Name} value={t.name || t.Name}>{t.name || t.Name}</option>
          ))}
        </select>
        <button onClick={loadTemplate} disabled={!templateName}>Load Template</button>
      </div>
      <div>
        <label>Client: </label>
        <select value={clientId} onChange={e => setClientId(e.target.value)}>
          {clients.map(c => (
            <option key={c.id || c.Id} value={c.id || c.Id}>{c.name || c.Name} ({c.email || c.Email})</option>
          ))}
        </select>
      </div>
      {editor && <EditorContent editor={editor} style={{ minHeight: 200, border: '1px solid #ccc', margin: '20px 0' }} />}
      <button onClick={generateLetter} disabled={!editor}>Generate PDF</button>
      {pdfPath && (
        <div>
          <p>PDF Path: {pdfPath}</p>
          <label>Recipient Email: </label>
          <input value={recipientEmail} onChange={e => setRecipientEmail(e.target.value)} />
          <button onClick={sendLetter}>Send Letter</button>
          <p>{sendStatus}</p>
        </div>
      )}
    </div>
  );
}
