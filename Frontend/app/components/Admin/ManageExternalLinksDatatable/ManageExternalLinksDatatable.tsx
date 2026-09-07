import {
  Book,
  BookOpen,
  Briefcase,
  Calculator,
  Camera,
  FileText,
  Github,
  GripVertical,
  LayoutDashboard,
  Link,
  MessageSquare,
  ShieldAlert,
  Trophy,
} from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useTranslation } from "react-i18next";
import type { ExternalLinkResponseDto } from "~/api";
import { getExternallinks, putExternallinksById } from "~/api";
import { getEnv } from "~/util/config.utils";
import { cn } from "~/util/tailwind.util";
import BorderedTile from "../../Tiles/BorderedTile";
import Button from "../../UI/Button";
import Modal from "../../UI/Modal/Modal";
import EditExternalLinkOverlay from "../EditExternalLinkOverlay/EditExternalLinkOverlay";

/**
 * A management dashboard component for viewing and modifying external links.
 *
 * Renders an interactive drag-and-drop list of external links.
 *
 * @component
 */
export default function ManageExternalLinksDatatable() {
  const { t, i18n } = useTranslation();
  const isDutch = i18n.language.startsWith("nl");

  const defaultIcons = [
    LayoutDashboard,
    Camera,
    FileText,
    Calculator,
    BookOpen,
    Briefcase,
    Github,
    MessageSquare,
    Book,
    ShieldAlert,
    Trophy,
  ];

  const [links, setLinks] = useState<ExternalLinkResponseDto[]>([]);
  const [loading, setLoading] = useState(true);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [editedLink, setEditedLink] = useState<
    ExternalLinkResponseDto | undefined
  >(undefined);
  const [draggedIndex, setDraggedIndex] = useState<number | null>(null);

  const fetchLinks = useCallback(async () => {
    setLoading(true);
    try {
      const res = await getExternallinks();
      if (res.data) {
        // Sort by sortOrder ascending
        const sorted = [...res.data].sort((a, b) => a.sortOrder - b.sortOrder);
        setLinks(sorted);
      }
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchLinks();
  }, [fetchLinks]);

  const handleLinkComplete = () => {
    setIsEditModalOpen(false);
    setEditedLink(undefined);
    fetchLinks();
  };

  const handleDragStart = (e: React.DragEvent, index: number) => {
    setDraggedIndex(index);
    e.dataTransfer.effectAllowed = "move";
  };

  const handleDragEnter = (index: number) => {
    if (draggedIndex === null || draggedIndex === index) return;
    const updated = [...links];
    const draggedItem = updated[draggedIndex];
    updated.splice(draggedIndex, 1);
    updated.splice(index, 0, draggedItem);
    setDraggedIndex(index);
    setLinks(updated);
  };

  const handleDragEnd = async () => {
    setDraggedIndex(null);

    // Build update requests
    const updates = links.map((link, index) => {
      const newSortOrder = index + 1;
      return putExternallinksById({
        path: { id: link.id },
        body: {
          titleDutch: link.titleDutch,
          titleEnglish: link.titleEnglish,
          descriptionDutch: link.descriptionDutch,
          descriptionEnglish: link.descriptionEnglish,
          url: link.url,
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
      fetchLinks();
    } catch (err) {
      console.error("Failed to save reordered external links:", err);
    }
  };

  return (
    <BorderedTile noPadding>
      <div className="flex justify-between items-center p-4 border-b border-gray-100 bg-gray-50/50 rounded-t-xl">
        <span className="text-sm text-gray-500 italic">
          {t("drag_to_reorder")}
        </span>
        <Button
          type="button"
          variant="primary"
          onClick={() => setIsEditModalOpen(true)}
        >
          {t("add_external_link")}
        </Button>
      </div>

      <div className="p-4 flex flex-col gap-3">
        {loading ? (
          <div className="text-center text-gray-500 py-6">{t("loading")}</div>
        ) : links.length === 0 ? (
          <div className="text-center text-gray-500 py-6">
            {t("no_external_links")}
          </div>
        ) : (
          links.map((item, index) => (
            <div
              key={item.id}
              draggable
              onDragStart={(e) => handleDragStart(e, index)}
              onDragEnter={() => handleDragEnter(index)}
              onDragEnd={handleDragEnd}
              onDragOver={(e) => e.preventDefault()}
              className={cn(
                "flex items-center gap-4 p-3 bg-white border border-gray-200 rounded-xl transition-all duration-200 select-none",
                draggedIndex === index
                  ? "opacity-40 border-dashed border-(--board-primary) bg-[color-mix(in_srgb,var(--board-primary),white_95%)] scale-[0.98]"
                  : "hover:border-(--board-primary) hover:shadow-xs",
              )}
            >
              <div className="text-gray-400 hover:text-gray-700 cursor-grab active:cursor-grabbing p-1">
                <GripVertical className="w-5 h-5 flex-shrink-0" />
              </div>

              {item.iconPath ? (
                <img
                  src={`${getEnv("ApiUrl")}/externallinks/${item.id}/icon`}
                  className="w-8 h-8 object-contain rounded-md flex-shrink-0"
                  alt=""
                  loading="lazy"
                />
              ) : (
                (() => {
                  const DefaultIcon = defaultIcons[index] || Link;
                  return (
                    <DefaultIcon className="w-8 h-8 text-(--board-primary) flex-shrink-0" />
                  );
                })()
              )}

              <div className="flex-1 min-w-0">
                <h4 className="font-semibold text-gray-800 truncate">
                  {isDutch ? item.titleDutch : item.titleEnglish}
                </h4>
                <p className="text-xs text-gray-500 truncate">{item.url}</p>
                <p className="text-xs text-gray-400 truncate">
                  {isDutch ? item.descriptionDutch : item.descriptionEnglish}
                </p>
              </div>

              <div className="flex-shrink-0">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => {
                    setEditedLink(item);
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
          setEditedLink(undefined);
        }}
        title={editedLink ? t("edit_external_link") : t("add_external_link")}
      >
        <EditExternalLinkOverlay
          onComplete={handleLinkComplete}
          link={editedLink}
        />
      </Modal>
    </BorderedTile>
  );
}
