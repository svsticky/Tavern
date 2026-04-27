import Tile from "../../Tiles/Tile";
import Checkbox from "../../UI/Checkbox";
import Input from "../../UI/Input";
import { FormSection } from "../../UI/Form/FormSection";
import { t } from "i18next";
import { useEffect, useState, useMemo } from "react";
import { type Study } from "~/api";
import Button from "../../UI/Button";
import { cn } from "~/util/tailwind.util";
import { useNavigate } from "react-router";
import Form from "../../UI/Form/Form";
import {
  handleRegisterInputChange,
  handleRegisterSubmit,
  handleStudyToggle,
  handleSubscriptionChange,
  type RegisterSubscriptions,
  loadStudies
} from "./RegisterForm.handlers";

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
        loadStudies(setLoading, setStudies);
    }, []);

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

    const [subscriptions, setSubscriptions] = useState<RegisterSubscriptions>({
        general: false,
        company: false,
        monday: false,
        lectures: false,
        teacher: false,
    });

    return (
        <Tile className={cn("shadow-xl border border-gray-200 bg-white", className)}>
            <Form
              onSubmit={(e) =>
                handleRegisterSubmit({
                  e,
                  isFormValid,
                  setLoading,
                  formData,
                  selectedStudies,
                  subscriptions,
                  studies,
                  navigate
                })
              }
            >
                <FormSection title={t("personal_information")}>
                    <Input label={t("first_name")} name="firstname" value={formData.firstname} onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleRegisterInputChange(e, setFormData)} disabled={loading} required />
                    <Input label={t("last_name")} name="lastname" value={formData.lastname} onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleRegisterInputChange(e, setFormData)} disabled={loading} required />
                    <Input label={t("email")} name="email" type="email" value={formData.email} onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleRegisterInputChange(e, setFormData)} disabled={loading} required />
                    <Input label={t("birth_date")} name="birthDate" type="date" value={formData.birthDate} onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleRegisterInputChange(e, setFormData)} disabled={loading} required />
                    <Input label={t("phone")} name="phone" type="tel" value={formData.phone} onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleRegisterInputChange(e, setFormData)} disabled={loading} required />
                    <Input label={t("parent_phone_number")} name="parentPhone" type="tel" value={formData.parentPhone} onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleRegisterInputChange(e, setFormData)} disabled={loading} />
                    <Input label={t("street")} name="street" value={formData.street} onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleRegisterInputChange(e, setFormData)} disabled={loading} required />
                    <Input label={t("house_number")} name="houseNumber" value={formData.houseNumber} onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleRegisterInputChange(e, setFormData)} disabled={loading} required />
                    <Input label={t("postal_code")} name="postalCode" value={formData.postalCode} onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleRegisterInputChange(e, setFormData)} disabled={loading} required />
                    <Input label={t("city")} name="city" value={formData.city} onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleRegisterInputChange(e, setFormData)} disabled={loading} required />
                </FormSection>

                <FormSection title={t("study_information")}>
                    <Input label={t("student_number")} name="studentNumber" value={formData.studentNumber} onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleRegisterInputChange(e, setFormData)} disabled={loading} required />
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
                                        onChange={() => handleStudyToggle(study.id!, setSelectedStudies)}
                                        disabled={loading}
                                    />
                                ))}
                            </div>
                        )}
                    </div>
                </FormSection>

                <FormSection title={t("mail_subscriptions")}>
                    <Checkbox label={t("general_member_meetings")} disabled={loading} checked={subscriptions.general} onChange={() => handleSubscriptionChange("general", setSubscriptions)} />
                    <Checkbox label={t("company_mails")} disabled={loading} checked={subscriptions.company} onChange={() => handleSubscriptionChange("company", setSubscriptions)} />
                    <Checkbox label={t("monday_morning_mails")} disabled={loading} checked={subscriptions.monday} onChange={() => handleSubscriptionChange("monday", setSubscriptions)} />
                    <Checkbox label={t("lectures_workshops")} disabled={loading} checked={subscriptions.lectures} onChange={() => handleSubscriptionChange("lectures", setSubscriptions)} />
                    <Checkbox label={t("teacher_mails")} disabled={loading} checked={subscriptions.teacher} onChange={() => handleSubscriptionChange("teacher", setSubscriptions)} />
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
