import { ArrowLeft, ArrowRight } from "lucide-react";
import { cn } from "~/util/tailwind.util";

type ButtonProps = React.ButtonHTMLAttributes<HTMLButtonElement> & {
  className?: string;
  showArrow?: boolean;
  arrowDirection?: "left" | "right";
  variant?: "primary" | "secondary";
  children: React.ReactNode;
};

export default function Button({
  className,
  showArrow = false,
  arrowDirection = "right",
  variant = "primary",
  children,
  ...props
}: ButtonProps) {
  
  const variants = {
    primary: "bg-(--board-primary) text-white hover:bg-(--board-primary-dark) shadow-sm hover:bg-(--board-primary-light)",
    secondary: "bg-white text-(--board-primary) border border-gray-200 hover:bg-gray-50",
  };

  const Icon = arrowDirection === "left" ? ArrowLeft : ArrowRight;

  return (
    <button
      {...props}
      className={cn(
        "inline-flex items-center justify-center gap-2 font-semibold px-6 py-2 rounded-lg transition-all duration-200 cursor-pointer",
        variants[variant],
        "disabled:opacity-50 disabled:cursor-not-allowed", 
        className,
      )}
    >
      {showArrow && arrowDirection === "left" && <Icon className="shrink-0" size={16} />}
      
      <span>{children}</span>
      
      {showArrow && arrowDirection === "right" && <Icon className="shrink-0" size={16} />}
    </button>
  );
}