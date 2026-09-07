import {
  ArrowDown,
  ArrowLeft,
  ArrowRight,
  ArrowUp,
  RotateCcw,
} from "lucide-react";
import { useState } from "react";
import { useTranslation } from "react-i18next";
import Button from "~/components/UI/Button";
import Checkbox from "~/components/UI/Checkbox";
import Modal from "~/components/UI/Modal/Modal";
import {
  type DashboardColumn,
  type DashboardWidgetConfig,
  type DashboardWidgetId,
  DEFAULT_DASHBOARD_WIDGETS,
} from "~/util/dashboardWidgets";

interface PersonaliseDashboardModalProps {
  isOpen: boolean;
  onClose: () => void;
  widgets: DashboardWidgetConfig[];
  onSave: (updated: DashboardWidgetConfig[]) => void;
  onReset: () => void;
}

function PersonaliseDashboardModalContent({
  isOpen,
  onClose,
  widgets,
  onSave,
  onReset,
}: PersonaliseDashboardModalProps) {
  const { t } = useTranslation();
  const [localWidgets, setLocalWidgets] =
    useState<DashboardWidgetConfig[]>(widgets);

  const getWidgetTitle = (id: DashboardWidgetId): string => {
    switch (id) {
      case "announcements":
        return t("latest_announcements");
      case "upcoming_activities":
        return t("upcoming_activities");
      case "my_enrollments":
        return t("my_enrollments");
      case "my_groups":
        return t("my_groups");
      default:
        return id;
    }
  };

  const handleToggleVisible = (id: DashboardWidgetId) => {
    setLocalWidgets((prev) =>
      prev.map((w) => (w.id === id ? { ...w, visible: !w.visible } : w)),
    );
  };

  const handleMoveOrder = (
    column: DashboardColumn,
    fromIndex: number,
    toIndex: number,
  ) => {
    const columnWidgets = localWidgets
      .filter((w) => w.column === column)
      .sort((a, b) => a.order - b.order);

    if (
      fromIndex < 0 ||
      fromIndex >= columnWidgets.length ||
      toIndex < 0 ||
      toIndex >= columnWidgets.length
    ) {
      return;
    }

    const reordered = [...columnWidgets];
    const [moved] = reordered.splice(fromIndex, 1);
    reordered.splice(toIndex, 0, moved);

    const reindexed = reordered.map((item, index) => ({
      ...item,
      order: index,
    }));

    setLocalWidgets((prev) =>
      prev.map((w) => {
        if (w.column === column) {
          const match = reindexed.find((item) => item.id === w.id);
          return match ? match : w;
        }
        return w;
      }),
    );
  };

  const handleSwitchColumn = (
    id: DashboardWidgetId,
    targetColumn: DashboardColumn,
  ) => {
    const targetWidgets = localWidgets.filter((w) => w.column === targetColumn);
    setLocalWidgets((prev) =>
      prev.map((w) =>
        w.id === id
          ? {
              ...w,
              column: targetColumn,
              order: targetWidgets.length,
            }
          : w,
      ),
    );
  };

  const handleSave = () => {
    onSave(localWidgets);
    onClose();
  };

  const handleReset = () => {
    onReset();
    setLocalWidgets(DEFAULT_DASHBOARD_WIDGETS.map((w) => ({ ...w })));
    onClose();
  };

  const mainWidgets = localWidgets
    .filter((w) => w.column === "main")
    .sort((a, b) => a.order - b.order);

  const sidebarWidgets = localWidgets
    .filter((w) => w.column === "sidebar")
    .sort((a, b) => a.order - b.order);

  return (
    <Modal isOpen={isOpen} onClose={onClose} title={t("personalise_dashboard")}>
      <div className="flex flex-col gap-6">
        <p className="text-sm text-gray-600">
          {t("personalise_dashboard_description")}
        </p>

        {/* Main Column Section */}
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-bold text-gray-900 uppercase tracking-wider">
              {t("main_content")}
            </h3>
            <span className="text-xs text-gray-500 font-medium">
              {mainWidgets.filter((w) => w.visible).length} /{" "}
              {mainWidgets.length}
            </span>
          </div>

          <div className="flex flex-col gap-2">
            {mainWidgets.length === 0 ? (
              <p className="text-xs text-gray-400 italic py-2">
                {t("no_data")}
              </p>
            ) : (
              mainWidgets.map((widget, index) => (
                <div
                  key={widget.id}
                  className={`flex items-center justify-between p-3 rounded-xl border transition-colors ${
                    widget.visible
                      ? "bg-white border-gray-200 shadow-xs"
                      : "bg-gray-50 border-dashed border-gray-200 opacity-60"
                  }`}
                >
                  <Checkbox
                    id={`widget-toggle-${widget.id}`}
                    label={getWidgetTitle(widget.id)}
                    checked={widget.visible}
                    onChange={() => handleToggleVisible(widget.id)}
                  />

                  <div className="flex items-center gap-1">
                    <button
                      type="button"
                      onClick={() => handleMoveOrder("main", index, index - 1)}
                      disabled={index === 0}
                      title={t("move_up")}
                      aria-label={t("move_up")}
                      className="p-1 rounded-md text-gray-500 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors cursor-pointer"
                    >
                      <ArrowUp size={16} />
                    </button>
                    <button
                      type="button"
                      onClick={() => handleMoveOrder("main", index, index + 1)}
                      disabled={index === mainWidgets.length - 1}
                      title={t("move_down")}
                      aria-label={t("move_down")}
                      className="p-1 rounded-md text-gray-500 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors cursor-pointer"
                    >
                      <ArrowDown size={16} />
                    </button>
                    <button
                      type="button"
                      onClick={() => handleSwitchColumn(widget.id, "sidebar")}
                      title={t("move_to_sidebar")}
                      aria-label={t("move_to_sidebar")}
                      className="p-1 rounded-md text-gray-500 hover:bg-gray-100 transition-colors ml-1 cursor-pointer"
                    >
                      <ArrowRight size={16} />
                    </button>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Sidebar Column Section */}
        <div className="space-y-3">
          <div className="flex items-center justify-between">
            <h3 className="text-sm font-bold text-gray-900 uppercase tracking-wider">
              {t("sidebar")}
            </h3>
            <span className="text-xs text-gray-500 font-medium">
              {sidebarWidgets.filter((w) => w.visible).length} /{" "}
              {sidebarWidgets.length}
            </span>
          </div>

          <div className="flex flex-col gap-2">
            {sidebarWidgets.length === 0 ? (
              <p className="text-xs text-gray-400 italic py-2">
                {t("no_data")}
              </p>
            ) : (
              sidebarWidgets.map((widget, index) => (
                <div
                  key={widget.id}
                  className={`flex items-center justify-between p-3 rounded-xl border transition-colors ${
                    widget.visible
                      ? "bg-white border-gray-200 shadow-xs"
                      : "bg-gray-50 border-dashed border-gray-200 opacity-60"
                  }`}
                >
                  <Checkbox
                    id={`widget-toggle-${widget.id}`}
                    label={getWidgetTitle(widget.id)}
                    checked={widget.visible}
                    onChange={() => handleToggleVisible(widget.id)}
                  />

                  <div className="flex items-center gap-1">
                    <button
                      type="button"
                      onClick={() => handleSwitchColumn(widget.id, "main")}
                      title={t("move_to_main")}
                      aria-label={t("move_to_main")}
                      className="p-1 rounded-md text-gray-500 hover:bg-gray-100 transition-colors mr-1 cursor-pointer"
                    >
                      <ArrowLeft size={16} />
                    </button>
                    <button
                      type="button"
                      onClick={() =>
                        handleMoveOrder("sidebar", index, index - 1)
                      }
                      disabled={index === 0}
                      title={t("move_up")}
                      aria-label={t("move_up")}
                      className="p-1 rounded-md text-gray-500 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors cursor-pointer"
                    >
                      <ArrowUp size={16} />
                    </button>
                    <button
                      type="button"
                      onClick={() =>
                        handleMoveOrder("sidebar", index, index + 1)
                      }
                      disabled={index === sidebarWidgets.length - 1}
                      title={t("move_down")}
                      aria-label={t("move_down")}
                      className="p-1 rounded-md text-gray-500 hover:bg-gray-100 disabled:opacity-30 disabled:cursor-not-allowed transition-colors cursor-pointer"
                    >
                      <ArrowDown size={16} />
                    </button>
                  </div>
                </div>
              ))
            )}
          </div>
        </div>

        {/* Action Footer */}
        <div className="flex items-center justify-between pt-4 border-t border-gray-200">
          <Button
            type="button"
            variant="secondary"
            onClick={handleReset}
            className="flex items-center gap-2 text-xs"
          >
            <RotateCcw size={14} />
            {t("reset_to_default")}
          </Button>

          <Button type="button" variant="primary" onClick={handleSave}>
            {t("done")}
          </Button>
        </div>
      </div>
    </Modal>
  );
}

export default function PersonaliseDashboardModal(
  props: PersonaliseDashboardModalProps,
) {
  if (!props.isOpen) return null;
  return <PersonaliseDashboardModalContent {...props} />;
}
