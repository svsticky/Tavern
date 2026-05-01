import { useState } from "react";
import { t } from "i18next";
import BorderedTile from "../../../Tiles/BorderedTile";
import Checkbox from "../../../UI/Checkbox";
import Button from "../../../UI/Button";
import Input from "../../../UI/Input";
import HtmlEditor from "../../../UI/HtmlEditor";
import { handleSendMail } from "./SendActivityMailComponent.handlers";

/**
 * A specialized administrative component that provides a rich-text interface for 
 * sending broadcast emails to activity participants.
 * 
 * Features:
 * - **Controlled Form**: Manages internal state for subject line, HTML content, and recipient toggles.
 * - **Rich Text Editing**: Integrates the `HtmlEditor` component for professional-grade mail formatting.
 * - **Smart Validation**: Disables the send button if the subject or content is missing, or if a 
 *   mail is currently being processed.
 * - **Success Lifecycle**: Automatically clears the form fields upon successful delivery.
 * 
 * @component
 * @param {Object} props - The component props.
 * @param {number} props.activityId - The unique ID of the activity used to determine the recipient pool.
 * 
 * @example
 * ```tsx
 * <SendActivityMailComponent 
 *   activityId={activity.id} 
 * />
 * ```
 */
export default function SendActivityMailComponent({ activityId }: { activityId: number }) {
    const [loading, setLoading] = useState<boolean>(false);
    const [subject, setSubject] = useState("");
    const [content, setContent] = useState("");
    const [includeWaitingList, setIncludeWaitingList] = useState(false);

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
                    onClick={() =>
                      handleSendMail({
                        activityId,
                        subject,
                        content,
                        includeWaitingList,
                        setLoading,
                        clearForm: () => {
                          setSubject("");
                          setContent("");
                        }
                      })
                    }
                    disabled={loading || !subject || !content}
                >
                    {t("send")}
                </Button>
            </div>
        </BorderedTile>
    );
}
