import { t } from "i18next";
import toast from "react-hot-toast";
import { postApiMailsActivity } from "~/api";

type SendMailArgs = {
  activityId: number;
  subject: string;
  content: string;
  includeWaitingList: boolean;
  setLoading: (loading: boolean) => void;
  clearForm: () => void;
};

export const handleSendMail = async ({ activityId, subject, content, includeWaitingList, setLoading, clearForm }: SendMailArgs) => {
  if (!content || content === "<p><br></p>") {
    toast.error(t("content_required"));
    return;
  }

  const sendMailAction = async () => {
    setLoading(true);
    try {
      const response = await postApiMailsActivity({
        body: {
          activityId,
          htmlContent: content,
          subject,
          includeWaitingList
        }
      });

      if (response.status !== 200) throw new Error();

      clearForm();
    } finally {
      setLoading(false);
    }
  };

  toast.promise(sendMailAction(), {
    loading: t("sending_mail"),
    success: t("mail_sent_successfully"),
    error: t("sending_mail_failed")
  });
};
