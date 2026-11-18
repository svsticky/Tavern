import { useEffect, useState } from "react";
import { cn } from "~/util/tailwind.util";
import RequiredAsterisk from "./RequiredAstrix";

/**
 * A styled multi-line text input component.
 *
 * This component provides a standardized wrapper for the native `<textarea>` element,
 * maintaining consistency with other form components like `Input` and `Select`.
 * It features a vertical layout with a label positioned above the input area and
 * includes smooth transition effects for focus states.
 *
 * @component
 * @param {Object} props - Component properties.
 * @param {string} props.label - The descriptive text to display above the text area.
 * @param {React.TextareaHTMLAttributes<HTMLTextAreaElement>} props... - All standard HTML textarea attributes (e.g., rows, value, onChange, placeholder).
 */
export default function TextArea({
  label,
  onChange,
  value,
  defaultValue,
  maxLength,
  ...props
}: any) {
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

  const handleChange = (e: React.ChangeEvent<HTMLTextAreaElement>) => {
    setCharCount(e.target.value.length);
    onChange?.(e);
  };

  return (
    <label className="flex flex-col gap-1 w-full">
      <div className="flex justify-between items-center">
        <div className="flex">
          <span className="text-sm font-medium text-gray-700">{label}</span>
          <RequiredAsterisk required={props.required || false} />
        </div>
        {maxLength !== undefined && (
          <span data-testid="char-counter" className="text-xs text-gray-500">
            {charCount} / {maxLength}
          </span>
        )}
      </div>
      <textarea
        {...props}
        value={value}
        defaultValue={defaultValue}
        maxLength={maxLength}
        onChange={handleChange}
        className={cn(
          "p-2 rounded-md border border-gray-200 focus:ring-2 outline-none transition-all",
          props.disabled && "bg-gray-100 cursor-not-allowed text-gray-500",
        )}
      />
    </label>
  );
}
