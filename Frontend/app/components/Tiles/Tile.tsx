import type { ReactNode } from "react";
import { cn } from "~/util/tailwind.util";

/**
 * Props for the Tile component.
 * @typedef {Object} TileProps
 * @property {string} [className] - Optional CSS classes to customize styling (e.g., background, borders, or shadows).
 * @property {ReactNode} [children] - The content to be rendered inside the tile.
 */
type TileProps = {
  className?: string;
  children?: ReactNode;
};

/**
 * A fundamental layout building block that provides a consistent container style.
 * 
 * The Tile component serves as the base for most UI cards, providing standard 
 * properties like rounded corners, internal padding, and overflow containment. 
 * It is designed to be highly composable and easily extended via Tailwind classes.
 * 
 * @component
 * @param {TileProps} props - The component properties.
 */
export default function Tile({ className, children }: TileProps) {
  return (
    <div className={cn("box-border rounded-2xl p-5 overflow-hidden", className)}>
      {children}
    </div>
  );
}
