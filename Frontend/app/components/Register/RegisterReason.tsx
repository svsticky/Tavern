import type { LucideIcon } from "lucide-react";
import Tile from "../Tiles/Tile";
import { cn } from "~/util/tailwind.util";

/**
 * Props for the RegisterReason component.
 * @interface RegisterReasonProps
 * @property {string} title - The heading text describing the reason to register.
 * @property {string} description - A detailed explanation of the benefits or features.
 * @property {LucideIcon} icon - A Lucide icon component to be displayed at the top of the tile.
 * @property {string} [className] - Optional CSS classes for custom styling of the Tile container.
 */
interface RegisterReasonProps {
    title: string;
    description: string;
    icon: LucideIcon;
    className?: string;
}

/**
 * A display component used to highlight specific benefits or reasons for joining.
 * It renders as a card (Tile) with an icon, title, and descriptive text, featuring 
 * hover effects to indicate interactivity or importance.
 * 
 * @component
 * @param {RegisterReasonProps} props - The component properties.
 */
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