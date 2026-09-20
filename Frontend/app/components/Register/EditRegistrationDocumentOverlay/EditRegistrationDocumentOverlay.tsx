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
    urlDutch: document?.urlDutch || "",
    urlEnglish: document?.urlEnglish || "",
    sortOrder: document?.sortOrder ?? 0,
  });

  const handleChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData((prev) => ({ ...prev, [name]: value }));
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

      <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
        <Input
          label={t("url_nl")}
          name="urlDutch"
          type="url"
          placeholder="https://example.com/document.pdf"
          value={formData.urlDutch}
          onChange={handleChange}
          required
        />
        <Input
          label={t("url_en")}
          name="urlEnglish"
          type="url"
          placeholder="https://example.com/document.pdf"
          value={formData.urlEnglish}
          onChange={handleChange}
          required
        />
      </div>

      <div className="flex flex-col-reverse sm:flex-row sm:items-center sm:justify-between gap-3 pt-4 border-t border-slate-100">
        {document ? (
          <Button
            type="button"
            variant="danger"
            className="w-full sm:w-auto"
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
          <div className="hidden sm:block" />
        )}

        <div className="flex flex-col-reverse sm:flex-row gap-2">
          <Button
            type="button"
            variant="secondary"
            className="w-full sm:w-auto"
            onClick={onComplete}
            disabled={loading}
          >
            {t("cancel")}
          </Button>

          <Button
            type="submit"
            variant="primary"
            className="w-full sm:w-auto"
            disabled={loading}
          >
            {loading ? t("saving") : document ? t("update") : t("create")}
          </Button>
        </div>
      </div>
    </form>
  );
}
