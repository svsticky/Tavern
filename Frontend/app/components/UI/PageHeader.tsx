import { useNavigate } from "react-router";
import Button from "./Button";
import { t } from "i18next";

export const PageHeader = ({ title, backTo, onBack, action }: { title: string, backTo?: string, onBack?: () => void, action?: React.ReactNode }) => {
  const navigate = useNavigate();
  
  return (
    <div className="mb-4 flex flex-row justify-between items-end w-full"> 
      <div className="flex flex-col items-start">
        {(backTo || onBack) && (
          <Button
            showArrow
            arrowDirection="left"
            className="bg-transparent p-0 hover:bg-transparent text-(--board-primary) shadow-none mb-2 min-h-0 h-auto"
            onClick={() => (onBack ? onBack() : navigate(backTo!))}
          >
            {t("back")}
          </Button>
        )}
        <h1 className="text-2xl font-bold leading-none">{title}</h1>
      </div>
      
      {action && (
        <div className="flex items-center">
          {action}
        </div>
      )}
    </div>
  );
};