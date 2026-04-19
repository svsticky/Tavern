import { cn } from "~/util/tailwind.util";

export default function Input({ label = null, className, ...props }: {label?: string | null} & React.InputHTMLAttributes<HTMLInputElement>) {

  if(props.type === "checkbox") {
    return (
      <div className={cn("flex items-center justify-start", className)}>
        <input type="checkbox" {...props} />
        {label && <span className="ml-2 text-sm">{label}</span>}
      </div>
    );
  }

  return (
    <label className="flex flex-col gap-1 w-full">
      {label && <span className="text-sm font-medium text-gray-700">{label}</span>}
      <input 
        {...props} 
        className={cn(
          "transition-all outline-none w-full border p-2 rounded-lg focus:ring-2",
          props.disabled && "bg-gray-100 cursor-not-allowed border-gray-300 text-gray-500",
          className
        )} 
      />
    </label>
  );
}