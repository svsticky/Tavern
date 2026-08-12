import { ArrowLeft, ArrowRight } from "lucide-react";
import { NavLink } from "react-router";
import { cn } from "~/util/tailwind.util";

/**
 * Common properties shared across all Button variants and underlying HTML elements.
 *
 * @typedef {Object} CommonProps
 * @property {string} [className] - Additional CSS classes to customize the button's appearance.
 * @property {boolean} [showArrow=false] - Whether to display a Lucide arrow icon.
 * @property {"left" | "right"} [arrowDirection="right"] - The direction and placement of the arrow icon.
 * @property {"primary" | "secondary" | "danger"} [variant="primary"] - The visual style preset of the button.
 * @property {React.ReactNode} children - The text or elements to be rendered inside the button.
 */
type CommonProps = {
  className?: string;
  showArrow?: boolean;
  arrowDirection?: "left" | "right";
  variant?: "primary" | "secondary" | "danger";
  children: React.ReactNode;
};

/**
 * Props for the Button component.
 * Dynamically extends native HTML anchor attributes when `href` is supplied, or native button attributes otherwise.
 *
 * @typedef {CommonProps & (AnchorProps | NativeButtonProps)} ButtonProps
 * @property {string} [href] - Optional URL. When supplied, renders an `<a>` tag to support navigation and middle-click.
 */
type ButtonProps = CommonProps &
  (
    | ({ href: string } & React.AnchorHTMLAttributes<HTMLAnchorElement>)
    | ({ href?: never } & React.ButtonHTMLAttributes<HTMLButtonElement>)
  );

/**
 * A versatile button component that dynamically renders either a native `<button>`
 * or an `<a>` tag when an `href` prop is supplied (enabling middle-click and open in new tab).
 * Supports variants, icons, and custom styling overrides.
 *
 * @component
 * @param {ButtonProps} props - The component properties extending button or anchor HTML attributes.
 */
export default function Button({
  className,
  showArrow = false,
  arrowDirection = "right",
  variant = "primary",
  children,
  href,
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

  const combinedClassName = cn(
    "inline-flex items-center justify-center gap-2 font-semibold px-6 py-2 rounded-lg transition-all duration-200 cursor-pointer whitespace-nowrap",
    variants[variant],
    "disabled:opacity-50 disabled:cursor-not-allowed",
    className,
  );

  const content = (
    <>
      {showArrow && arrowDirection === "left" && (
        <Icon className="shrink-0" size={16} />
      )}
      {children}
      {showArrow && arrowDirection === "right" && (
        <Icon className="shrink-0" size={16} />
      )}
    </>
  );

  // Render as anchor if 'href' is passed to support middle-click / new tab behavior
  if (href) {
    return (
      <NavLink
        to={href}
        className={combinedClassName}
        {...(props as React.AnchorHTMLAttributes<HTMLAnchorElement>)}
      >
        {content}
      </NavLink>
    );
  }

  return (
    <button
      {...(props as React.ButtonHTMLAttributes<HTMLButtonElement>)}
      className={combinedClassName}
    >
      {content}
    </button>
  );
}