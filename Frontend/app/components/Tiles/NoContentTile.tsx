import { cn } from "~/util/tailwind.util";
import Tile from "./Tile";

/**
 * Props for the NoContentTile component.
 * @typedef {Object} Props
 * @property {string} text - The message to display (e.g., "No items found" or "Nothing to see here").
 * @property {string} [className] - Optional CSS classes to override or extend default styling.
 */
type Props = {
  text: string;
  className?: string;
};

/**
 * A specialized Tile component used as a placeholder when a list or container is empty.
 * 
 * It features a centered, italicized text layout with a dashed border to visually 
 * indicate an empty state or a drop zone, distinguishing it from standard content-heavy tiles.
 * 
 * @component
 * @param {Props} props - The component properties.
 */
export function NoContentTile({ text, className }: Props) {
  return (
    <Tile className={cn("p-8 text-center border-2 border-dashed border-gray-200 text-gray-500 italic", className)}>
      {text}
    </Tile>
  );
}