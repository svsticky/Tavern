import { t } from "i18next";
import { useState } from "react";
import type { Mailinglist } from "~/api";
import Button from "~/components/UI/Button";
import Form from "~/components/UI/Form/Form";
import Input from "~/components/UI/Input";
import {
  handleMailingListDelete,
  handleMailingListSubmit,
} from "./EditMailinglistOverlay.handlers";

interface EditMailingListOverlayProps {
  onMailingListEdited: (list?: Mailinglist) => void;
  mailingList?: Mailinglist;
}

export default function EditMailingListOverlay({
  onMailingListEdited: onComplete,
  mailingList = undefined,
}: EditMailingListOverlayProps) {
  const [formData, setFormData] = useState({
    name: mailingList?.name ?? "",
    serviceId: mailingList?.serviceId ?? "",
  });
  const [loading, setLoading] = useState(false);

  const isFormValid = formData.name.trim() !== "";

  return (
    <Form>
      <div className="space-y-4">
        <Input
          label={t("name")}
          value={formData.name}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
            setFormData({ ...formData, name: e.target.value })
          }
          required
        />

        <Input
          label={t("service_id")}
          placeholder="e.g. newsletter_general"
          value={formData.serviceId}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
            setFormData({ ...formData, serviceId: e.target.value })
          }
        />

        <div className="flex flex-col gap-2 pt-4">
          <Button
            variant="primary"
            type="button"
            onClick={(e) =>
              handleMailingListSubmit({
                e,
                formData,
                mailingList,
                setLoading,
                onComplete,
              })
            }
            disabled={loading || !isFormValid}
          >
            {mailingList ? t("save") : t("create")}
          </Button>

          {mailingList && (
            <Button
              variant="danger"
              type="button"
              onClick={() =>
                handleMailingListDelete({ mailingList, setLoading, onComplete })
              }
              disabled={loading}
            >
              {t("delete")}
            </Button>
          )}
        </div>
      </div>
    </Form>
  );
}
