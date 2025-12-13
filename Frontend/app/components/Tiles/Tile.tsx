import type { ReactNode } from "react";
import { cn } from "~/util/tailwind.util";

type TileProps = {
  className?: string;
  children?: ReactNode;
};

export default function Tile({ className, children }: TileProps) {
  return (
    <div className={cn("box-border rounded-2xl p-5", className)}>
      {children}
    </div>
  );
}
