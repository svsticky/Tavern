import { useTranslation } from "react-i18next";
import { Link } from "react-router";
import { formatDate } from "~/util/date.util";
import { cn } from "~/util/tailwind.util";
import { NoContentTile } from "../Tiles/NoContentTile";
import Tile from "../Tiles/Tile";
import Modal from "../UI/Modal/Modal";

/**
 * An activity shown in the ActivityListModal.
 * @interface ActivityListModalItem
 * @property {number} [id] - The id of the activity, used to link to its detail page.
 * @property {string} name - The name of the activity.
 * @property {string} [dateTimeStart] - The start of the activity, used for sorting and display.
 * @property {string} [dateTimeEnd] - The end of the activity; only activities that have not ended yet are clickable.
 * @property {number} [price] - When given, shown instead of the date.
 */
export type ActivityListModalItem = {
  id?: number;
  name: string;
  dateTimeStart?: string;
  dateTimeEnd?: string;
  price?: number;
};

/**
 * Props for the ActivityListModal component.
 * @interface ActivityListModalProps
 * @property {boolean} isOpen - Controls whether the modal is visible.
 * @property {() => void} onClose - Callback to close the modal.
 * @property {string} title - The heading of the modal.
 * @property {ActivityListModalItem[]} items - The activities to list.
 * @property {string} emptyText - The text shown when there are no items.
 */
type ActivityListModalProps = {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  items: ActivityListModalItem[];
  emptyText: string;
};

/**
 * A modal listing activities sorted by date, showing either their date or their price.
 * Activities that have not ended yet link to their detail page.
 *
 * @component
 * @param {ActivityListModalProps} props - The component properties.
 */
export default function ActivityListModal({
  isOpen,
  onClose,
  title,
  items,
  emptyText,
}: ActivityListModalProps) {
  const { t } = useTranslation();

  // Items without a date are placed at the end of the list.
  const sortedItems = [...items].sort(
    (a, b) =>
      (a.dateTimeStart
        ? new Date(a.dateTimeStart).getTime()
        : Number.POSITIVE_INFINITY) -
      (b.dateTimeStart
        ? new Date(b.dateTimeStart).getTime()
        : Number.POSITIVE_INFINITY),
  );
  const now = Date.now();

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={title}>
      {sortedItems.length === 0 ? (
        <NoContentTile text={emptyText} className="p-6" />
      ) : (
        <ul className="flex flex-col gap-3">
          {sortedItems.map((item, index) => {
            const isClickable =
              item.id !== undefined &&
              item.dateTimeEnd !== undefined &&
              new Date(item.dateTimeEnd).getTime() > now;

            const content = (
              <Tile
                className={cn(
                  "flex items-center justify-between gap-4 rounded-xl border border-gray-200 bg-gray-50 px-4 py-3",
                  isClickable &&
                    "transition-colors hover:border-(--board-primary-light) hover:bg-orange-50",
                )}
              >
                <span className="truncate font-medium text-slate-700">
                  {item.name}
                </span>
                <span className="shrink-0 font-semibold text-(--board-primary-dark)">
                  {item.price !== undefined
                    ? item.price > 0
                      ? `€${item.price.toFixed(2)}`
                      : t("free")
                    : item.dateTimeStart &&
                      formatDate(new Date(item.dateTimeStart), "dateOnly")}
                </span>
              </Tile>
            );

            return (
              <li key={item.id ?? index}>
                {isClickable ? (
                  <Link to={`/activities/${item.id}`} onClick={onClose}>
                    {content}
                  </Link>
                ) : (
                  content
                )}
              </li>
            );
          })}
        </ul>
      )}
    </Modal>
  );
}
