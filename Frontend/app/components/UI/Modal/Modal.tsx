import { X } from "lucide-react";
import { t } from "i18next";
import { useEffect } from "react";
import { createModalKeyDownHandler } from "./Modal.handlers";

/**
 * Props for the Modal component.
 * @interface ModalProps
 * @property {boolean} isOpen - Controls whether the modal is visible.
 * @property {() => void} onClose - Callback function to execute when the modal is requested to close (via backdrop click, close button, or Escape key).
 * @property {string} title - The heading text displayed in the modal header.
 * @property {React.ReactNode} children - The content to be rendered inside the modal body.
 */
interface ModalProps {
  isOpen: boolean;
  onClose: () => void;
  title: string;
  children: React.ReactNode;
}

/**
 * A highly accessible overlay component for displaying dialogs, forms, or focus-heavy content.
 * 
 * This component handles several UX requirements:
 * - **Scroll Locking**: Prevents the background body from scrolling when the modal is active.
 * - **Accessibility**: Supports closing via the "Escape" key and backdrop interaction.
 * - **Responsive Design**: Renders as a full-screen view on mobile and a centered card on larger screens.
 * - **Transitions**: Includes entrance animations using Tailwind's `animate-in` utilities.
 * 
 * @component
 * @param {ModalProps} props - The component properties.
 */
export default function Modal({ isOpen, onClose, title, children }: ModalProps) {
  useEffect(() => {
    const onKeyDown = createModalKeyDownHandler(onClose);

    if (isOpen) {
      document.body.style.overflow = "hidden";
      window.addEventListener("keydown", onKeyDown);
    } else {
      document.body.style.overflow = "unset";
    }

    return () => {
      document.body.style.overflow = "unset";
      window.removeEventListener("keydown", onKeyDown);
    };
  }, [isOpen, onClose]);

  if (!isOpen) return null;

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-0 sm:p-4">
      <div 
        className="absolute inset-0 bg-slate-900/60 backdrop-blur-sm" 
        onClick={onClose} 
      />
      
      <div className="relative bg-white w-full h-full sm:h-auto sm:max-w-lg sm:rounded-2xl shadow-2xl flex flex-col overflow-hidden animate-in fade-in zoom-in duration-200">
        <div className="flex items-center justify-between p-4 border-b">
          <h2 className="font-bold text-lg text-slate-900">{title}</h2>
          <button 
            onClick={onClose} 
            className="p-2 hover:bg-slate-100 rounded-full transition-colors"
            aria-label={t("close_modal")}
          >
            <X size={20} className="hover:cursor-pointer" />
          </button>
        </div>
        
        <div className="flex-1 overflow-y-auto p-4">
          {children}
        </div>
      </div>
    </div>
  );
}
