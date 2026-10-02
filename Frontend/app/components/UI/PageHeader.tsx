import { t } from "i18next";
import { useNavigate } from "react-router";
import { hasInAppHistory } from "~/util/navigation.util";
import Button from "./Button";

const BACK_BUTTON_CLASSNAME =
  "bg-transparent p-0 hover:bg-transparent text-(--board-primary) shadow-none mb-2 min-h-0 h-auto";

/**
 * A standardized header component for main application pages.
 *
 * It displays a prominent page title and optionally provides a back button and a
 * right-aligned action area. The back button functionality is intelligent: it can
 * either trigger a custom callback, or mimic the browser's native back navigation,
 * falling back to the configured route only when there's nothing to go back to.
 *
 * @component
 * @param {Object} props - The component properties.
 * @param {string} props.title - The primary title of the page.
 * @param {string} [props.backTo] - The route to fall back to when there's no previous page to go back to.
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
  const navigate = useNavigate();

  const handleBackClick = (event: React.MouseEvent<HTMLAnchorElement>) => {
    // Let modified/middle clicks (open in new tab) use the plain href instead.
    if (
      event.defaultPrevented ||
      event.button !== 0 ||
      event.metaKey ||
      event.ctrlKey ||
      event.shiftKey ||
      event.altKey
    ) {
      return;
    }

    event.preventDefault();

    const hasSomewhereToGoBack = hasInAppHistory() || document.referrer !== "";

    if (hasSomewhereToGoBack) {
      navigate(-1);
    } else if (backTo) {
      navigate(backTo);
    }
  };

  return (
    <div className="mb-4 flex flex-row flex-wrap justify-between items-center w-full gap-x-4 gap-y-2">
      <div className="flex flex-col items-start">
        {onBack ? (
          <Button
            showArrow
            arrowDirection="left"
            className={BACK_BUTTON_CLASSNAME}
            onClick={onBack}
          >
            {t("back")}
          </Button>
        ) : (
          backTo && (
            <Button
              showArrow
              arrowDirection="left"
              className={BACK_BUTTON_CLASSNAME}
              href={backTo}
              onClick={handleBackClick}
            >
              {t("back")}
            </Button>
          )
        )}
        <h1 className="text-2xl font-bold leading-tight">{title}</h1>
      </div>

      {action && (
        <div className="flex-grow sm:flex-grow-0 flex justify-end">
          {action}
        </div>
      )}
    </div>
  );
};
