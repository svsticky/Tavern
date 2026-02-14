import { ArrowRight } from "lucide-react";
import { cn } from "~/util/tailwind.util";

type ButtonProps = {
  className?: string;
  showArrow?: boolean;
  children: React.ReactNode;
};

export default function Button({
  className,
  showArrow = false,
  children,
}: ButtonProps) {
  return (
    <button
      type="button"
      className={cn(
        "bg-white text-(--board-primary) font-semibold px-6 py-2 rounded-lg hover:bg-gray-100 transition cursor-pointer",
        className,
      )}
    >
      {children}

      {/* Optional Arrow Icon */}
      {showArrow && <ArrowRight className="inline-block ml-2" size={16} />}
    </button>
  );
}
