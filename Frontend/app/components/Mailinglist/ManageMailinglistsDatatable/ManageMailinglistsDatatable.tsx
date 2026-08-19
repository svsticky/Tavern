import { t } from "i18next";
import { TriangleAlert } from "lucide-react";
import { useEffect, useState } from "react";
import type { CuratedMailinglistDto } from "~/api";
import EditMailinglistOverlay from "~/components/Mailinglist/EditMailinglistOverlay/EditMailinglistOverlay";
import BorderedTile from "~/components/Tiles/BorderedTile";
import DataTableTile from "~/components/Tiles/DataTableTile";
import Button from "~/components/UI/Button";
import Modal from "~/components/UI/Modal/Modal";
import {
  fetchCuratedMailinglists,
  handleMailinglistEdited,
} from "./ManageMailinglistsDatatable.handlers";

/**
 * Admin management table for curated mailing lists.
 *
 * Lists every mailing list Tavern currently curates from the mail subscription provider, with
 * its live-resolved name, visibility (General vs. yearly renewal only), and a warning when the
 * underlying provider list no longer exists. Supports adding a new curated list (picked from the
 * provider's not-yet-curated lists) and editing an existing one's visibility or removing it.
 *
 * @component
 */
export default function ManageMailinglistsDatatable() {
  const [curatedLists, setCuratedLists] = useState<CuratedMailinglistDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [editedList, setEditedList] = useState<
    CuratedMailinglistDto | undefined
  >(undefined);

  const columns = [
    {
      header: t("name"),
      render: (item: CuratedMailinglistDto) => (
        <div className="flex items-center gap-2">
          {item.name ?? item.providerListId}
          {item.orphaned && (
            <span title={t("orphaned_mailing_list_warning")}>
              <TriangleAlert className="w-4 h-4 text-amber-500" />
            </span>
          )}
        </div>
      ),
    },
    {
      header: t("visibility"),
      render: (item: CuratedMailinglistDto) =>
        item.visibility === "YearlyRenewalOnly"
          ? t("yearly_renewal_only")
          : t("general"),
    },
    {
      header: (
        <div className="flex justify-end">
          <Button
            type="button"
            variant="primary"
            onClick={() => setIsEditModalOpen(true)}
          >
            {t("add_mailing_list")}
          </Button>
        </div>
      ),
      render: (item: CuratedMailinglistDto) => (
        <div className="flex justify-end">
          <Button
            type="button"
            variant="secondary"
            onClick={() => {
              setEditedList(item);
              setIsEditModalOpen(true);
            }}
          >
            {t("edit")}
          </Button>
        </div>
      ),
      className: "w-px whitespace-nowrap text-right",
    },
  ];

  useEffect(() => {
    fetchCuratedMailinglists(setLoading, setCuratedLists);
  }, []);

  return (
    <BorderedTile>
      <DataTableTile
        columns={columns}
        data={curatedLists}
        emptyText={loading ? t("loading") : t("no_mailing_lists")}
      />
      <Modal
        isOpen={isEditModalOpen}
        onClose={() => {
          setIsEditModalOpen(false);
          setEditedList(undefined);
        }}
        title={editedList ? t("edit_mailing_list") : t("add_mailing_list")}
      >
        {isEditModalOpen && (
          <EditMailinglistOverlay
            onMailinglistEdited={(list?: CuratedMailinglistDto) =>
              handleMailinglistEdited({
                list,
                editedList,
                setCuratedLists,
                setIsEditModalOpen,
                setEditedList,
              })
            }
            curatedList={editedList}
          />
        )}
      </Modal>
    </BorderedTile>
  );
}
