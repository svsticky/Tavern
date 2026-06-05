import { t } from "i18next";
import { useEffect, useState } from "react";
import type { Mailinglist } from "~/api";
import EditMailingListOverlay from "~/components/Mailinglist/EditMailinglistOverlay/EditMailinglistOverlay";
import BorderedTile from "~/components/Tiles/BorderedTile";
import DataTableTile from "~/components/Tiles/DataTableTile";
import Button from "~/components/UI/Button";
import Modal from "~/components/UI/Modal/Modal";
import {
  fetchMailingLists,
  handleMailingListEdited,
} from "./ManageMailinglistsDatatable.handlers";

export default function ManageMailingListsDatatable() {
  const [mailingLists, setMailingLists] = useState<Mailinglist[]>([]);
  const [loading, setLoading] = useState(true);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [editedList, setEditedList] = useState<Mailinglist | undefined>(
    undefined,
  );

  const columns = [
    {
      header: t("name"),
      render: (item: Mailinglist) => item.name,
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
      render: (item: Mailinglist) => (
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
    fetchMailingLists(setLoading, setMailingLists);
  }, []);

  return (
    <BorderedTile>
      <DataTableTile
        columns={columns}
        data={mailingLists}
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
          <EditMailingListOverlay
            onMailingListEdited={(list?: Mailinglist) =>
              handleMailingListEdited({
                list,
                editedList,
                setMailingLists,
                setIsEditModalOpen,
                setEditedList,
              })
            }
            mailingList={editedList}
          />
        )}
      </Modal>
    </BorderedTile>
  );
}
