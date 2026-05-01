/**
 * A container component for the bottom section of a menu (SideBar or Dropdown).
 * It applies a subtle top border to visually separate footer content, such as
 * user profiles or settings, from the main navigation links.
 *
 * @component
 * @param {Object} props - The component properties.
 * @param {React.ReactNode} [props.children] - The elements to be rendered inside the footer.
 */
export default function MenuFooter({
  children,
}: {
  children?: React.ReactNode;
}) {
  return <div className="border-t border-white/20">{children}</div>;
}
