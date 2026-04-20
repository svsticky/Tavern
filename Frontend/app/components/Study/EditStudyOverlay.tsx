import { deleteApiStudiesById, postApiStudies, putApiStudiesById, type Study, type StudyType } from "~/api";
import Form from "../UI/Form/Form";
import Input from "../UI/Input";
import Select from "../UI/Select";
import Button from "../UI/Button";
import { useState } from "react";
import { t } from "i18next";
import toast from "react-hot-toast";

export default function EditStudyOverlay({onStudyAdded: onComplete, study = undefined}: {onStudyAdded: (study?: Study) => void, study?: Study}) {
    const [formData, setFormData] = useState({
        title: study ? study.title : "",
        type: study ? study.type : "Bachelor",
        nominalDurationYears: study?.nominalDurationYears ? study.nominalDurationYears : 0,
    });
    const [loading, setLoading] = useState(false);

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!formData.title || !formData.type || !formData.nominalDurationYears) {
            return;
        }

        const editStudyProcess = async () => {
            setLoading(true);
            try {
                const response = study
                    ? await putApiStudiesById({
                        path: { id: study.id! },
                        body: {
                            title: formData.title,
                            type: formData.type as StudyType,
                            nominalDurationYears: formData.nominalDurationYears,
                        }
                    })
                    : await postApiStudies({
                        body: {
                            title: formData.title,
                            type: formData.type as StudyType,
                            nominalDurationYears: formData.nominalDurationYears,
                        }
                    });

                if (!response.error) {
                    onComplete({title: formData.title, type: formData.type as StudyType, nominalDurationYears: formData.nominalDurationYears, id: study ? study.id : (response.data as any).id});
                }
            } catch (error) {
                console.error("Error creating study:", error);
            } finally {
                setLoading(false);
            }
        };

        toast.promise(editStudyProcess(), {
            loading: t("creating_study"),
            success: t("study_created_successfully"),
            error: t("failed_to_create_study")
        });
    };

    const handleDelete = async () => {
        if (!study) {
            toast.error(t("no_study_to_delete"));
            return;
        }

        const deleteStudyProcess = async () => {
            setLoading(true);
            try {
                await deleteApiStudiesById({ path: { id: study.id! } });
                onComplete();
            } catch (error) {
                console.error("Error deleting study:", error);
            } finally {
                setLoading(false);
            }
        };
        toast.promise(deleteStudyProcess(), {
            loading: t("deleting_study"),
            success: t("study_deleted_successfully"),
            error: t("failed_to_delete_study")
        });
    };

    return (
        <Form onSubmit={handleSubmit}>
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
                <Button variant="danger" className="flex-1" onClick={handleDelete}>
                    {t("delete")}
                </Button>
            )}
        </Form>
    );
}