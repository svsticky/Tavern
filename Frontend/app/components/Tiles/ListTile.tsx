import React from "react";
import { cn } from "~/util/tailwind.util";
import Tile from "./Tile";

type Props = {
  children: React.ReactNode;
  className?: string;
};

export function ListTile({ children, className }: Props) {
  const items = React.Children.toArray(children);

  return (
    <Tile className={cn("rounded-xl border border-gray-200 p-0", className)}>
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
    </Tile>
  );
}
