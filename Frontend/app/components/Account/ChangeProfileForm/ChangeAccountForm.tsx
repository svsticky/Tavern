import { t } from "i18next";
import { useEffect, useState } from "react";
import type { MemberResponseDto } from "~/api/types.gen";
import Tile from "~/components/Tiles/Tile";
import Button from "~/components/UI/Button";
import Checkbox from "~/components/UI/Checkbox";
import Form from "~/components/UI/Form/Form";
import { FormHeader } from "~/components/UI/Form/FormHeader";
import { FormSection } from "~/components/UI/Form/FormSection";
import Input from "~/components/UI/Input";
import { handleChangeEmail, handleChangePassword, handleSaveAccount, handleSubscriptionChange } from "./ChangeAccountForm.handlers";
import { useKeycloak } from "@react-keycloak/web";
import { type ChangeAccountFormData } from "./ChangeAccountForm.types";

enum MailSubscriptions {
  GeneralMemberMeetings = 1,
  CompanyMails = 2,
  MondayMorningMails = 4,
  LecturesAndWorkshops = 8,
  TeacherMails = 16
}

export default function ChangeAccountForm({member}: {member: MemberResponseDto}) {
    const {keycloak} = useKeycloak();
    const [saving, setSaving] = useState(false);

    const [formData, setFormData] = useState<ChangeAccountFormData>({
        phoneNumber: "",
        street: "",
        houseNumber: "",
        postalCode: "",
        city: "",
        parentPhoneNumber: "",
        preferredLanguage: "NL",
        mailSubscriptions: 0
    });

    const [isFormValid, setIsFormValid] = useState(false);

    useEffect(() => {
        const validateForm = () => {
        const { phoneNumber, street, houseNumber, postalCode, city, parentPhoneNumber, preferredLanguage } = formData;
        const basicFieldsFilled = !!(phoneNumber && street && houseNumber && postalCode && city && preferredLanguage !== undefined);

        let parentPhoneValid = true;
        if (member?.dateOfBirth) {
            const age = new Date().getFullYear() - new Date(member.dateOfBirth).getFullYear();
            if (age < 18) parentPhoneValid = !!parentPhoneNumber?.trim();
        }
        setIsFormValid(basicFieldsFilled && parentPhoneValid);
        };
        validateForm();
    }, [formData, member]);

    useEffect(() => {
        setFormData({
            phoneNumber: member.phoneNumber || "",
            street: member.street || "",
            houseNumber: member.houseNumber || "",
            postalCode: member.postalCode || "",
            city: member.city || "",
            parentPhoneNumber: member.parentPhoneNumber || "",
            preferredLanguage: member.preferredLanguage ?? "EN",
            mailSubscriptions: Number(member.mailSubscriptions) || 0
        });
    }, [member]);


    return (
        <Form className="w-full">
          
          <FormSection title={t("contact_details")}>
            <Input 
              label={t("phone_number")} required
              value={formData.phoneNumber} 
              onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, phoneNumber: e.target.value})} 
            />
            <Input 
              label={t("parent_phone_number")} 
              required={member?.dateOfBirth ? (new Date().getFullYear() - new Date(member.dateOfBirth).getFullYear() < 18) : false}
              value={formData.parentPhoneNumber} 
              onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, parentPhoneNumber: e.target.value})} 
            />
            <div className="flex items-end gap-4 w-full">
              <div className="flex-1">
                <Input 
                  label={t("email")} 
                  type="email" 
                  value={member.email!} 
                  disabled 
                  className="border-transparent bg-transparent p-0 cursor-default text-gray-900 disabled:text-gray-900"
                />
              </div>

              <div className="pb-1"> 
                <a 
                  onClick={() => handleChangeEmail(keycloak)} 
                >
                  {t("change_email")}
                </a>
              </div> 
            </div>
          </FormSection>

          <FormSection title={t("address")} columns={3}>
            <div className="md:col-span-2">
              <Input label={t("street")} required value={formData.street} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, street: e.target.value})} />
            </div>
            <Input label={t("house_number")} required value={formData.houseNumber} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, houseNumber: e.target.value})} />
            <Input label={t("postal_code")} required value={formData.postalCode} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, postalCode: e.target.value})} />
            <div className="md:col-span-2">
              <Input label={t("city")} required value={formData.city} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, city: e.target.value})} />
            </div>
          </FormSection>

          <section>
            <FormHeader title={t("mail_subscriptions")} />
            <Tile className="grid grid-cols-1 md:grid-cols-2 gap-4 p-5 bg-gray-50 border border-gray-100">
              <Checkbox 
                label={t("general_member_meetings")} 
                checked={(formData.mailSubscriptions & MailSubscriptions.GeneralMemberMeetings) !== 0}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleSubscriptionChange(MailSubscriptions.GeneralMemberMeetings, e.target.checked, setFormData)}
              />
              <Checkbox 
                label={t("company_mails")} 
                checked={(formData.mailSubscriptions & MailSubscriptions.CompanyMails) !== 0}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleSubscriptionChange(MailSubscriptions.CompanyMails, e.target.checked, setFormData)}
              />
              <Checkbox 
                label={t("monday_morning_mails")} 
                checked={(formData.mailSubscriptions & MailSubscriptions.MondayMorningMails) !== 0}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleSubscriptionChange(MailSubscriptions.MondayMorningMails, e.target.checked, setFormData  )}
              />
              <Checkbox 
                label={t("lectures_workshops")} 
                checked={(formData.mailSubscriptions & MailSubscriptions.LecturesAndWorkshops) !== 0}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleSubscriptionChange(MailSubscriptions.LecturesAndWorkshops, e.target.checked, setFormData)}
              />
              <Checkbox 
                label={t("teacher_mails")} 
                checked={(formData.mailSubscriptions & MailSubscriptions.TeacherMails) !== 0}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleSubscriptionChange(MailSubscriptions.TeacherMails, e.target.checked, setFormData)}
              />
            </Tile>
          </section>

          <FormSection columns={2}>
            <div>
              <FormHeader title={t("preferred_language")} border={false} />
              <div className="flex gap-2">
                <Button 
                  variant={formData.preferredLanguage === "NL" ? "primary" : "secondary"}
                  className="flex-1"
                  onClick={() => setFormData({...formData, preferredLanguage: "NL"})}
                >
                  {t("dutch")}
                </Button>
                <Button 
                  variant={formData.preferredLanguage === "EN" ? "primary" : "secondary"}
                  className="flex-1"
                  onClick={() => setFormData({...formData, preferredLanguage: "EN"})}
                >
                  {t("english")}
                </Button>
              </div>
            </div>
            <div>
              <FormHeader title={t("security")} border={false} />
              <Button variant="secondary" className="w-full" onClick={() => handleChangePassword(keycloak)}>
                {t("change_password")}
              </Button>
            </div>
          </FormSection>

          <Button 
            onClick={() => member.id && handleSaveAccount(member.id, formData, setSaving)}
            disabled={saving || !isFormValid}
            className="w-full"
          >
            {saving ? t("saving") : t("save")}
          </Button>
        </Form>
    )
}