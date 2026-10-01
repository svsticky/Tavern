import { useTranslation } from "react-i18next";
import { NoContentTile } from "../Tiles/NoContentTile";

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
    return <NoContentTile text={emptyText} className="p-6" />;
  }

  return (
    <ul className="flex flex-col gap-3">
      {items.map((item, index) => (
        <li
          key={index}
          className="flex items-center justify-between gap-4 rounded-xl border border-gray-200 bg-gray-50 px-4 py-3 transition-colors hover:border-(--board-primary-light) hover:bg-orange-50"
        >
          <span className="truncate font-medium text-slate-700">
            {item.name}
          </span>
          <span className="shrink-0 font-semibold text-(--board-primary-dark)">
            {item.price > 0 ? `€${item.price.toFixed(2)}` : t("free")}
          </span>
        </li>
      ))}
    </ul>
  );
}
