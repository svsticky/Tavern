import { FormHeader } from "./FormHeader";

export const FormSection = ({ children, title, columns = 2, className = "" }: { 
  children: React.ReactNode, 
  title?: string, 
  columns?: number,
  className?: string 
}) => (
  <section className={className}>
    {title && <FormHeader title={title} />}
    <div className={`grid grid-cols-1 md:grid-cols-${columns} gap-6`}>
      {children}
    </div>
  </section>
);