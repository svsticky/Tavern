import React from "react";
import { cn } from "~/util/tailwind.util";
import Tile from "./Tile";

type Props = {
  text: string;
  className?: string;
};

export function NoContentTile({ text, className }: Props) {
  return (
    <Tile className={cn("p-8 text-center border-2 border-dashed border-gray-200 text-gray-500 italic", className)}>
      {text}
    </Tile>
  );
}