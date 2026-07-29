import { FileText, GripVertical } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import toast from "react-hot-toast";
import { useTranslation } from "react-i18next";
import type { RegistrationDocumentResponseDto } from "~/api";
import { getRegistrationdocuments, putRegistrationdocumentsById } from "~/api";
import { cn } from "~/util/tailwind.util";
import BorderedTile from "../../Tiles/BorderedTile";
import Button from "../../UI/Button";
import Modal from "../../UI/Modal/Modal";
import EditRegistrationDocumentOverlay from "../EditRegistrationDocumentOverlay/EditRegistrationDocumentOverlay";

/**
 * A management dashboard component for viewing and modifying registration documents/agreements.
 *
 * Renders an interactive drag-and-drop list of documents.
 *
 * @component
 */
export default function ManageRegistrationDocumentsDatatable() {
  const { t, i18n } = useTranslation();
  const isDutch = i18n.language.startsWith("nl");

  const [documents, setDocuments] = useState<RegistrationDocumentResponseDto[]>(
    [],
  );
  const [loading, setLoading] = useState(true);
  const [isEditModalOpen, setIsEditModalOpen] = useState(false);
  const [editedDocument, setEditedDocument] = useState<
    RegistrationDocumentResponseDto | undefined
  >(undefined);
  const [draggedIndex, setDraggedIndex] = useState<number | null>(null);

  const fetchDocuments = useCallback(async () => {
    setLoading(true);
    try {
      const res = await getRegistrationdocuments();
      if (res.data) {
        const sorted = [...res.data].sort((a, b) => a.sortOrder - b.sortOrder);
        setDocuments(sorted);
      }
    } catch (e) {
      console.error(e);
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    fetchDocuments();
  }, [fetchDocuments]);

  const handleDocumentComplete = () => {
    setIsEditModalOpen(false);
    setEditedDocument(undefined);
    fetchDocuments();
  };

  const handleDragStart = (e: React.DragEvent, index: number) => {
    setDraggedIndex(index);
    e.dataTransfer.effectAllowed = "move";
  };

  const handleDragEnter = (index: number) => {
    if (draggedIndex === null || draggedIndex === index) return;
    const updated = [...documents];
    const draggedItem = updated[draggedIndex];
    updated.splice(draggedIndex, 1);
    updated.splice(index, 0, draggedItem);
    setDraggedIndex(index);
    setDocuments(updated);
  };

  const handleDragEnd = async () => {
    setDraggedIndex(null);

    const updates = documents.map((doc, index) => {
      const newSortOrder = index + 1;
      return putRegistrationdocumentsById({
        path: { id: doc.id },
        body: {
          nameDutch: doc.nameDutch,
          nameEnglish: doc.nameEnglish,
          url: doc.url,
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
      fetchDocuments();
    } catch (err) {
      console.error("Failed to save reordered documents:", err);
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
          {t("add_document")}
        </Button>
      </div>

      <div className="p-4 flex flex-col gap-3">
        {loading ? (
          <div className="text-center text-slate-500 py-6">{t("loading")}</div>
        ) : documents.length === 0 ? (
          <div className="text-center text-slate-500 py-6">
            {t("no_documents")}
          </div>
        ) : (
          documents.map((item, index) => (
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

              <FileText className="w-6 h-6 text-(--board-primary) flex-shrink-0" />

              <div className="flex-1 min-w-0">
                <h4 className="font-semibold text-slate-800 truncate">
                  {isDutch ? item.nameDutch : item.nameEnglish}
                </h4>
                <a
                  href={item.url}
                  target="_blank"
                  rel="noopener noreferrer"
                  className="text-xs text-blue-600 hover:underline truncate block"
                  onClick={(e) => e.stopPropagation()}
                >
                  {item.url}
                </a>
              </div>

              <div className="flex-shrink-0">
                <Button
                  type="button"
                  variant="secondary"
                  onClick={() => {
                    setEditedDocument(item);
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
          setEditedDocument(undefined);
        }}
        title={editedDocument ? t("edit_document") : t("add_document")}
      >
        <EditRegistrationDocumentOverlay
          onComplete={handleDocumentComplete}
          document={editedDocument}
        />
      </Modal>
    </BorderedTile>
  );
}
