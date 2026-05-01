import React from "react";
import { cn } from "~/util/tailwind.util";
import BorderedTile from "./BorderedTile";

/**
 * Props for the ListTile component.
 * @typedef {Object} Props
 * @property {React.ReactNode} children - The list items to be rendered within the tile.
 * @property {string} [className] - Optional CSS classes for the outer container.
 */
type Props = {
  children: React.ReactNode;
  className?: string;
};

/**
 * A layout component that renders a collection of items as a vertical list inside a BorderedTile.
 *
 * Each child is wrapped in a container with a bottom border, except for the last item,
 * creating a clean, separated list view. It automatically utilizes `BorderedTile` with
 * `noPadding` to ensure the list items extend to the edges of the card.
 *
 * @component
 * @param {Props} props - The component properties.
 */
export function ListTile({ children, className }: Props) {
  const items = React.Children.toArray(children);

  return (
    <BorderedTile className={className} noPadding>
      <div className="flex flex-col">
        {items.map((child, index) => (
          <div
            key={index}
            className={cn(
              "p-0",
              index < items.length - 1 && "border-b border-gray-200",
            )}
          >
            {child}
          </div>
        ))}
      </div>
    </BorderedTile>
  );
}
