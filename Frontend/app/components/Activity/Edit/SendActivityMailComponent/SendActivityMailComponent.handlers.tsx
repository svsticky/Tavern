import { t } from "i18next";
import toast from "react-hot-toast";
import { postMailsActivity } from "~/api";
import { appendErrorMessage } from "~/util/error.util";

/**
 * Handles the logic for sending a broadcast email to activity participants.
 *
 * Features:
 * - **Content Validation**: Prevents sending if the content is empty or contains only empty HTML tags.
 * - **Toast Feedback**: Uses `toast.promise` to provide real-time loading, success, and error notifications.
 * - **State Management**: Controls a loading spinner state and clears the form inputs upon a successful send.
 *
 * @function
 * @param {Object} args - The configuration object.
 * @param {number} args.activityId - The unique identifier of the activity whose participants should receive the mail.
 * @param {string} args.subject - The subject line of the email.
 * @param {string} args.content - The HTML content string (usually from a rich text editor).
 * @param {boolean} args.includeWaitingList - Whether members on the activity's waiting list should also receive the email.
 * @param {(loading: boolean) => void} args.setLoading - Callback to toggle the UI's loading/submitting state.
 * @param {() => void} args.clearForm - Callback to reset the email form inputs after a successful operation.
 *
 * @example
 * ```tsx
 * handleSendMail({
 *   activityId: 42,
 *   subject: "Important Update",
 *   content: htmlString,
 *   includeWaitingList: true,
 *   setLoading: setIsSending,
 *   clearForm: () => setContent("")
 * });
 * ```
 */
export const handleSendMail = async ({
  activityId,
  subject,
  content,
  includeWaitingList,
  setLoading,
  clearForm,
}: {
  activityId: number;
  subject: string;
  content: string;
  includeWaitingList: boolean;
  setLoading: (loading: boolean) => void;
  clearForm: () => void;
}) => {
  if (!content || content === "<p><br></p>") {
    toast.error(appendErrorMessage(t("content_required")));
    return;
  }

  const sendMailAction = async () => {
    setLoading(true);
    try {
      const response = await postMailsActivity({
        body: {
          activityId,
          htmlContent: content,
          subject,
          includeWaitingList,
        },
      });

      if (response.error) {
        throw response.error ?? new Error("Failed to send mail");
      }

      clearForm();
    } finally {
      setLoading(false);
    }
  };

  toast.promise(sendMailAction(), {
    loading: t("sending_mail"),
    success: t("mail_sent_successfully"),
    error: (error) => appendErrorMessage(t("sending_mail_failed"), error),
  });
};
