import type { ComponentProps, PropsWithChildren, KeyboardEvent } from "react";
import { cn } from "~/util/tailwind.util";

/**
 * Props for the Form component.
 * Extends standard HTML form attributes and includes React children.
 * @typedef {PropsWithChildren<ComponentProps<"form">>} FormProps
 */
type FormProps = PropsWithChildren<ComponentProps<"form">>;

/**
 * A standardized wrapper for HTML forms that provides consistent vertical spacing and layout.
 *
 * This component acts as a thin abstraction over the native `<form>` element,
 * automatically applying a flex-column layout with a standard gap between fields.
 * It passes through all native HTML form attributes like `onSubmit`, `action`, and `method`.
 *
 * @component
 * @param {FormProps} props - The component properties and native HTML form attributes.
 */
export default function Form({ children, className, onKeyDown, ...props }: FormProps) {
  const handleKeyDown = (event: KeyboardEvent<HTMLFormElement>) => {
    if (event.key === "Enter" && (event.ctrlKey || event.metaKey)) {
      event.preventDefault();
      
      event.currentTarget.requestSubmit();
    }

    onKeyDown?.(event);
  };

  return (
    <form {...props} className={cn("flex flex-col gap-4", className)} onKeyDown={handleKeyDown}>
      {children}
    </form>
  );
}
