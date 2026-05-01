import { FormHeader } from "./FormHeader";

/**
 * A layout component for grouping related form fields into a logical section.
 *
 * It automatically handles the inclusion of a `FormHeader` if a title is provided
 * and organizes its children into a responsive grid. The grid defaults to a single
 * column on mobile devices and scales to a specified number of columns on medium
 * screens and larger.
 *
 * @component
 * @param {Object} props - The component properties.
 * @param {React.ReactNode} props.children - The form inputs or elements to be displayed in the grid.
 * @param {string} [props.title] - Optional title for the section, rendered via FormHeader.
 * @param {number} [props.columns=2] - The number of grid columns to display on medium screens and larger.
 * @param {string} [props.className=""] - Additional CSS classes for the section container.
 */
export const FormSection = ({
  children,
  title,
  columns = 2,
  className = "",
}: {
  children: React.ReactNode;
  title?: string;
  columns?: number;
  className?: string;
}) => (
  <section className={className}>
    {title && <FormHeader title={title} />}
    <div className={`grid grid-cols-1 md:grid-cols-${columns} gap-6`}>
      {children}
    </div>
  </section>
);
