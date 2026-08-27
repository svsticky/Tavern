import { t } from "i18next";
import { useEffect, useState } from "react";
import type {
  CuratedMailinglistDto,
  MailinglistDto,
  MailinglistVisibility,
} from "~/api";
import Button from "~/components/UI/Button";
import { useConfirm } from "~/components/UI/ConfirmModal/useConfirm";
import Form from "~/components/UI/Form/Form";
import Select from "~/components/UI/Select";
import {
  fetchAddableMailinglists,
  handleMailinglistDelete,
  handleMailinglistSubmit,
} from "./EditMailinglistOverlay.handlers";

/**
 * An overlay for curating a new mailing list, or changing an existing curated mailing list's
 * visibility.
 *
 * In "add" mode (no `curatedList` given) it fetches the provider's not-yet-curated lists and
 * lets the admin pick one plus a visibility. In "edit" mode, the underlying provider list can't
 * be repointed - only its visibility can be changed, or the curation entry deleted entirely.
 *
 * @component
 * @param {Object} props - Component properties.
 * @param {function} props.onMailinglistEdited - Callback triggered when a mailing list is added, updated, or deleted.
 * @param {CuratedMailinglistDto} [props.curatedList] - Optional existing curated list; if present, the component switches to edit/delete mode.
 */
export default function EditMailinglistOverlay({
  onMailinglistEdited: onComplete,
  curatedList = undefined,
}: {
  onMailinglistEdited: (list?: CuratedMailinglistDto) => void;
  curatedList?: CuratedMailinglistDto;
}) {
  const [addableLists, setAddableLists] = useState<MailinglistDto[]>([]);
  const [loadingAddableLists, setLoadingAddableLists] = useState(false);
  const [providerListId, setProviderListId] = useState("");
  const [visibility, setVisibility] = useState<MailinglistVisibility>(
    curatedList?.visibility ?? "General",
  );
  const [loading, setLoading] = useState(false);
  const [confirmModal, confirm] = useConfirm();

  useEffect(() => {
    if (!curatedList) {
      fetchAddableMailinglists(setLoadingAddableLists, setAddableLists);
    }
  }, [curatedList]);

  const isFormValid = curatedList ? true : !!providerListId;

  return (
    <Form
      onSubmit={(e) =>
        handleMailinglistSubmit({
          e,
          curatedList,
          providerListId,
          visibility,
          setLoading,
          onComplete,
        })
      }
    >
      {curatedList ? (
        <p className="text-sm text-gray-700">
          {curatedList.name ?? curatedList.providerListId}
        </p>
      ) : loadingAddableLists ? (
        <p className="text-gray-500 italic">{t("loading")}</p>
      ) : addableLists.length === 0 ? (
        <p className="text-gray-500 italic">{t("no_addable_mailing_lists")}</p>
      ) : (
        <Select
          label={t("name")}
          value={providerListId}
          onChange={(e) => setProviderListId(e.target.value)}
          options={[
            { value: "", label: t("select_mailing_list") },
            ...addableLists.map((list) => ({
              value: list.id ?? "",
              label: list.name ?? list.id ?? "",
            })),
          ]}
          required
        />
      )}

      <Select
        label={t("visibility")}
        value={visibility}
        onChange={(e) => setVisibility(e.target.value as MailinglistVisibility)}
        options={[
          { value: "General", label: t("general") },
          { value: "YearlyRenewalOnly", label: t("yearly_renewal_only") },
        ]}
        required
      />

      <Button
        variant="primary"
        className="flex-1"
        disabled={loading || !isFormValid}
        type="submit"
      >
        {curatedList ? t("save") : t("add_mailing_list")}
      </Button>

      {curatedList && (
        <Button
          variant="danger"
          className="flex-1"
          type="button"
          onClick={() =>
            handleMailinglistDelete({
              curatedList,
              setLoading,
              onComplete,
              confirm,
            })
          }
        >
          {t("delete")}
        </Button>
      )}
      {confirmModal}
    </Form>
  );
}
