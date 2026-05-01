import { useState, useRef } from "react";
import { t } from "i18next";
import { type GroupType } from "~/api";
import Input from "~/components/UI/Input";
import Button from "~/components/UI/Button";
import Select from "~/components/UI/Select";
import Form from "../../UI/Form/Form";
import { handleCreateGroupSubmit, handleFileChange, resetCreateGroupForm } from "./CreateGroupOverlay.handlers";

/**
 * A modal overlay component for creating a new group.
 * Handles image selection, group naming, and type classification.
 * 
 * @component
 * @param {Object} props - Component props.
 * @param {Function} props.onSuccess - Callback function triggered after a group is successfully created.
 * @returns {JSX.Element} The rendered CreateGroupOverlay component.
 */
export default function CreateGroupOverlay({ onSuccess }: { onSuccess: () => void }) {
  const [loading, setLoading] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [imagePreview, setImagePreview] = useState<string | null>(null);

  const [formData, setFormData] = useState({
    name: "",
    type: 'Committee',
    groupPicture: null as File | null,
  });

  return (
      <Form
        onSubmit={(e) =>
          handleCreateGroupSubmit({
            e,
            formData,
            setLoading,
            onSuccess,
            resetForm: () => resetCreateGroupForm(setFormData, setImagePreview)
          })
        }
      >
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
            onChange={(e) => handleFileChange(e, formData, setFormData, setImagePreview)}
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

        <Button variant="primary" className="flex-1" disabled={loading || !formData.name || !formData.groupPicture} type="submit">
          {t("create")}
        </Button>
      </Form>
  );
}
