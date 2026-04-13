import type { ComponentProps, PropsWithChildren } from "react";
import { cn } from "~/util/tailwind.util";

type FormProps = PropsWithChildren<ComponentProps<"form">>;

export default function Form({ children, className, ...props }: FormProps) {
  return (
    <form 
      {...props}
      className={cn("flex flex-col gap-4", className)}
    >
      {children}
    </form>
  );
}