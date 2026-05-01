import { ArrowLeft, ArrowRight } from "lucide-react";
import { cn } from "~/util/tailwind.util";

/**
 * Props for the Button component.
 * Extends standard HTML button attributes to support all native behaviors.
 *
 * @typedef {Object} ButtonProps
 * @property {string} [className] - Additional CSS classes to customize the button's appearance.
 * @property {boolean} [showArrow=false] - Whether to display a Lucide arrow icon.
 * @property {"left" | "right"} [arrowDirection="right"] - The direction and placement of the arrow icon.
 * @property {"primary" | "secondary" | "danger"} [variant="primary"] - The visual style preset of the button.
 * @property {React.ReactNode} children - The text or elements to be rendered inside the button.
 */
type ButtonProps = React.ButtonHTMLAttributes<HTMLButtonElement> & {
  className?: string;
  showArrow?: boolean;
  arrowDirection?: "left" | "right";
  variant?: "primary" | "secondary" | "danger";
  children: React.ReactNode;
};

/**
 * A highly customizable button component with built-in support for icons and visual variants.
 *
 * This component wraps the native HTML `<button>` and applies standardized styling
 * including transitions, rounded corners, and flex alignment. It supports three
 * main semantic variants (primary, secondary, and danger) and can conditionally
 * render arrows on either side of the content.
 *
 * @component
 * @param {ButtonProps} props - The component properties and native button attributes.
 */
export default function Button({
  className,
  showArrow = false,
  arrowDirection = "right",
  variant = "primary",
  children,
  ...props
}: ButtonProps) {
  const variants = {
    primary:
      "bg-(--board-primary) text-white hover:bg-(--board-primary-dark) shadow-sm hover:bg-(--board-primary-light)",
    secondary:
      "bg-white text-(--board-primary) border border-gray-200 hover:bg-gray-50",
    danger: "bg-red-600 text-white hover:bg-red-700 shadow-sm",
  };

  const Icon = arrowDirection === "left" ? ArrowLeft : ArrowRight;

  return (
    <button
      {...props}
      className={cn(
        "inline-flex items-center justify-center gap-2 font-semibold px-6 py-2 rounded-lg transition-all duration-200 cursor-pointer whitespace-nowrap",
        variants[variant],
        "disabled:opacity-50 disabled:cursor-not-allowed",
        className,
      )}
    >
      {showArrow && arrowDirection === "left" && (
        <Icon className="shrink-0" size={16} />
      )}

      {children}

      {showArrow && arrowDirection === "right" && (
        <Icon className="shrink-0" size={16} />
      )}
    </button>
  );
}
