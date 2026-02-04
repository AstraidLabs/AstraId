import type { ChangeEvent } from "react";

export type MultiLineError = {
  line: number;
  message: string;
};

type Props = {
  value: string;
  onChange: (value: string) => void;
  onBlur?: () => void;
  placeholder?: string;
  error?: string | null;
  lineErrors?: MultiLineError[];
  minRows?: number;
};

export default function MultiLineListInput({
  value,
  onChange,
  onBlur,
  placeholder,
  error,
  lineErrors = [],
  minRows = 6,
}: Props) {
  const hasError = Boolean(error) || lineErrors.length > 0;

  const handleChange = (event: ChangeEvent<HTMLTextAreaElement>) => {
    onChange(event.target.value);
  };

  return (
    <div>
      <textarea
        className={`w-full rounded-md border bg-slate-950 px-3 py-2 text-slate-100 ${
          hasError ? "border-rose-400" : "border-slate-700"
        }`}
        rows={minRows}
        value={value}
        onChange={handleChange}
        onBlur={onBlur}
        placeholder={placeholder}
      />
      {lineErrors.length > 0 && (
        <div className="mt-2 space-y-1 text-xs text-rose-300">
          {lineErrors.map((lineError, index) => (
            <div key={`${lineError.line}-${index}`}>
              {lineError.line > 0 ? `Line ${lineError.line}: ` : ""}
              {lineError.message}
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
