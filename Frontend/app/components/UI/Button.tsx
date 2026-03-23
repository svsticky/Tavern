import { ArrowRight } from "lucide-react";
import { cn } from "~/util/tailwind.util";

type ButtonProps = React.ButtonHTMLAttributes<HTMLButtonElement> & {
  className?: string;
  showArrow?: boolean;
  children: React.ReactNode;
};

export default function Button({
  className,
  showArrow = false,
  children,
  ...props
}: ButtonProps) {
  return (
    <button
      {...props}
      className={cn(
        "bg-white text-(--board-primary) font-semibold px-6 py-2 rounded-lg transition",
        // Default
        "hover:bg-gray-100 cursor-pointer",
        // Disabled:
        "disabled:opacity-50 disabled:cursor-not-allowed disabled:hover:bg-white", 
        className,
      )}
    >
      {children}
      {showArrow && <ArrowRight className="inline-block ml-2" size={16} />}
    </button>
  );
}
