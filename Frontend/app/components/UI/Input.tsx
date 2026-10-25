import { useEffect, useState } from "react";
import { cn } from "~/util/tailwind.util";
import RequiredAsterisk from "./RequiredAstrix";

/**
 * A versatile input component that handles standard text-based inputs and checkboxes.
 *
 * For text-based types (text, number, email, etc.), it renders a stacked layout
 * with a label above the input field. For checkbox types, it renders an inline
 * layout with the checkbox preceding the label.
 *
 * @component
 * @param {Object} props - Component properties.
 * @param {string | null} [props.label=null] - The text label associated with the input.
 * @param {string} [props.className] - Optional CSS classes to override or extend the input styling.
 * @param {React.InputHTMLAttributes<HTMLInputElement>} props... - All standard HTML input attributes (e.g., type, value, onChange, disabled).
 */
export default function Input({
  label = null,
  className,
  onChange,
  value,
  defaultValue,
  maxLength,
  ...props
}: { label?: string | null } & React.InputHTMLAttributes<HTMLInputElement>) {
  const [charCount, setCharCount] = useState<number>(() => {
    if (value !== undefined && value !== null) return String(value).length;
    if (defaultValue !== undefined && defaultValue !== null)
      return String(defaultValue).length;
    return 0;
  });

  useEffect(() => {
    if (value !== undefined && value !== null) {
      setCharCount(String(value).length);
    } else if (defaultValue !== undefined && defaultValue !== null) {
      setCharCount(String(defaultValue).length);
    }
  }, [value, defaultValue]);

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setCharCount(e.target.value.length);
    onChange?.(e);
  };

  if (props.type === "checkbox") {
    return (
      <div className={cn("flex items-center justify-start", className)}>
        <input type="checkbox" onChange={onChange} {...props} />
        {label && <span className="ml-2 text-sm">{label}</span>}
        <RequiredAsterisk required={props.required || false} />
      </div>
    );
  }

  return (
    <label className="flex flex-col gap-1 w-full">
      {(label || maxLength !== undefined) && (
        <div className="flex justify-between items-center">
          <div className="flex">
            {label && (
              <span className="text-sm font-medium text-gray-700">{label}</span>
            )}
            <RequiredAsterisk required={props.required || false} />
          </div>
          {maxLength !== undefined && (
            <span data-testid="char-counter" className="text-xs text-gray-500">
              {charCount} / {maxLength}
            </span>
          )}
        </div>
      )}
      <input
        {...props}
        value={value}
        defaultValue={defaultValue}
        maxLength={maxLength}
        onChange={handleChange}
        className={cn(
          "transition-all outline-none w-full h-7 px-2 py-auto rounded-md border border-gray-200 focus:ring-2",
          props.disabled && "bg-gray-100 cursor-not-allowed text-gray-500",
          className,
        )}
      />
    </label>
  );
}
