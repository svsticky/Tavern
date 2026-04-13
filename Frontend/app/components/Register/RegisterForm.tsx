import Tile from "../Tiles/Tile";
import Checkbox from "../UI/Checkbox";
import Input from "../UI/Input";
import { FormSection } from "../UI/Form/FormSection";
import { t } from "i18next";
import { useEffect, useState, useMemo } from "react";
import { getApiStudies, postApiMembers, postApiPaymentsMembership, type MailSubscriptions, type PostMemberDto, type Study } from "~/api";
import Button from "../UI/Button";
import { cn } from "~/util/tailwind.util";
import { useNavigate } from "react-router";
import i18n from "~/i18n";
import Form from "../UI/Form/Form";
import { mailSubscriptionMap } from "~/types/MailSubscriptionMap";
import toast from "react-hot-toast";

export default function RegisterForm({ className }: { className?: string }) {
    const [loading, setLoading] = useState(false);
    const [studies, setStudies] = useState<Study[]>([]);

    const navigate = useNavigate();
    
    const [formData, setFormData] = useState({
        firstname: "",
        lastname: "",
        email: "",
        birthDate: "",
        phone: "",
        parentPhone: "",
        street: "",
        houseNumber: "",
        postalCode: "",
        city: "",
        studentNumber: "",
    });

    const [selectedStudies, setSelectedStudies] = useState<number[]>([]);

    useEffect(() => {
        const fetchStudies = async () => {
            try{
                setLoading(true);
                const response = await getApiStudies();
                if (response.data) {
                    setStudies(response.data);
                }
            }
            catch(error){
                console.error("Failed to fetch studies", error);
                toast.error(t("failed_to_load_studies"));
            }
            finally{
                setLoading(false);
            };
        };
        fetchStudies();
    }, []);

    const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
        const { name, value } = e.target;
        setFormData((prev) => ({ ...prev, [name]: value }));
    };

    const handleStudyToggle = (id: number) => {
        setSelectedStudies((prev) =>
            prev.includes(id) ? prev.filter((s) => s !== id) : [...prev, id]
        );
    };

    const isFormValid = useMemo(() => {
        const birthDateValue = new Date(formData.birthDate);
        let isAdult = false;
        
        if (birthDateValue && !isNaN(birthDateValue.getTime())) {
            const today = new Date();
            let age = today.getFullYear() - birthDateValue.getFullYear();
            const monthDiff = today.getMonth() - birthDateValue.getMonth();
            
            if (monthDiff < 0 || (monthDiff === 0 && today.getDate() < birthDateValue.getDate())) {
                age--;
            }
            isAdult = age >= 18;
        }

        const allFieldsFilled = Object.entries(formData).every(([key, value]) => {
            if (key === "parentPhone" && isAdult) {
                return true; 
            }
            return value.trim() !== "";
        });

        const hasAtLeastOneStudy = selectedStudies.length > 0;

        return allFieldsFilled && hasAtLeastOneStudy;
    }, [formData, selectedStudies]);

    const [subscriptions, setSubscriptions] = useState({
        general: false,
        company: false,
        monday: false,
        lectures: false,
        teacher: false,
    });

    const handleSubscriptionChange = (key: keyof typeof subscriptions) => {
        setSubscriptions(prev => ({ ...prev, [key]: !prev[key] }));
    };

    const calculateMailSubscriptions = (): MailSubscriptions => {
        let total = 0;
        if (subscriptions.general) total |= 1;
        if (subscriptions.company) total |= 2;
        if (subscriptions.monday) total |= 4;
        if (subscriptions.lectures) total |= 8;
        if (subscriptions.teacher) total |= 16;
        return mailSubscriptionMap[total];
    };

    const handleSubmit = async (e: React.FormEvent) => {
        e.preventDefault();
        if (!isFormValid) return;

        setLoading(true);

        const isDutch = i18n.language.startsWith('nl');

        const registerProcess = async () => {
            try {
                const payload: PostMemberDto = {
                    firstName: formData.firstname,
                    lastName: formData.lastname,
                    email: formData.email,
                    phoneNumber: formData.phone,
                    dateOfBirth: new Date(formData.birthDate).toISOString(),
                    street: formData.street,
                    houseNumber: formData.houseNumber,
                    postalCode: formData.postalCode,
                    city: formData.city,
                    studentNumber: parseInt(formData.studentNumber),
                    parentPhoneNumber: formData.parentPhone || null,
                    preferredLanguage: isDutch ? "NL" : "EN",
                    mailSubscriptions: calculateMailSubscriptions(),
                    studyEnrollments: selectedStudies.map(id => ({
                        studyId: id,
                        memberId: "00000000-0000-0000-0000-000000000000",
                        enrollmentDate: new Date().toISOString(),
                    }))
                };

                var response = await postApiMembers({ body: payload });

                if(response.status === 201 && response.data) {
                    if(!studies.some(s => selectedStudies.includes(s.id!) && s.type === 'Master')) {
                        var paymentResponse = await postApiPaymentsMembership({
                            body: {
                                memberId: response.data.id,
                            }
                        });
                        
                        if(paymentResponse.status === 200 && paymentResponse.data) {
                            window.location.href = paymentResponse.data.checkoutUrl;
                        }
                    }
                    else{
                        navigate("/confirm-mail");
                    }
                }
            } catch (error) {
                console.error("Registratie mislukt", error);
                throw error;
            } finally {
                setLoading(false);
            }
        }

        toast.promise(registerProcess(), {
            loading: t("registering"),
            success: t("registration_successful"),
            error: t("registration_failed")
        });
    };

    return (
        <Tile className={cn("shadow-xl border border-gray-200 bg-white", className)}>
            <Form onSubmit={handleSubmit} >
                <FormSection title={t("personal_information")}>
                    <Input label={t("first_name")} name="firstname" value={formData.firstname} onChange={handleInputChange} disabled={loading} required />
                    <Input label={t("last_name")} name="lastname" value={formData.lastname} onChange={handleInputChange} disabled={loading} required />
                    <Input label={t("email")} name="email" type="email" value={formData.email} onChange={handleInputChange} disabled={loading} required />
                    <Input label={t("birth_date")} name="birthDate" type="date" value={formData.birthDate} onChange={handleInputChange} disabled={loading} required />
                    <Input label={t("phone")} name="phone" type="tel" value={formData.phone} onChange={handleInputChange} disabled={loading} required />
                    <Input label={t("parent_phone_number")} name="parentPhone" type="tel" value={formData.parentPhone} onChange={handleInputChange} disabled={loading} />
                    <Input label={t("street")} name="street" value={formData.street} onChange={handleInputChange} disabled={loading} required />
                    <Input label={t("house_number")} name="houseNumber" value={formData.houseNumber} onChange={handleInputChange} disabled={loading} required />
                    <Input label={t("postal_code")} name="postalCode" value={formData.postalCode} onChange={handleInputChange} disabled={loading} required />
                    <Input label={t("city")} name="city" value={formData.city} onChange={handleInputChange} disabled={loading} required />
                </FormSection>

                <FormSection title={t("study_information")}>
                    <Input label={t("student_number")} name="studentNumber" value={formData.studentNumber} onChange={handleInputChange} disabled={loading} required />
                    <div>
                        <span className="text-sm font-medium text-gray-700 block mb-2">{t("studies")}</span>
                        {loading ? (
                            t("loading")
                        ) : (
                            <div className="space-y-2">
                                {studies.map((study) => (
                                    <Checkbox 
                                        key={study.id}
                                        label={study.title} 
                                        checked={selectedStudies.includes(study.id!)}
                                        onChange={() => handleStudyToggle(study.id!)}
                                        disabled={loading}
                                    />
                                ))}
                            </div>
                        )}
                    </div>
                </FormSection>

                <FormSection title={t("mail_subscriptions")}>
                    <Checkbox label={t("general_member_meetings")} disabled={loading} checked={subscriptions.general} onChange={() => handleSubscriptionChange("general")} />
                    <Checkbox label={t("company_mails")} disabled={loading} checked={subscriptions.company} onChange={() => handleSubscriptionChange("company")} />
                    <Checkbox label={t("monday_morning_mails")} disabled={loading} checked={subscriptions.monday} onChange={() => handleSubscriptionChange("monday")} />
                    <Checkbox label={t("lectures_workshops")} disabled={loading} checked={subscriptions.lectures} onChange={() => handleSubscriptionChange("lectures")} />
                    <Checkbox label={t("teacher_mails")} disabled={loading} checked={subscriptions.teacher} onChange={() => handleSubscriptionChange("teacher")} />
                </FormSection>

                <Button 
                    type="submit" 
                    className={cn("w-full", !isFormValid && "opacity-50 cursor-not-allowed")} 
                    disabled={!isFormValid || loading}
                >
                    {t("become_member")}
                </Button>
            </Form>
        </Tile>
    );
}