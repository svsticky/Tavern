/**
 * A presentational component used to display a labeled piece of information with an accompanying icon.
 * Commonly used in grids to show activity metadata like dates, locations, or participant counts.
 *
 * @component
 * @param {Object} props - The component props.
 * @param {React.ReactNode} props.icon - A Lucide icon or any React element to be displayed in the left-hand slot.
 * @param {string} props.label - The descriptive title of the information (rendered in small uppercase text).
 * @param {string} props.value - The actual data or content to be displayed.
 *
 * @example
 * ```tsx
 * <InfoItem
 *   icon={<MapPin size={18} />}
 *   label="Location"
 *   value="Utrecht, NL"
 * />
 * ```
 */
export default function InfoItem({
  icon,
  label,
  value,
}: {
  icon: React.ReactNode;
  label: string;
  value: string;
}) {
  return (
    <div className="flex items-start gap-3">
      <div className="mt-1 w-9 h-9 shrink-0 flex items-center justify-center bg-gray-50 rounded-lg text-gray-400 font-bold">
        {icon}
      </div>
      <div>
        <p className="text-[10px] uppercase font-bold text-gray-400 tracking-wider leading-none mb-1">
          {label}
        </p>
        <p className="text-gray-700 font-semibold leading-tight">{value}</p>
      </div>
    </div>
  );
}
