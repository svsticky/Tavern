import { useState } from "react";
import { t } from "i18next";
import BorderedTile from "../../Tiles/BorderedTile";
import Checkbox from "../../UI/Checkbox";
import Button from "../../UI/Button";
import Input from "../../UI/Input";
import toast from "react-hot-toast";
import { postApiMailsActivity } from "~/api";
import HtmlEditor from "../../UI/HtmlEditor";

export default function SendActivityMailComponent({ activityId }: { activityId: number }) {
    const [loading, setLoading] = useState<boolean>(false);
    const [subject, setSubject] = useState("");
    const [content, setContent] = useState("");
    const [includeWaitingList, setIncludeWaitingList] = useState(false);

    const handleSendMail = async () => {
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
                        subject: subject,
                        includeWaitingList
                    }
                });

                if (response.status !== 200) throw new Error();
                
                setSubject("");
                setContent("");
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

    return (
        <BorderedTile title={t("send_activity_mail")}>
            <div className="flex flex-col gap-4">
                <Input 
                    id="mailSubject" 
                    label={t("mail_subject")} 
                    value={subject}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) => setSubject(e.target.value)}
                />
                
                <HtmlEditor 
                    label={t("mail_content")}
                    value={content}
                    onChange={setContent}
                    placeholder={t("write_your_mail_here")}
                />

                <Checkbox 
                    id="includeWaitingList" 
                    label={t("include_waiting_list")} 
                    checked={includeWaitingList}
                    onChange={(e: React.ChangeEvent<HTMLInputElement>) => setIncludeWaitingList(e.target.checked)}
                />

                <Button 
                    onClick={handleSendMail} 
                    disabled={loading || !subject || !content}
                >
                    {t("send")}
                </Button>
            </div>
        </BorderedTile>
    );
}