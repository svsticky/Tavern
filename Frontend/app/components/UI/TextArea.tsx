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
export default function TextArea({ label, ...props }: any) {
  return (
    <label className="flex flex-col gap-1 w-full">
      <div className="flex">
        <span className="text-sm font-medium text-gray-700">{label}</span>
        <RequiredAsterisk required={props.required || false} />
      </div>
      <textarea
        {...props}
        className="border p-2 rounded-lg focus:ring-2 outline-none transition-all"
      />
    </label>
  );
}
