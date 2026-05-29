import { useTranslation } from "react-i18next";
import { useEffect, useState } from "react";
import toast from "react-hot-toast";
import { GripVertical, UsersRound, Book, PartyPopper, HeartHandshake, BriefcaseBusiness } from "lucide-react";
import type { RegisterReasonResponseDto } from "~/api";
import { getRegisterreasons, putRegisterreasonsById } from "~/api";
import { cn } from "~/util/tailwind.util";
import { getEnv } from "~/util/config.utils";
import BorderedTile from "../../Tiles/BorderedTile";
import Button from "../../UI/Button";
import Modal from "../../UI/Modal/Modal";
import EditRegisterReasonOverlay from "../EditRegisterReasonOverlay/EditRegisterReasonOverlay";

/**
 * A management dashboard component for viewing and modifying registration reasons.
 *
 * Renders an interactive drag-and-drop list of reasons.
 *
 * @component
 */
export default function ManageRegisterReasonsDatatable() {
  const { t, i18n } = useTranslation();
  const isDutch = i18n.language.startsWith("nl");

  const defaultIcons = [
    Book,
    PartyPopper,
    HeartHandshake,
    HeartHandshake,
    BriefcaseBusiness,
    UsersRound,
  ];
  
  const [reasons, setReasons] = useState<RegisterReasonResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [editedReason, setEditedReason] = useState<RegisterReasonResponseDto | undefined>(undefined);
  const [draggedIndex, setDraggedIndex] = useState<number | null>(null);

  const fetchReasons = async () => {
    setLoading(true);
    try {
      const res = await getRegisterreasons();
      if (res.data) {
        // Sort by sortOrder ascending
        const sorted = [...res.data].sort((a, b) => a.sortOrder - b.sortOrder);
        setReasons(sorted);
      }
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  };

  useEffect(() => {
    fetchReasons();
  }, []);

  const handleReasonComplete = () => {
    setIsEditModalOpen(false);
    setEditedReason(undefined);
    fetchReasons();
  };

  const handleDragStart = (e: React.DragEvent, index: number) => {
    setDraggedIndex(index);
    e.dataTransfer.effectAllowed = "move";
  };

  const handleDragEnter = (index: number) => {
    if (draggedIndex === null || draggedIndex === index) return;
    const updated = [...reasons];
    const draggedItem = updated[draggedIndex];
    updated.splice(draggedIndex, 1);
    updated.splice(index, 0, draggedItem);
    setDraggedIndex(index);
    setReasons(updated);
  };

  const handleDragEnd = async () => {
    setDraggedIndex(null);
    
    // Build update requests
    const updates = reasons.map((reason, index) => {
      const newSortOrder = index + 1;
      return putRegisterreasonsById({
        path: { id: reason.id },
        body: {
          titleDutch: reason.titleDutch,
          titleEnglish: reason.titleEnglish,
          descriptionDutch: reason.descriptionDutch,
          descriptionEnglish: reason.descriptionEnglish,
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
      fetchReasons();
    } catch (err) {
      console.error("Failed to save reordered reasons:", err);
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
          {t("add_reason")}
        </Button>
      </div>

      <div className="p-4 flex flex-col gap-3">
        {loading ? (
          <div className="text-center text-slate-500 py-6">{t("loading")}</div>
        ) : reasons.length === 0 ? (
          <div className="text-center text-slate-500 py-6">{t("no_reasons")}</div>
        ) : (
          reasons.map((item, index) => (
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
                  : "hover:border-(--board-primary) hover:shadow-sm"
              )}
            >
              <div className="text-slate-400 hover:text-slate-700 cursor-grab active:cursor-grabbing p-1">
                <GripVertical className="w-5 h-5 flex-shrink-0" />
              </div>

              {item.iconPath ? (
                <img
                  src={`${getEnv("ApiUrl")}/registerreasons/${item.id}/icon`}
                  className="w-8 h-8 object-contain rounded-md flex-shrink-0"
                  alt=""
                  loading="lazy"
                />
              ) : (() => {
                const DefaultIcon = defaultIcons[index] || UsersRound;
                return <DefaultIcon className="w-8 h-8 text-(--board-primary) flex-shrink-0" />;
              })()}

              <div className="flex-1 min-w-0">
                <h4 className="font-semibold text-slate-800 truncate">
                  {isDutch ? item.titleDutch : item.titleEnglish}
                </h4>
                <p className="text-xs text-slate-500 truncate">
                  {isDutch ? item.descriptionDutch : item.descriptionEnglish}
                </p>
              </div>

              <div className="flex-shrink-0">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => {
                    setEditedReason(item);
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
          setEditedReason(undefined);
        }}
        title={editedReason ? t("edit_reason") : t("add_reason")}
      >
        <EditRegisterReasonOverlay
          onComplete={handleReasonComplete}
          reason={editedReason}
        />
      </Modal>
    </BorderedTile>
  );
}
