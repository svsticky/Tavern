import React from "react";

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
  onClose,
}: {
  children?: React.ReactNode;
  onClose?: () => void;
}) {
  return (
    <div className="border-t border-white/20">
      {React.Children.map(children, (child) => {
        if (!React.isValidElement(child)) return child;

        return React.cloneElement(child as React.ReactElement<any>, {
          onClose,
        });
      })}
    </div>
  );
}
