/**
 * A wrapper component for the primary content area of a Menu.
 * It provides a flex-growing container with vertical scrolling support to ensure 
 * the main view remains functional even with overflow.
 * 
 * @component
 * @param {Object} props - The component properties.
 * @param {React.ReactNode} props.children - The content to be displayed within the main scrollable area.
 */
export default function MenuContent({
  children,
}: {
  children: React.ReactNode;
}) {
  return <div className="flex-1 overflow-y-auto">{children}</div>;
}
