import { t } from "i18next";
import Button from "./Button";

/**
 * A standardized header component for main application pages.
 *
 * It displays a prominent page title and optionally provides a back button and a
 * right-aligned action area. The back button functionality is intelligent: it can
 * either trigger a custom callback or navigate to a specific route.
 *
 * @component
 * @param {Object} props - The component properties.
 * @param {string} props.title - The primary title of the page.
 * @param {string} [props.backTo] - The route path to navigate to when the back button is clicked.
 * @param {() => void} [props.onBack] - A custom callback to execute for back-navigation instead of routing.
 * @param {React.ReactNode} [props.action] - Optional content (like buttons or menus) to display on the right side of the header.
 */
export const PageHeader = ({
  title,
  backTo,
  onBack,
  action,
}: {
  title: string;
  backTo?: string;
  onBack?: () => void;
  action?: React.ReactNode;
}) => {
  return (
    <div className="mb-4 flex flex-row justify-between items-center w-full gap-x-4 gap-y-2">
      <div className="flex flex-col items-start min-w-0 flex-1">
        {(backTo || onBack) && (
          <Button
            showArrow
            arrowDirection="left"
            className="bg-transparent p-0 hover:bg-transparent text-(--board-primary) shadow-none mb-2 min-h-0 h-auto"
            onClick={onBack}
            href={backTo}
          >
            {t("back")}
          </Button>
        )}
        <h1
          className="text-2xl font-bold leading-tight truncate w-full"
          title={title}
        >
          {title}
        </h1>
      </div>

      {action && <div className="shrink-0 flex justify-end">{action}</div>}
    </div>
  );
};
