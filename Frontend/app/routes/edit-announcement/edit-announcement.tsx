import { useParams, useNavigate } from "react-router";
import { useEffect, useState } from "react";
import Input from "~/components/UI/Input";
import TextArea from "~/components/UI/TextArea";
import Button from "~/components/UI/Button";
import { t } from "i18next";
import { FormSection } from "~/components/UI/Form/FormSection";
import { PageHeader } from "~/components/UI/PageHeader/PageHeader";
import Form from "~/components/UI/Form/Form";
import { handleAnnouncementSubmit, loadAnnouncementData } from "./edit-announcement.handlers";

export default function AnnouncementFormPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const isEdit = !!id;

  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [initialData, setInitialData] = useState({ Title: "", Content: "" });

  useEffect(() => {
    loadAnnouncementData({ isEdit, id, setInitialData, setLoading });
  }, [id, isEdit]);

  if (loading) return <div className="p-8 text-center">{t("loading")}...</div>;

  return (
    <div className="max-w-4xl mx-auto">
      <PageHeader 
        title={isEdit ? t("edit_announcement") : t("create_announcement")} 
        backTo="/announcements" 
      />

      <Form onSubmit={(e) => handleAnnouncementSubmit({ e, isEdit, id, setSaving, navigate })}>
        <FormSection title={t("announcement_details")} columns={1}>
          <Input 
            label={t("title")} 
            name="Title" 
            defaultValue={initialData.Title} 
            required 
          />
          <TextArea 
            label={t("content")} 
            name="Content" 
            defaultValue={initialData.Content} 
            rows={12} 
            required 
          />
        </FormSection>

        <Button type="submit" disabled={saving} className="w-full">
          {saving ? t("saving") : isEdit ? t("update") : t("create")}
        </Button>
      </Form>
    </div>
  );
}
