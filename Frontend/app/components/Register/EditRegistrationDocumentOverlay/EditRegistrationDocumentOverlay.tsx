import { useState } from "react";
import { useTranslation } from "react-i18next";
import type { RegistrationDocumentResponseDto } from "~/api";
import Button from "../../UI/Button";
import Input from "../../UI/Input";
import {
  handleDocumentDelete,
  handleDocumentSubmit,
} from "./EditRegistrationDocumentOverlay.handlers";

type Props = {
  onComplete: () => void;
  document?: RegistrationDocumentResponseDto;
};

export default function EditRegistrationDocumentOverlay({
  onComplete,
  document,
}: Props) {
  const { t } = useTranslation();
  const [loading, setLoading] = useState(false);

  const [formData, setFormData] = useState({
    nameDutch: document?.nameDutch || "",
    nameEnglish: document?.nameEnglish || "",
    url: document?.url || "",
    sortOrder: document?.sortOrder ?? 0,
  });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value, type } = e.target;
    setFormData((prev) => ({
      ...prev,
      [name]: type === "number" ? Number.parseInt(value, 10) || 0 : value,
    }));
  };

  return (
    <form
      onSubmit={(e) =>
        handleDocumentSubmit({
          e,
          formData,
          document,
          setLoading,
          onComplete,
        })
      }
      className="space-y-4 pt-2"
    >
      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <Input
          label={t("title_nl")}
          name="nameDutch"
          value={formData.nameDutch}
          onChange={handleChange}
          required
        />
        <Input
          label={t("title_en")}
          name="nameEnglish"
          value={formData.nameEnglish}
          onChange={handleChange}
          required
        />
      </div>

      <Input
        label={t("url")}
        name="url"
        type="url"
        placeholder="https://example.com/document.pdf"
        value={formData.url}
        onChange={handleChange}
        required
      />

      <Input
        label={t("sort_order")}
        name="sortOrder"
        type="number"
        value={formData.sortOrder.toString()}
        onChange={handleChange}
        required
      />

      <div className="flex items-center justify-between gap-3 pt-4 border-t border-slate-100">
        {document ? (
          <Button
            type="button"
            variant="danger"
            onClick={() =>
              handleDocumentDelete({
                document,
                setLoading,
                onComplete,
              })
            }
            disabled={loading}
          >
            {t("delete")}
          </Button>
        ) : (
          <div />
        )}

        <div className="flex gap-2">
          <Button
            type="button"
            variant="secondary"
            onClick={onComplete}
            disabled={loading}
          >
            {t("cancel")}
          </Button>

          <Button type="submit" variant="primary" disabled={loading}>
            {loading ? t("saving") : document ? t("update") : t("create")}
          </Button>
        </div>
      </div>
    </form>
  );
}
