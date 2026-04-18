import React, { useEffect, useState } from 'react';
import BorderedTile from '../../Tiles/BorderedTile';
import Form from '../../UI/Form/Form';
import Button from '../../UI/Button';
import { t } from 'i18next';
import { FormSection } from '../../UI/Form/FormSection';
import { NoContentTile } from '../../Tiles/NoContentTile';
import { FormHeader } from '../../UI/Form/FormHeader';
import Checkbox from '../../UI/Checkbox';
import Input from '../../UI/Input';
import Select from '../../UI/Select';
import TextArea from '../../UI/TextArea';
import { getApiGroups, postApiActivities, putApiActivitiesById, type ActivityResponseDto, type GetSpecificationQuestionResponseDto, type GroupResponseDto } from '~/api';
import { useNavigate } from 'react-router';
import { audienceMap } from '~/types/AudienceMap';
import toast from 'react-hot-toast';
import { getAssociationYear } from '~/util/date.util';
import EditQuestionTile from './EditQuestionTile';

const formatForInput = (isoString?: string) => isoString ? isoString.substring(0, 16) : "";
const formatDateOnly = (isoString?: string) => isoString ? isoString.substring(0, 10) : "";

export default function EditActivityForm({ activity, id, isBoard }: { activity: ActivityResponseDto | null; id: string | undefined; isBoard: boolean }) {
    const navigate = useNavigate();
    const { pathname } = window.location;

    const isEdit = !!id;

    const [loading, setLoading] = useState(isEdit);
    const [saving, setSaving] = useState(false);
    const [groups, setGroups] = useState<GroupResponseDto[]>([]);
    const [formValid, setFormValid] = useState(isEdit);
    const [questions, setQuestions] = useState<Partial<GetSpecificationQuestionResponseDto>[]>([]);

    useEffect(() => {
        if (activity?.specificationQuestions) {
            setQuestions(activity.specificationQuestions);
        }
    }, [activity]);

    useEffect(() => {
        async function loadData() {
        try {
            const groupsRes = await getApiGroups({ 
            query: { IncludeInactive: false, MembershipYear: getAssociationYear() } 
            });
            if (groupsRes.data) setGroups(groupsRes.data);
        } catch (error) {
            console.error("Error loading data:", error);
            toast.error(t("loading_failed"));
        } finally {
            setLoading(false);
        }
        }

        if(isEdit) {
            setFormValid(true);
        }
        
        loadData();
    }, [id, isEdit]);

    const handleFormChange = (e: React.FormEvent<HTMLFormElement>) => {
        const fd = new FormData(e.currentTarget);
        const required = ["Name", "DateTimeStart", "DateTimeEnd", "Location", "OrganizerId"];
        setFormValid(required.every(field => !!fd.get(field)));
    };

    const addQuestion = () => {
        setQuestions([...questions, { 
        questionDutch: "", 
        questionEnglish: "", 
        type: "String", 
        isMandatory: false, 
        isPublic: true, 
        options: [] 
        }]);
    };

    const removeQuestion = (index: number) => {
        setQuestions(questions.filter((_, i) => i !== index));
    };

    const updateQuestion = (index: number, field: keyof GetSpecificationQuestionResponseDto, value: any) => {
        const newQuestions = [...questions];
        newQuestions[index] = { ...newQuestions[index], [field]: value };
        setQuestions(newQuestions);
    };

    const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
        e.preventDefault();

        const fd = new FormData(e.currentTarget);
        const audienceFlags = fd.getAll("AudienceBit").reduce((acc, val) => acc + Number(val), 0);

        setSaving(true);
        
        const payload = {
        body: {
            Name: fd.get("Name") as string,
            Location: fd.get("Location") as string,
            DutchDescription: fd.get("DutchDescription") as string,
            EnglishDescription: fd.get("EnglishDescription") as string,
            Price: Number(fd.get("Price")) || 0,
            ParticipantLimit: fd.get("ParticipantLimit") ? Number(fd.get("ParticipantLimit")) : undefined,
            OrganizerId: fd.get("OrganizerId") ? Number(fd.get("OrganizerId")) : undefined,
            DateTimeStart: new Date(fd.get("DateTimeStart") as string).toISOString(),
            DateTimeEnd: new Date(fd.get("DateTimeEnd") as string).toISOString(),
            EnrollmentDeadline: fd.get("EnrollmentDeadline") ? new Date(fd.get("EnrollmentDeadline") as string).toISOString() : undefined,
            UnenrollmentDeadline: fd.get("UnenrollmentDeadline") ? new Date(fd.get("UnenrollmentDeadline") as string).toISOString() : undefined,

            ShowInKoala: isBoard ? fd.get("ShowInKoala") === "on" : false,
            ShowOnWebsite: isBoard ? fd.get("ShowOnWebsite") === "on" : false,
            IsEnrollable: isBoard ? fd.get("IsEnrollable") === "on" : false,
            AreParticipantsVisible: fd.get("AreParticipantsVisible") === "on",
            IsAdultOnly: fd.get("IsAdultOnly") === "on",

            AllowedAudience: audienceMap[audienceFlags],

            VatRate: isBoard ? (fd.get("VatRate") ? Number(fd.get("VatRate")) : undefined) : undefined,
            GLAccountId: isBoard ? (fd.get("GLAccountId") as string || undefined) : undefined,
            CostCenterId: isBoard ? (fd.get("CostCenterId") as string || undefined) : undefined,

            Poster: (fd.get("Poster") as File)?.size > 0 ? (fd.get("Poster") as File) : undefined,

            SpecificationQuestionsJson: JSON.stringify(questions),

            PaymentDeadline: isBoard ? (fd.get("PaymentDeadline") ? new Date(fd.get("PaymentDeadline") as string).toISOString() : undefined) : undefined
        }
        };

        const submitProcess = async () => {
        try {
            if (isEdit) {
                console.log("Updating activity with payload:", payload);
                await putApiActivitiesById({ path: { id: Number(id) }, ...payload });
            } else {
                console.log("Creating activity with payload:", payload);
                await postApiActivities(payload);
            }
            
            const redirectPath = `${pathname.startsWith("/admin") ? '/admin' : ''}/activities`;

            navigate(redirectPath);
        } catch (error) {
            console.error(error);
            throw error;
        } finally {
            setSaving(false);
        }
        }

        toast.promise(submitProcess(), {
        loading: isEdit ? t("saving") : t("creating"),
        success: isEdit ? t("activity_updated") : t("activity_created"),
        error: isEdit ? t("activity_update_failed") : t("activity_creation_failed")
        });
    };

  if (loading) return t("loading");

  return (
    <div className="lg:col-span-2">
        <BorderedTile>
            <Form onSubmit={handleSubmit} onChange={handleFormChange}>
                <FormSection title={t("basic_information")}>
                <Input label={t("name")} name="Name" defaultValue={activity?.name} required />
                <Input label={t("location")} name="Location" defaultValue={activity?.location} required />
                <TextArea label={t("dutch_description")} rows={10} name="DutchDescription" defaultValue={activity?.dutchDescription} />
                <TextArea label={t("english_description")} rows={10} name="EnglishDescription" defaultValue={activity?.englishDescription} />
                </FormSection>

                <FormSection title={t("planning_enrollment")} columns={2}>
                <Input label={t("datetime_start")} name="DateTimeStart" type="datetime-local" defaultValue={formatForInput(activity?.dateTimeStart)} required />
                <Input label={t("datetime_end")} name="DateTimeEnd" type="datetime-local" defaultValue={formatForInput(activity?.dateTimeEnd)} required />
                <Input label={t("enrollment_deadline")} name="EnrollmentDeadline" type="datetime-local" defaultValue={formatForInput(activity?.enrollmentDeadline ?? "")} />
                <Input label={t("unenrollment_deadline")} name="UnenrollmentDeadline" type="datetime-local" defaultValue={formatForInput(activity?.unenrollmentDeadline ?? "")} />
                </FormSection>

                <FormSection columns={2}>
                <div>
                    <FormHeader title={t("target_audience")} border />
                    <div className="flex flex-wrap gap-4 p-4 bg-gray-50 rounded-xl mt-4">
                    <Checkbox label={t("year_1")} name="AudienceBit" value="1" defaultChecked={isEdit ? !!(activity?.allowedAudience ?? 0 & 1) : true} />
                    <Checkbox label={t("year_2")} name="AudienceBit" value="2" defaultChecked={isEdit ? !!(activity?.allowedAudience ?? 0 & 2) : true} />
                    <Checkbox label={t("year_3_plus")} name="AudienceBit" value="4" defaultChecked={isEdit ? !!(activity?.allowedAudience ?? 0 & 4) : true} />
                    <Checkbox label={t("masters")} name="AudienceBit" value="8" defaultChecked={isEdit ? !!(activity?.allowedAudience ?? 0 & 8) : true} />
                    </div>
                </div>
                <div>
                    <FormHeader title={t("organizer")} border />
                    <Select 
                    label={t("organizer")}
                    name="OrganizerId"
                    defaultValue={activity?.organizerId ?? ""}
                    required
                    options={[
                        { value: "", label: t("select_organizer") },
                        ...groups.map(g => ({ value: g?.id ?? 0, label: g?.name ?? "" }))
                    ]}
                    />
                </div>
                </FormSection>

                <FormSection title={t("finance_capacity")} columns={2}>
                <Input label={t("price")} name="Price" type="number" step="0.01" defaultValue={activity?.price} />
                <Input label={t("participant_limit")} name="ParticipantLimit" type="number" defaultValue={activity?.participantLimit} />
                {isBoard && (
                    <>
                    <Input label={t("vat_rate")} name="VatRate" type="number" defaultValue={activity?.vatRate} />
                    <Input label={t("gl_account_id")} name="GLAccountId" defaultValue={activity?.glAccountId} />
                    <Input label={t("cost_center_id")} name="CostCenterId" defaultValue={activity?.costCenterId} />
                    <Input label={t("payment_deadline")} name="PaymentDeadline" type="date" defaultValue={formatDateOnly(activity?.paymentDeadline ?? "")} />
                    </>
                )}
                </FormSection>

                <FormSection columns={2}>
                <div className="grid grid-cols-2 gap-4">
                    <div className="col-span-2"><FormHeader title={t("settings")} border /></div>
                    {isBoard && (
                    <>
                        <Checkbox label={t("is_enrollable")} name="IsEnrollable" defaultChecked={activity?.isEnrollable ?? true} />
                        <Checkbox label={t("show_in_koala")} name="ShowInKoala" defaultChecked={activity?.showInKoala ?? true} />
                        <Checkbox label={t("show_on_website")} name="ShowOnWebsite" defaultChecked={activity?.showOnWebsite ?? true} />
                    </>
                    )}
                    <Checkbox label={t("are_participants_visible")} name="AreParticipantsVisible" defaultChecked={activity?.areParticipantsVisible ?? true} />
                    <Checkbox label={t("is_adult_only")} name="IsAdultOnly" defaultChecked={activity?.isAdultOnly ?? false} />
                </div>
                <div>
                    <FormHeader title={t("poster")} border />
                    <input 
                    name="Poster" 
                    type="file" 
                    accept="image/png, image/jpeg, image/gif, image/webp"
                    className="w-full p-2 border border-dashed rounded-lg mt-4" 
                    />
                    {isEdit && <p className="text-xs text-gray-400 mt-2 italic">{t("leave_empty_to_keep_current")}</p>}
                </div>
                </FormSection>

                <FormSection title={t("specification_questions")} columns={1}>
                <div className="space-y-6">
                    <div className="flex flex-col sm:flex-row sm:justify-between sm:items-start gap-4">
                    <span className="min-w-0 flex-1">
                        {t("specification_questions_description")}
                    </span>
                    <Button type="button" variant="secondary" onClick={addQuestion} className="flex-none whitespace-nowrap">
                        + {t("add_question")}
                    </Button>
                    </div>
                    {questions.map((q, index) => (
                        <EditQuestionTile 
                            key={index}
                            question={q}
                            onRemove={() => removeQuestion(index)}
                            onUpdate={(field, value) => updateQuestion(index, field, value)}
                        />
                    ))}

                    {questions.length === 0 && (
                        <NoContentTile text={t("no_specification_questions_yet")} />
                    )}
                </div>
                </FormSection>

                <Button type="submit" disabled={saving || !formValid} className="w-full">
                    {saving ? t("saving") : isEdit ? t("save") : t("create_activity")}
                </Button>
            </Form>
        </BorderedTile>
    </div>
  );
};