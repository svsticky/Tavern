import { useParams, useNavigate } from "react-router";
import { useEffect, useState } from "react";
import { getApiAnnouncementsById, postApiAnnouncements, putApiAnnouncementsById } from "~/api";
import Input from "~/components/UI/Input";
import TextArea from "~/components/UI/TextArea";
import Button from "~/components/UI/Button";
import { t } from "i18next";
import { FormSection } from "~/components/UI/Form/FormSection";
import { PageHeader } from "~/components/UI/PageHeader";

export default function AnnouncementFormPage() {
  const { id } = useParams();
  const navigate = useNavigate();
  const isEdit = !!id;

  const [loading, setLoading] = useState(isEdit);
  const [saving, setSaving] = useState(false);
  const [initialData, setInitialData] = useState({ Title: "", Content: "" });

  useEffect(() => {
    if (isEdit) {
      getApiAnnouncementsById({ path: { id: Number(id) } })
        .then(res => {
          if (res.data) setInitialData({ Title: res.data.title, Content: res.data.content });
        })
        .finally(() => setLoading(false));
    }
  }, [id, isEdit]);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const fd = new FormData(e.currentTarget);
    const body = { 
        title: fd.get("Title") as string, 
        content: fd.get("Content") as string 
    };

    setSaving(true);
    try {
      if (isEdit) {
        await putApiAnnouncementsById({ path: { id: Number(id) }, body });
      } else {
        await postApiAnnouncements({ body });
      }
      navigate("/announcements");
    } catch (error) {
      console.error(error);
    } finally {
      setSaving(false);
    }
  };

  if (loading) return <div className="p-8 text-center">{t("loading")}...</div>;

  return (
    <div className="max-w-4xl mx-auto">
      <PageHeader 
        title={isEdit ? t("edit_announcement") : t("create_announcement")} 
        backTo="/announcements" 
      />

      <form onSubmit={handleSubmit} className="space-y-8">
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
      </form>
    </div>
  );
}