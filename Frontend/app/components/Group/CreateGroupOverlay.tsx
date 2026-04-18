import React, { useState, useRef, type ReactEventHandler } from "react";
import { t } from "i18next";
import { postApiGroups, type GroupType } from "~/api";
import Modal from "~/components/UI/Modal";
import Input from "~/components/UI/Input";
import Button from "~/components/UI/Button";
import Select from "~/components/UI/Select";
import toast from "react-hot-toast";
import Form from "../UI/Form/Form";

interface CreateGroupModalProps {
  onSuccess: () => void;
}

export default function CreateGroupOverlay({ onSuccess }: CreateGroupModalProps) {
  const [loading, setLoading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [imagePreview, setImagePreview] = useState<string | null>(null);

  const [formData, setFormData] = useState({
    name: "",
    type: 'Committee',
    groupPicture: null as File | null,
  });

  const handleFileChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (file) {
      setFormData({ ...formData, groupPicture: file });
      setImagePreview(URL.createObjectURL(file));
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!formData.name || !formData.groupPicture) {
      toast.error(t("please_fill_all_fields"));
      return;
    }

    setLoading(true);
    try {
      await postApiGroups({
        body: {
          Name: formData.name,
          Type: formData.type as GroupType,
          GroupPicture: formData.groupPicture,
        }
      });

      toast.success(t("group_created_successfully"));
      onSuccess(); // Ververs de lijst
      resetForm();
    } catch (err) {
      toast.error(t("failed_to_create_group"));
      console.error(err);
    } finally {
      setLoading(false);
    }
  };

  const resetForm = () => {
    setFormData({ name: "", type: 'Committee', groupPicture: null });
    setImagePreview(null);
  };

  return (
      <Form onSubmit={handleSubmit} className="space-y-6">
        {/* Foto Upload Preview */}
        <div className="flex flex-col items-center gap-4">
          <div 
            className="w-32 h-32 rounded-full border-2 border-dashed border-slate-300 flex items-center justify-center overflow-hidden cursor-pointer hover:border-primary transition-colors"
            onClick={() => fileInputRef.current?.click()}
          >
            {imagePreview ? (
              <img src={imagePreview} className="w-full h-full object-cover" alt="Preview" />
            ) : (
              <span className="text-xs text-slate-400 text-center px-2">{t("upload_picture")}</span>
            )}
          </div>
          <input 
            type="file" 
            ref={fileInputRef} 
            hidden 
            accept="image/*" 
            onChange={handleFileChange} 
          />
        </div>

        <Input 
          label={t("group_name")} 
          required 
          value={formData.name} 
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({ ...formData, name: e.target.value })} 
        />

        <Select 
          label={t("group_type")}
          value={formData.type}
          onChange={(e) => setFormData({ ...formData, type: e.target.value as GroupType })}
          options={[
            { value: 'Committee', label: t("committee") },
            { value: 'WorkingGroup', label: t("department") },
            { value: 'Dispute', label: t("dispute") },
          ]}
        />

        <div className="flex gap-3 pt-4">
          <Button variant="primary" className="flex-1" disabled={loading || !formData.name || !formData.groupPicture} type="submit">
            {t("create")}
          </Button>
        </div>
      </Form>
  );
}