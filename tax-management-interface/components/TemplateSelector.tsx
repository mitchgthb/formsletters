"use client";

import React from "react";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Button } from "@/components/ui/button";
import { Upload } from "lucide-react";

export interface TemplateSelectorProps {
  templates: any[];
  selectedTemplate: string | null;
  onChange: (value: string | null) => void;
  loading?: boolean;
}

export const TemplateSelector: React.FC<TemplateSelectorProps> = ({
  templates,
  selectedTemplate,
  onChange,
  loading = false,
}) => {
  return (
    <div className="space-y-2">
      <Select
        value={selectedTemplate ?? ""}
        onValueChange={(value) => onChange(value || null)}
        disabled={loading}
      >
        <SelectTrigger className="w-full">
          <SelectValue placeholder={loading ? "Loading templates..." : "Select a template"} />
        </SelectTrigger>
        <SelectContent>
          {templates.map((t) => (
            <SelectItem key={t.Path ?? t.path} value={t.Path ?? t.path}>
              {t.Name ?? t.name}
            </SelectItem>
          ))}
        </SelectContent>
      </Select>
    </div>
  );
};
