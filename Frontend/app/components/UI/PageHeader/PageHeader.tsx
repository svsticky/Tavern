import { useNavigate } from "react-router";
import Button from "../Button";
import { t } from "i18next";
import { handleBack } from "./PageHeader.handlers";

export const PageHeader = ({ 
  title, 
  backTo, 
  onBack, 
  action 
}: { 
  title: string, 
  backTo?: string, 
  onBack?: () => void, 
  action?: React.ReactNode 
}) => {
  const navigate = useNavigate();

  return (
    <div className="mb-4 flex flex-row justify-between items-start w-full gap-4"> 
      <div className="flex flex-col items-start">
        {(backTo || onBack) && (
          <Button
            showArrow
            arrowDirection="left"
            className="bg-transparent p-0 hover:bg-transparent text-(--board-primary) shadow-none mb-2 min-h-0 h-auto"
            onClick={() => handleBack(onBack, backTo, navigate)}
          >
            {t("back")}
          </Button>
        )}
        <h1 className="text-2xl font-bold leading-tight">{title}</h1>
      </div>
      
      {action && (
        <div className="flex items-center flex-shrink-0">
          {action}
        </div>
      )}
    </div>
  );
};
