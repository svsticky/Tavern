import React from "react";
import { cn } from "~/util/tailwind.util";
import Tile from "./Tile";
import BorderedTile from "./BorderedTile";

type Props = {
  children: React.ReactNode;
  className?: string;
};

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
