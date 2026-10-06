import { t } from "i18next";
import Button from "~/components/UI/Button";
import Modal from "~/components/UI/Modal/Modal";

/**
 * Props for the ConfirmModal component.
 * @interface ConfirmModalProps
 * @property {boolean} isOpen - Controls whether the modal is visible.
 * @property {string} title - The heading text displayed in the modal header.
 * @property {string} message - The question or warning shown to the user.
 * @property {string} [confirmLabel] - Label for the confirm button. Defaults to `t("confirm")`.
 * @property {string} [cancelLabel] - Label for the cancel button. Defaults to `t("cancel")`.
 * @property {"primary" | "secondary" | "danger"} [variant] - Visual style of the confirm button. Defaults to `"danger"`.
 * @property {() => void} onConfirm - Callback executed when the confirm button is clicked.
 * @property {() => void} onCancel - Callback executed when the modal is dismissed (cancel button, backdrop, close button, or Escape key).
 */
interface ConfirmModalProps {
  isOpen: boolean;
  title: string;
  message: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: "primary" | "secondary" | "danger";
  onConfirm: () => void;
  onCancel: () => void;
}

/**
 * A confirmation dialog built on top of the shared Modal component, meant as a drop-in
 * replacement for the browser's native `window.confirm`.
 *
 * @component
 */
export default function ConfirmModal({
  isOpen,
  title,
  message,
  confirmLabel,
  cancelLabel,
  variant = "danger",
  onConfirm,
  onCancel,
}: ConfirmModalProps) {
  return (
    <Modal isOpen={isOpen} onClose={onCancel} title={title}>
      <p className="mb-6 text-slate-700">{message}</p>
      <div className="flex justify-end gap-2">
        <Button type="button" variant="secondary" onClick={onCancel}>
          {cancelLabel ?? t("cancel")}
        </Button>
        <Button type="button" variant={variant} onClick={onConfirm}>
          {confirmLabel ?? t("confirm")}
        </Button>
      </div>
    </Modal>
  );
}
