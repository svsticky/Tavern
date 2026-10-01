import { useTranslation } from "react-i18next";

/**
 * Props for the ActivityPriceList component.
 * @interface ActivityPriceListProps
 * @property {{ name: string; price: number }[]} items - The activities to list with their price.
 * @property {string} emptyText - The text shown when there are no items.
 */
type ActivityPriceListProps = {
  items: { name: string; price: number }[];
  emptyText: string;
};

/**
 * A simple list of activity names with their price.
 *
 * @component
 * @param {ActivityPriceListProps} props - The component properties.
 */
export default function ActivityPriceList({
  items,
  emptyText,
}: ActivityPriceListProps) {
  const { t } = useTranslation();

  if (items.length === 0) {
    return <p className="text-gray-500">{emptyText}</p>;
  }

  return (
    <ul className="flex flex-col divide-y divide-gray-200">
      {items.map((item, index) => (
        <li key={index} className="flex justify-between gap-4 py-2">
          <span className="truncate">{item.name}</span>
          <span className="shrink-0">
            {item.price > 0 ? `€${item.price.toFixed(2)}` : t("free")}
          </span>
        </li>
      ))}
    </ul>
  );
}
