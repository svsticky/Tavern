import Tile from "../../Tiles/Tile";
import Checkbox from "../../UI/Checkbox";
import Input from "../../UI/Input";
import { FormSection } from "../../UI/Form/FormSection";
import { t } from "i18next";
import { useEffect, useState, useMemo } from "react";
import { type Mailinglist, type Study } from "~/api";
import Button from "../../UI/Button";
import { cn } from "~/util/tailwind.util";
import { useNavigate } from "react-router";
import Form from "../../UI/Form/Form";
import {
  handleRegisterInputChange,
  handleRegisterSubmit,
  handleStudyToggle,
  loadStudies,
  loadMailingLists
} from "./RegisterForm.handlers";

/**
 * The primary registration form component for new members.
 * 
 * This component manages the state for personal details, study selection, and mailing 
 * list subscriptions. It includes complex client-side validation, such as checking 
 * the user's age to determine if parent contact information is required.
 * 
 * @component
 * @param {Object} props - Component properties.
 * @param {string} [props.className] - Optional CSS classes for the outer Tile container.
 */
export default function RegisterForm({ className }: { className?: string }) {
    const [loading, setLoading] = useState(false);
    const [studies, setStudies] = useState<Study[]>([]);
    const [mailingLists, setMailingLists] = useState<Mailinglist[]>([]);

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
        setLoading(true);
        loadStudies(setStudies);
        loadMailingLists(setMailingLists);
        setLoading(false);
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

    const [subscriptions, setSubscriptions] = useState<number>(0);

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
                    {loading ? (
                        t("loading")
                    ) : (
                        <div className="space-y-2">
                            {mailingLists.map((list) => (
                                <Checkbox 
                                    key={list.id}
                                    label={list.name}
                                    disabled={loading}
                                    checked={(subscriptions & list.bitValue!) !== 0}
                                    onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
                                        const newValue = e.target.checked 
                                            ? subscriptions | list.bitValue! 
                                            : subscriptions & ~list.bitValue!;
                                        setSubscriptions(newValue);
                                    }}
                                />
                            ))}
                        </div>
                    )}
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
