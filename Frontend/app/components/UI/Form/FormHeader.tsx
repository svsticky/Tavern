/**
 * A header component designed for sections within a form.
 * 
 * It displays a stylized, uppercase title in a small font weight to act as a 
 * label for a group of inputs. It also supports an optional border/spacing 
 * and a secondary slot for action elements (like buttons or badges) aligned 
 * to the right of the title.
 * 
 * @component
 * @param {Object} props - The component properties.
 * @param {string} props.title - The heading text to display.
 * @param {boolean} [props.border=true] - Whether to show a bottom border and larger margin below the header.
 * @param {React.ReactNode} [props.children] - Optional secondary content to render on the right side of the header.
 */
export const FormHeader = ({ title, border = true, children }: { title: string, border?: boolean, children?: React.ReactNode }) => (
  <div className={`flex items-end justify-between ${border ? "border-b mb-4" : "mb-2"}`}>
    <h3 className="font-bold uppercase text-xs text-gray-500 tracking-wider leading-none pb-1">
      {title}
    </h3>
    <div className="flex items-center gap-2 pb-1">
      {children}
    </div>
  </div>
);