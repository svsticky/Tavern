import { GripVertical } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useTranslation } from "react-i18next";
import type { RegisterSlideResponseDto } from "~/api";
import { getRegisterslides, putRegisterslidesById } from "~/api";
import { getEnv } from "~/util/config.utils";
import { cn } from "~/util/tailwind.util";
import BorderedTile from "../../Tiles/BorderedTile";
import Button from "../../UI/Button";
import Modal from "../../UI/Modal/Modal";
import EditRegisterSlideOverlay from "../EditRegisterSlideOverlay/EditRegisterSlideOverlay";

/**
 * A management dashboard component for viewing and modifying registration slideshow slides.
 *
 * Renders an interactive drag-and-drop list of slides.
 *
 * @component
 */
export default function ManageRegisterSlidesDatatable() {
  const { t } = useTranslation();
  const [slides, setSlides] = useState<RegisterSlideResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [editedSlide, setEditedSlide] = useState<
    RegisterSlideResponseDto | undefined
  >(undefined);
  const [draggedIndex, setDraggedIndex] = useState<number | null>(null);

  const fetchSlides = useCallback(async () => {
    setLoading(true);
    try {
      const res = await getRegisterslides();
      if (res.data) {
        // Sort by sortOrder ascending
        const sorted = [...res.data].sort((a, b) => a.sortOrder - b.sortOrder);
        setSlides(sorted);
      }
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchSlides();
  }, [fetchSlides]);

  const handleSlideComplete = () => {
    setIsEditModalOpen(false);
    setEditedSlide(undefined);
    fetchSlides();
  };

  const handleDragStart = (e: React.DragEvent, index: number) => {
    setDraggedIndex(index);
    e.dataTransfer.effectAllowed = "move";
  };

  const handleDragEnter = (index: number) => {
    if (draggedIndex === null || draggedIndex === index) return;
    const updated = [...slides];
    const draggedItem = updated[draggedIndex];
    updated.splice(draggedIndex, 1);
    updated.splice(index, 0, draggedItem);
    setDraggedIndex(index);
    setSlides(updated);
  };

  const handleDragEnd = async () => {
    setDraggedIndex(null);

    // Build update requests
    const updates = slides.map((slide, index) => {
      const newSortOrder = index + 1;
      return putRegisterslidesById({
        path: { id: slide.id },
        body: {
          sortOrder: newSortOrder,
        },
      });
    });

    const updatePromise = Promise.all(updates);

    toast.promise(updatePromise, {
      loading: t("saving_order"),
      success: t("order_updated_successfully"),
      error: t("failed_to_update_order"),
    });

    try {
      await updatePromise;
      fetchSlides();
    } catch (err) {
      console.error("Failed to save reordered slides:", err);
    }
  };

  return (
    <BorderedTile noPadding>
      <div className="flex justify-between items-center p-4 border-b border-slate-100 bg-slate-50/50 rounded-t-xl">
        <span className="text-sm text-slate-500 italic">
          {t("drag_to_reorder")}
        </span>
        <Button
          type="button"
          variant="primary"
          onClick={() => setIsEditModalOpen(true)}
        >
          {t("add_slide")}
        </Button>
      </div>

      <div className="p-4 flex flex-col gap-3">
        {loading ? (
          <div className="text-center text-slate-500 py-6">{t("loading")}</div>
        ) : slides.length === 0 ? (
          <div className="text-center text-slate-500 py-6">
            {t("no_slides")}
          </div>
        ) : (
          slides.map((item, index) => (
            <div
              key={item.id}
              draggable
              onDragStart={(e) => handleDragStart(e, index)}
              onDragEnter={() => handleDragEnter(index)}
              onDragEnd={handleDragEnd}
              onDragOver={(e) => e.preventDefault()}
              className={cn(
                "flex items-center gap-4 p-3 bg-white border border-slate-200 rounded-xl transition-all duration-200 select-none",
                draggedIndex === index
                  ? "opacity-40 border-dashed border-(--board-primary) bg-[color-mix(in_srgb,var(--board-primary),white_95%)] scale-[0.98]"
                  : "hover:border-(--board-primary) hover:shadow-sm",
              )}
            >
              <div className="text-slate-400 hover:text-slate-700 cursor-grab active:cursor-grabbing p-1">
                <GripVertical className="w-5 h-5 flex-shrink-0" />
              </div>

              <img
                src={`${getEnv("ApiUrl")}/registerslides/${item.id}/image`}
                className="w-24 h-16 object-cover rounded-md border border-slate-200 flex-shrink-0"
                alt=""
                loading="lazy"
              />

              <div className="flex-1 min-w-0">
                <h4 className="font-semibold text-slate-800">
                  {t("slide")} #{index + 1}
                </h4>
              </div>

              <div className="flex-shrink-0">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => {
                    setEditedSlide(item);
                    setIsEditModalOpen(true);
                  }}
                >
                  {t("edit")}
                </Button>
              </div>
            </div>
          ))
        )}
      </div>

      <Modal
        isOpen={isEditModalOpen}
        onClose={() => {
          setIsEditModalOpen(false);
          setEditedSlide(undefined);
        }}
        title={editedSlide ? t("edit_slide") : t("add_slide")}
      >
        <EditRegisterSlideOverlay
          onComplete={handleSlideComplete}
          slide={editedSlide}
        />
      </Modal>
    </BorderedTile>
  );
}
