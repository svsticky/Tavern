import type { LucideIcon } from "lucide-react";
import { ChevronDown } from "lucide-react"; 
import { useState, type ReactNode } from "react";
import Tile from "./Tile";
import { cn } from "~/util/tailwind.util";

interface BorderedTileProps {
  title?: string | null;
  subtitle?: string | null;
  icon?: LucideIcon;
  className?: string;
  children: ReactNode; 
  collapsibleContent?: ReactNode; 
  defaultOpen?: boolean;
  noPadding?: boolean;
}
export default function BorderedTile({
  title = null,
  subtitle = null,
  icon: Icon,
  className,
  children,
  collapsibleContent,
  defaultOpen = false,
  noPadding = false
}: BorderedTileProps) {
  const [isOpen, setIsOpen] = useState(defaultOpen);

  const hasBottomContent = !!subtitle || !!children;

  return (
    <Tile className={cn("border border-gray-200 flex flex-col p-0", className)}>
      <div 
        className={cn(
          "p-4 transition-colors flex flex-col", 
          collapsibleContent && "cursor-pointer hover:bg-slate-50", noPadding && "p-0"
        )}
        onClick={() => collapsibleContent && setIsOpen(!isOpen)}
      >
        <div className="flex justify-between items-start">
          {title ? (
            <span className="text-slate-500 font-medium text-[1.1rem]">
              {title}
            </span>
          ) : (
            <div /> 
          )}
          
          <div className="flex items-center gap-3">
            {Icon && (
              <div className="bg-orange-50 p-3 rounded-2xl flex items-center justify-center">
                <Icon className="w-6 h-6 text-orange-600" strokeWidth={2.5} />
              </div>
            )}
            {collapsibleContent && (
              <ChevronDown 
                className={cn("w-5 h-5 text-slate-400 transition-transform duration-200", isOpen && "rotate-180")} 
              />
            )}
          </div>
        </div>

        {hasBottomContent && (
          <div className="flex flex-col gap-4 mt-1"> 
            {subtitle && (
              <span className="text-sm text-slate-400">
                {subtitle}
              </span>
            )}

            {children && (
              <div className="flex flex-col w-full">
                {children}
              </div>
            )}
          </div>
        )}
      </div>

      {collapsibleContent && (
        <div className={cn(
          "grid transition-all duration-200 ease-in-out border-t border-slate-50 bg-white",
          isOpen ? "grid-rows-[1fr] opacity-100" : "grid-rows-[0fr] opacity-0"
        )}>
          <div className="overflow-hidden">
            <div className="p-4">
               {collapsibleContent}
            </div>
          </div>
        </div>
      )}
    </Tile>
  );
}