import { t } from "i18next";
import { useCallback, useRef, useState } from "react";
import ConfirmModal from "./ConfirmModal";

/**
 * Options accepted by the `confirm` function returned from {@link useConfirm}.
 * @interface ConfirmOptions
 * @property {string} [title] - The heading text displayed in the modal header. Defaults to `t("confirm")`.
 * @property {string} [confirmLabel] - Label for the confirm button. Defaults to `t("confirm")`.
 * @property {string} [cancelLabel] - Label for the cancel button. Defaults to `t("cancel")`.
 * @property {"primary" | "secondary" | "danger"} [variant] - Visual style of the confirm button. Defaults to `"danger"`.
 */
interface ConfirmOptions {
  title?: string;
  confirmLabel?: string;
  cancelLabel?: string;
  variant?: "primary" | "secondary" | "danger";
}

type PendingConfirm = {
  message: string;
  options?: ConfirmOptions;
};

/**
 * A hook that replaces the browser's native `window.confirm` with the app's own modal.
 *
 * Returns a tuple of `[confirmModal, confirm]`:
 * - `confirmModal` must be rendered somewhere in the component's JSX tree.
 * - `confirm(message, options?)` opens the modal and resolves to `true`/`false` depending on
 *   the user's choice, mirroring the return value of `window.confirm`.
 *
 * @example
 * const [confirmModal, confirm] = useConfirm();
 * // ...
 * if (!(await confirm(t("delete_account_confirmation")))) return;
 * // ...
 * return <>{confirmModal}</>;
 */
export function useConfirm(): [
  React.ReactNode,
  (message: string, options?: ConfirmOptions) => Promise<boolean>,
] {
  const [pending, setPending] = useState<PendingConfirm | null>(null);
  const resolveRef = useRef<((result: boolean) => void) | null>(null);

  const confirm = useCallback((message: string, options?: ConfirmOptions) => {
    setPending({ message, options });
    return new Promise<boolean>((resolve) => {
      resolveRef.current = resolve;
    });
  }, []);

  const respond = (result: boolean) => {
    setPending(null);
    resolveRef.current?.(result);
    resolveRef.current = null;
  };

  const confirmModal = (
    <ConfirmModal
      isOpen={pending !== null}
      title={pending?.options?.title ?? t("confirm")}
      message={pending?.message ?? ""}
      confirmLabel={pending?.options?.confirmLabel}
      cancelLabel={pending?.options?.cancelLabel}
      variant={pending?.options?.variant}
      onConfirm={() => respond(true)}
      onCancel={() => respond(false)}
    />
  );

  return [confirmModal, confirm];
}
