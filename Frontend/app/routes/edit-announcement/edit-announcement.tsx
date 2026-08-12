import { t } from "i18next";
import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router";
import Button from "~/components/UI/Button";
import Form from "~/components/UI/Form/Form";
import { FormSection } from "~/components/UI/Form/FormSection";
import Input from "~/components/UI/Input";
import { PageHeader } from "~/components/UI/PageHeader";
import TextArea from "~/components/UI/TextArea";
import {
  handleAnnouncementSubmit,
  handleDeleteAnnouncement,
  loadAnnouncementData,
} from "./edit-announcement.handlers";

/**
 * An administrative page for creating or editing system-wide announcements.
 *
 * This component provides a focused interface for board members to communicate with the association.
 * It manages several operational states:
 * - **Context Switching**: Automatically toggles between 'Create' and 'Edit' modes based on the URL ID parameter.
 * - **Data Hydration**: Fetches existing announcement content when in edit mode to prepopulate the form.
 * - **Form Orchestration**: Leverages specialized handlers to process complex logic like FormData extraction,
 *   API status management (saving/deleting), and navigation.
 * - **Destructive Actions**: Provides a 'Delete' option only when modifying an existing entry,
 *   protected by loading states to prevent duplicate requests.
 *
 * The layout is constrained to a readable width (`max-w-4xl`) to improve the editing experience
 * for long-form announcement content.
 *
 * @page
 * @component
 */
export default function AnnouncementFormPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const isEdit = !!id;

  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [deleting, setDeleting] = useState(false);
  const [initialData, setInitialData] = useState({
    TitleDutch: "",
    TitleEnglish: "",
    ContentDutch: "",
    ContentEnglish: "",
  });

  useEffect(() => {
    loadAnnouncementData({ isEdit, id, setInitialData, setLoading });
  }, [id, isEdit]);

  if (loading) return t("loading");

  return (
    <div className="max-w-4xl mx-auto">
      <PageHeader
        title={isEdit ? t("edit_announcement") : t("create_announcement")}
        backTo="/announcements"
      />

      <Form
        onSubmit={(e) =>
          handleAnnouncementSubmit({ e, isEdit, id, setSaving, navigate })
        }
      >
        <FormSection title={t("announcement_details")} columns={2}>
          <Input
            label={t("title_nl")}
            name="TitleDutch"
            defaultValue={initialData.TitleDutch}
            required
          />
          <Input
            label={t("title_en")}
            name="TitleEnglish"
            defaultValue={initialData.TitleEnglish}
            required
          />
          <TextArea
            label={t("description_nl")}
            name="ContentDutch"
            defaultValue={initialData.ContentDutch}
            rows={12}
            required
          />
          <TextArea
            label={t("description_en")}
            name="ContentEnglish"
            defaultValue={initialData.ContentEnglish}
            rows={12}
            required
          />
        </FormSection>

        {id && (
          <Button
            variant="danger"
            type="button"
            onClick={() => handleDeleteAnnouncement(id, setDeleting, navigate)}
            disabled={saving || deleting}
            className="w-full sm:w-auto"
          >
            {deleting ? `${t("deleting")}...` : t("delete")}
          </Button>
        )}

        <Button type="submit" disabled={saving || deleting} className="w-full">
          {saving ? t("saving") : isEdit ? t("update") : t("create")}
        </Button>
      </Form>
    </div>
  );
}
