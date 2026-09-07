import { useState } from "react";
import { useTranslation } from "react-i18next";
import type { ExternalLinkResponseDto } from "~/api";
import { cn } from "~/util/tailwind.util";
import Button from "../../UI/Button";
import Form from "../../UI/Form/Form";
import Input from "../../UI/Input";
import {
  handleLinkDelete,
  handleLinkSubmit,
} from "./EditExternalLinkOverlay.handlers";

/**
 * An overlay component used for creating or editing external link information.
 *
 * @component
 */
export default function EditExternalLinkOverlay({
  onComplete,
  link = undefined,
}: {
  onComplete: () => void;
  link?: ExternalLinkResponseDto;
}) {
  const { t } = useTranslation();
  const [formData, setFormData] = useState({
    titleDutch: link ? link.titleDutch : "",
    titleEnglish: link ? link.titleEnglish : "",
    descriptionDutch: link ? link.descriptionDutch : "",
    descriptionEnglish: link ? link.descriptionEnglish : "",
    url: link ? link.url : "",
    sortOrder: link ? link.sortOrder : 0,
  });
  const [iconFile, setIconFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    handleLinkSubmit({
      e,
      formData,
      iconFile,
      link,
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

      <Input
        label={t("url")}
        value={formData.url}
        onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
          setFormData({ ...formData, url: e.target.value })
        }
        required
        type="url"
        placeholder="https://example.com"
      />

      <div className="w-full flex flex-col gap-1">
        <label className="text-sm font-semibold text-gray-800">
          {t("icon")}
        </label>
        <input
          type="file"
          accept="image/*"
          className={cn(
            "w-full p-2 py-auto bg-gray-100 border border-dashed rounded-md mt-1",
          )}
          onChange={(e) => setIconFile(e.target.files?.[0] || null)}
        />
        {link && (
          <p className="text-xs text-gray-400 italic">
            {t("leave_empty_to_keep_current")}
          </p>
        )}
      </div>

      <div className="flex gap-4 w-full pt-4 border-t border-gray-200">
        <Button
          variant="primary"
          className="flex-1"
          disabled={
            loading ||
            !formData.titleDutch ||
            !formData.titleEnglish ||
            !formData.descriptionDutch ||
            !formData.descriptionEnglish ||
            !formData.url
          }
          type="submit"
        >
          {link ? t("save") : t("create")}
        </Button>

        {link && (
          <Button
            variant="danger"
            onClick={() => handleLinkDelete({ link, setLoading, onComplete })}
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
