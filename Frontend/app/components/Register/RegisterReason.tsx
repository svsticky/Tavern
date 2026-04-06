import type { LucideIcon } from "lucide-react";
import Tile from "../Tiles/Tile";
import { cn } from "~/util/tailwind.util";

interface RegisterReasonProps {
    title: string;
    description: string;
    icon: LucideIcon;
    className?: string;
}

export default function RegisterReason({ title, icon: Icon, description, className }: RegisterReasonProps) {
    return (
        <Tile className={cn("border border-gray-200 hover:border-(--board-primary) hover:shadow-lg transition-all duration-300 flex flex-col items-start gap-3 bg-white", className)}>
            <Icon className="w-8 h-8 text-(--board-primary)" />
            
            <div className="space-y-1">
                <h3 className="text-lg font-semibold text-gray-900">{title}</h3>
                <p className="text-sm text-gray-600 leading-relaxed">{description}</p>
            </div>
        </Tile>
    );
}