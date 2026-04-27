import { type Study, type StudyType } from "~/api";
import Form from "../../UI/Form/Form";
import Input from "../../UI/Input";
import Select from "../../UI/Select";
import Button from "../../UI/Button";
import { useState } from "react";
import { t } from "i18next";
import { handleStudyDelete, handleStudySubmit } from "./EditStudyOverlay.handlers";

export default function EditStudyOverlay({onStudyAdded: onComplete, study = undefined}: {onStudyAdded: (study?: Study) => void, study?: Study}) {
    const [formData, setFormData] = useState({
        title: study ? study.title : "",
        type: study?.type ?? "Bachelor",
        nominalDurationYears: study?.nominalDurationYears ? study.nominalDurationYears : 0,
    });
    const [loading, setLoading] = useState(false);

    return (
        <Form onSubmit={(e) => handleStudySubmit({ e, formData, study, setLoading, onComplete })}>
            <Input 
                label={t("name")} 
                value={formData.title} 
                onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({ ...formData, title: e.target.value })} 
                required 
            />
    
            <Select 
                label={t("study_type")}
                value={formData.type}
                onChange={(e) => setFormData({ ...formData, type: e.target.value as StudyType })}
                options={[
                    { value: 'Bachelor', label: t("bachelor") },
                    { value: 'Master', label: t("master") }
                ]}
                required
            />

            <Input 
                label={t("nominal_duration")} 
                type="number" 
                min="1"
                required 
                value={formData.nominalDurationYears} 
                onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({ ...formData, nominalDurationYears: Number(e.target.value) })} 
            />
    
            <Button variant="primary" className="flex-1" disabled={loading || !formData.title || !formData.type || !formData.nominalDurationYears} type="submit">
                {study ? t("save") : t("create")}
            </Button>

            {study && (
                <Button variant="danger" className="flex-1" onClick={() => handleStudyDelete({ study, setLoading, onComplete })}>
                    {t("delete")}
                </Button>
            )}
        </Form>
    );
}
