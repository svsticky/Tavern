import { useState } from "react";
import { useTranslation } from "react-i18next";
import type { RegisterSlideResponseDto } from "~/api";
import Button from "../../UI/Button";
import Form from "../../UI/Form/Form";
import {
  handleSlideDelete,
  handleSlideSubmit,
} from "./EditRegisterSlideOverlay.handlers";
import { cn } from "~/util/tailwind.util";

/**
 * An overlay component used for creating or editing registration page slideshow slides.
 *
 * @component
 */
export default function EditRegisterSlideOverlay({
  onComplete,
  slide = undefined,
}: {
  onComplete: () => void;
  slide?: RegisterSlideResponseDto;
}) {
  const { t } = useTranslation();
  const [slideFile, setSlideFile] = useState<File | null>(null);
  const [loading, setLoading] = useState(false);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    handleSlideSubmit({
      e,
      slideFile,
      slide,
      setLoading,
      onComplete,
    });
  };

  return (
    <Form onSubmit={handleSubmit}>
      <div className="w-full flex flex-col gap-1">
        <label className="text-sm font-semibold text-slate-800">
          {t("slide_image")}
        </label>
        <input
          type="file"
          accept="image/*"
          className={cn("w-full p-2 py-auto border border-dashed border-gray-300 rounded-md mt-1")}
          onChange={(e) => setSlideFile(e.target.files?.[0] || null)}
          required={!slide}
        />
        {slide && (
          <p className="text-xs text-gray-400 italic">
            {t("leave_empty_to_keep_current")}
          </p>
        )}
      </div>

      <div className="flex gap-4 w-full pt-4 border-t border-slate-100">
        <Button
          variant="primary"
          className="flex-1"
          disabled={loading || (!slide && !slideFile)}
          type="submit"
        >
          {slide ? t("save") : t("create")}
        </Button>

        {slide && (
          <Button
            variant="danger"
            onClick={() => handleSlideDelete({ slide, setLoading, onComplete })}
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
