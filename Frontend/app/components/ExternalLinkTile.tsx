import { cn } from "~/util/tailwind.util";
import Tile from "./Tiles/Tile"; // Zorg dat het pad klopt
import type { ReactNode } from "react";
import BorderedTile from "./Tiles/BorderedTile";

type ExternalLinkTileProps = {
  title: string;
  description: string;
  url: string;
  icon: ReactNode;
  iconBgColor?: string;
  iconColor?: string;
};

export default function ExternalLinkTile({
  title,
  description,
  url,
  icon,
  iconBgColor = "bg-gray-100",
  iconColor = "text-gray-600",
}: ExternalLinkTileProps) {
  return (
    <a 
      href={url} 
      target="_blank" 
      rel="noopener noreferrer" 
      className="group block no-underline"
    >
      <BorderedTile className="hover:shadow-md transition-shadow">
        <div className="flex flex-row gap-5 h-full">
          {/* Icon Container */}
          <div className={cn(
            "flex h-12 w-12 shrink-0 items-center justify-center rounded-xl", 
            iconBgColor, 
            iconColor
          )}>
            {icon}
          </div>

          {/* Content */}
          <div className="flex flex-col gap-1">
            <div className="flex items-center gap-1.5">
              <h3 className="text-lg font-semibold text-slate-800 m-0">
                {title}
              </h3>
              {/* External Link Icon */}
              <svg 
                className="w-4 h-4 text-slate-400 group-hover:text-slate-600 transition-colors" 
                fill="none" 
                stroke="currentColor" 
                viewBox="0 0 24 24"
              >
                <path strokeLinecap="round" strokeLinejoin="round" strokeWidth="2" d="M10 6H6a2 2 0 00-2 2v10a2 2 0 002 2h10a2 2 0 002-2v-4M14 4h6m0 0v6m0-6L10 14" />
              </svg>
            </div>
            <p className="text-sm text-slate-500 leading-relaxed m-0">
              {description}
            </p>
          </div>
        </div>
      </BorderedTile>
    </a>
  );
}