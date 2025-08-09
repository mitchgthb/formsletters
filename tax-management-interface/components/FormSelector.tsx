"use client";

import React from "react";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";

export interface FormSelectorProps {
  forms: any[];
  selectedForm: number | null;
  onChange: (value: number | null) => void;
  loading?: boolean;
}

export const FormSelector: React.FC<FormSelectorProps> = ({
  forms,
  selectedForm,
  onChange,
  loading = false,
}) => {
  return (
    <Select
      value={selectedForm?.toString() ?? ""}
      onValueChange={(val) => onChange(val ? Number(val) : null)}
      disabled={loading}
    >
      <SelectTrigger className="w-full">
        <SelectValue placeholder={loading ? "Loading forms..." : "Select a form"} />
      </SelectTrigger>
      <SelectContent>
        {forms.map((f) => (
          <SelectItem key={f.id} value={f.id.toString()}>
            {f.name || f.Name}
          </SelectItem>
        ))}
      </SelectContent>
    </Select>
  );
};
