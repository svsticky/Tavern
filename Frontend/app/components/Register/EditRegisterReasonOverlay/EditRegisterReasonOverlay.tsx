import { useState } from "react";
import { useTranslation } from "react-i18next";
import type { RegisterReasonResponseDto } from "~/api";
import Button from "../../UI/Button";
import Form from "../../UI/Form/Form";
import Input from "../../UI/Input";
import {
  handleReasonDelete,
  handleReasonSubmit,
} from "./EditRegisterReasonOverlay.handlers";

/**
 * An overlay component used for creating or editing registration reason information.
 *
 * It manages a local form state for title, description, ordering, and icon file.
 *
 * @component
 */
export default function EditRegisterReasonOverlay({
  onComplete,
  reason = undefined,
}: {
  onComplete: () => void;
  reason?: RegisterReasonResponseDto;
}) {
  const { t } = useTranslation();
  const [formData, setFormData] = useState({
    titleDutch: reason ? reason.titleDutch : "",
    titleEnglish: reason ? reason.titleEnglish : "",
    descriptionDutch: reason ? reason.descriptionDutch : "",
    descriptionEnglish: reason ? reason.descriptionEnglish : "",
    sortOrder: reason ? reason.sortOrder : 0,
  });
  const [iconFile, setIconFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    handleReasonSubmit({
      e,
      formData,
      iconFile,
      reason,
      setLoading,
      onComplete,
    });
  };

  return (
    <Form onSubmit={handleSubmit}>
      <Input
        label={t("title_nl")}
        value={formData.titleDutch}
        onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
          setFormData({ ...formData, titleDutch: e.target.value })
        }
        required
      />

      <Input
        label={t("title_en")}
        value={formData.titleEnglish}
        onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
          setFormData({ ...formData, titleEnglish: e.target.value })
        }
        required
      />

      <Input
        label={t("description_nl")}
        value={formData.descriptionDutch}
        onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
          setFormData({ ...formData, descriptionDutch: e.target.value })
        }
        required
      />

      <Input
        label={t("description_en")}
        value={formData.descriptionEnglish}
        onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
          setFormData({ ...formData, descriptionEnglish: e.target.value })
        }
        required
      />

      <div className="w-full flex flex-col gap-1">
        <label className="text-sm font-semibold text-slate-800">
          {t("icon")}
        </label>
        <input
          type="file"
          accept="image/*"
          className="w-full p-2 border border-dashed rounded-lg mt-1"
          onChange={(e) => setIconFile(e.target.files?.[0] || null)}
        />
        {reason && (
          <p className="text-xs text-gray-400 italic">
            {t("leave_empty_to_keep_current")}
          </p>
        )}
      </div>

      <div className="flex gap-4 w-full pt-4 border-t border-slate-100">
        <Button
          variant="primary"
          className="flex-1"
          disabled={
            loading ||
            !formData.titleDutch ||
            !formData.titleEnglish ||
            !formData.descriptionDutch ||
            !formData.descriptionEnglish
          }
          type="submit"
        >
          {reason ? t("save") : t("create")}
        </Button>

        {reason && (
          <Button
            variant="danger"
            onClick={() =>
              handleReasonDelete({ reason, setLoading, onComplete })
            }
            type="button"
            disabled={loading}
          >
            {t("delete")}
          </Button>
        )}
      </div>
    </Form>
  );
}
