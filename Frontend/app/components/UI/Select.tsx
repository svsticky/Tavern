import { cn } from "~/util/tailwind.util";
import RequiredAsterisk from "./RequiredAstrix";

/**
 * Represents an individual option within a select dropdown.
 * @typedef {Object} Option
 * @property {string | number} value - The internal value passed to the form state.
 * @property {string} label - The human-readable text displayed in the dropdown list.
 */
type Option = {
  value: string | number;
  label: string;
};

/**
 * Props for the Select component.
 * Extends standard HTML select attributes to support native functionality.
 *
 * @interface SelectProps
 * @extends {React.SelectHTMLAttributes<HTMLSelectElement>}
 * @property {string | null} [label=null] - The text label displayed above the dropdown.
 * @property {Option[]} options - An array of value-label pairs to populate the dropdown.
 */
interface SelectProps extends React.SelectHTMLAttributes<HTMLSelectElement> {
  label?: string | null;
  options: Option[];
}

/**
 * A styled dropdown selection component.
 *
 * This component provides a standardized wrapper for the native `<select>` element,
 * including a vertical label layout and consistent padding, borders, and focus
 * rings to match the rest of the form suite. It maps through the provided `options`
 * array to generate the child `<option>` elements automatically.
 *
 * @component
 * @param {SelectProps} props - The component properties and native select attributes.
 */
export default function Select({
  label = null,
  options,
  ...props
}: SelectProps) {
  return (
    <label className="flex flex-col gap-1 w-full">
      {label && (
        <div className="flex">
          <span className="text-sm font-medium text-gray-700">{label}</span>
          <RequiredAsterisk required={props.required || false} />
        </div>
      )}
      <select
        {...props}
        className={cn(
          "h-7 px-2 py-auto rounded-md border border-gray-200 focus:ring-2 outline-none transition-all hover:cursor-pointer",
          props.disabled &&
            "bg-gray-100 cursor-not-allowed text-gray-500",
        )}
      >
        {options.map((opt) => (
          <option key={opt.value} value={opt.value}>
            {opt.label}
          </option>
        ))}
      </select>
    </label>
  );
}
