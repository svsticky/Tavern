/**
 * A styled checkbox component with an integrated label.
 *
 * This component wraps a standard HTML checkbox input within a label element
 * to provide a larger, more accessible click area. It includes hover states
 * for both the input and the text, utilizing Tailwind transitions for a smooth
 * color shift on the label.
 *
 * @component
 * @param {Object} props - Component properties.
 * @param {string} props.label - The text to display alongside the checkbox.
 * @param {React.InputHTMLAttributes<HTMLInputElement>} props... - Any other native HTML input attributes (e.g., checked, onChange, disabled).
 */
export default function Checkbox({ label, ...props }: any) {
  return (
    <label className="flex items-center gap-2 cursor-pointer group">
      <input
        type="checkbox"
        {...props}
        className="w-5 h-5 accent-[var(--primary-board)] rounded border-gray-300 hover:cursor-pointer"
      />
      <span className="text-sm font-medium text-gray-700 group-hover:text-black transition-colors">
        {label}
      </span>
    </label>
  );
}
