import { useKeycloak } from "@react-keycloak/web";
import { t } from "i18next";
import { useEffect, useState, useRef } from "react";
import { 
  getApiMembersById, 
  getApiMembersByIdProfilePicture, 
  patchApiMembersById, 
  postApiProfilepictureByIdProfilePicture, 
  type MemberResponseDto
} from "~/api";
import Input from "~/components/UI/Input";
import Button from "~/components/UI/Button";
import Checkbox from "~/components/UI/Checkbox";
import i18n from "~/i18n";
import { PageHeader } from "~/components/UI/PageHeader";
import { FormSection } from "~/components/UI/Form/FormSection";
import { FormHeader } from "~/components/UI/Form/FormHeader";
import Tile from "~/components/Tiles/Tile";
import Form from "~/components/UI/Form/Form";
import toast from "react-hot-toast";

enum MailSubscriptions {
  GeneralMemberMeetings = 1,
  CompanyMails = 2,
  MondayMorningMails = 4,
  LecturesAndWorkshops = 8,
  TeacherMails = 16
}

export default function AccountPage() {
  const { keycloak } = useKeycloak();
  const userId = keycloak.tokenParsed?.UserId;

  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [member, setMember] = useState<MemberResponseDto | null>(null);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [profilePictureSrc, setProfilePictureSrc] = useState<string | null>(null);

  const [formData, setFormData] = useState({
    email: "",
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
      const { email, phoneNumber, street, houseNumber, postalCode, city, parentPhoneNumber, preferredLanguage } = formData;
      const basicFieldsFilled = !!(email && phoneNumber && street && houseNumber && postalCode && city && preferredLanguage !== undefined);
      const emailValid = /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);

      let parentPhoneValid = true;
      if (member?.dateOfBirth) {
        const age = new Date().getFullYear() - new Date(member.dateOfBirth).getFullYear();
        if (age < 18) parentPhoneValid = !!parentPhoneNumber?.trim();
      }
      setIsFormValid(basicFieldsFilled && emailValid && parentPhoneValid);
    };
    validateForm();
  }, [formData, member]);

  useEffect(() => {
    let url = null as string | null;
    async function loadUser() {
      if (!userId) return;
      try {
        const res = await getApiMembersById({ path: { id: userId } });
        if (res.data) {
          setMember(res.data);
          setFormData({
            email: res.data.email || "",
            phoneNumber: res.data.phoneNumber || "",
            street: res.data.street || "",
            houseNumber: res.data.houseNumber || "",
            postalCode: res.data.postalCode || "",
            city: res.data.city || "",
            parentPhoneNumber: res.data.parentPhoneNumber || "",
            preferredLanguage: res.data.preferredLanguage ?? "EN",
            mailSubscriptions: Number(res.data.mailSubscriptions) || 0
          });
        }

        const ppRes = await getApiMembersByIdProfilePicture({ path: { id: userId }, responseType: 'blob' });
        if (ppRes.data instanceof Blob && ppRes.status === 200) {
          url = URL.createObjectURL(ppRes.data);
          setProfilePictureSrc(url);
        } else {
          setProfilePictureSrc("/profile-picture.svg");
        }
      } catch (err) {
        console.error("Error while loading user data:", err);
        toast.error(t("loading_profile_failed"));
      } finally {
        setLoading(false);
      }
    }
    
    loadUser();
    
    return () => { if (url) URL.revokeObjectURL(url); };  
  }, [userId]);

  const handleSubscriptionChange = (flag: number, checked: boolean) => {
    setFormData(prev => ({
      ...prev,
      mailSubscriptions: checked ? prev.mailSubscriptions | flag : prev.mailSubscriptions & ~flag
    }));
  };

  const handleProfilePictureUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !userId) return;
    setSaving(true);
    
    const saveProcess = async () => {
      try {
        await postApiProfilepictureByIdProfilePicture({
          path: { id: userId },
          body: { image: file }
        });
        window.location.reload();
      } catch (err) {
        console.error("Failed to upload profile picture:", err);
        throw err;
      } finally {
        setSaving(false);
      }
    };

    toast.promise(saveProcess(), {
      loading: t("uploading_profile_picture"),
      success: t("upload_successful"),
      error: t("upload_failed")
    });
  }; 

  const handleChangePassword = async () => {
    if (keycloak) {
      const url = await keycloak.createLoginUrl({
        action: 'UPDATE_PASSWORD',
        redirectUri: window.location.href
      });
      window.location.href = url;
    }
    else{
      window.location.href = "/logout";
    }
  }; 

  const handleSaveAccount = async () => {
    if (!userId) return;
    setSaving(true);

    const saveProcess = async () => {
      try {
        await patchApiMembersById({
          path: { id: userId },
          body: [
            { op: "replace", path: "/email", value: formData.email },
            { op: "replace", path: "/phoneNumber", value: formData.phoneNumber },
            { op: "replace", path: "/street", value: formData.street },
            { op: "replace", path: "/houseNumber", value: formData.houseNumber },
            { op: "replace", path: "/postalCode", value: formData.postalCode },
            { op: "replace", path: "/city", value: formData.city },
            { op: "replace", path: "/parentPhoneNumber", value: formData.parentPhoneNumber },
            { op: "replace", path: "/preferredLanguage", value: formData.preferredLanguage },
            { op: "replace", path: "/mailSubscriptions", value: formData.mailSubscriptions }
          ]
        });
        i18n.changeLanguage(formData.preferredLanguage === "NL" ? "nl" : "en");
      } catch (err) {
        console.error("Error saving account:", err);
        throw err;
      } finally {
        setSaving(false);
      }
    };

    toast.promise(saveProcess(), {
      loading: t("saving"),
      success: t("save_successful"),
      error: t("save_failed")
    });
  };

  if (loading) return t("loading");

  if (!member) return t("failed_fetching");

  return (
    <>
      <PageHeader title={t("account")} />
      
      <div className="flex flex-col lg:flex-row gap-12">
        {/* Left: Profile Picture */}
        <div className="flex flex-col items-center lg:w-48">
          <div 
            className="relative w-40 h-40 group cursor-pointer"
            onClick={() => fileInputRef.current?.click()}
          >
            <div className="w-full h-full rounded-full overflow-hidden flex items-center justify-center bg-(--board-primary) shadow-md border-4 border-white transition-transform group-hover:scale-105">
              <img 
                src={profilePictureSrc || "/profile-picture.svg"} 
                className={profilePictureSrc && profilePictureSrc !== "/profile-picture.svg" ? "w-full h-full object-cover" : "w-2/3 h-2/3 opacity-80"}
                alt="Profile"
              />
            </div>
            <div className="absolute inset-0 flex items-center justify-center bg-black/40 text-white rounded-full opacity-0 group-hover:opacity-100 transition-opacity text-xs font-bold uppercase">
              {t("change")}
            </div>
          </div>
          <input 
            type="file" 
            ref={fileInputRef} 
            hidden 
            accept="image/*" 
            onChange={handleProfilePictureUpload} 
          />
          
          <div className="mt-6 text-center">
            <h2 className="font-bold text-xl">{member?.firstName} {member?.lastName}</h2>
            <p className="text-gray-500 font-mono text-sm">{member?.studentNumber}</p>
          </div>
        </div>

        {/* Right: Forms */}
        <Form className="w-full">
          
          <FormSection title={t("contact_details")}>
            <Input 
              label={t("email")} 
              type="email" required
              value={formData.email} 
              onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, email: e.target.value})} 
            />
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
                onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleSubscriptionChange(MailSubscriptions.GeneralMemberMeetings, e.target.checked)}
              />
              <Checkbox 
                label={t("company_mails")} 
                checked={(formData.mailSubscriptions & MailSubscriptions.CompanyMails) !== 0}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleSubscriptionChange(MailSubscriptions.CompanyMails, e.target.checked)}
              />
              <Checkbox 
                label={t("monday_morning_mails")} 
                checked={(formData.mailSubscriptions & MailSubscriptions.MondayMorningMails) !== 0}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleSubscriptionChange(MailSubscriptions.MondayMorningMails, e.target.checked)}
              />
              <Checkbox 
                label={t("lectures_workshops")} 
                checked={(formData.mailSubscriptions & MailSubscriptions.LecturesAndWorkshops) !== 0}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleSubscriptionChange(MailSubscriptions.LecturesAndWorkshops, e.target.checked)}
              />
              <Checkbox 
                label={t("teacher_mails")} 
                checked={(formData.mailSubscriptions & MailSubscriptions.TeacherMails) !== 0}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) => handleSubscriptionChange(MailSubscriptions.TeacherMails, e.target.checked)}
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
              <Button variant="secondary" className="w-full" onClick={handleChangePassword}>
                {t("change_password")}
              </Button>
            </div>
          </FormSection>

          <Button 
            onClick={handleSaveAccount} 
            disabled={saving || !isFormValid}
            className="w-full"
          >
            {saving ? t("saving") : t("save")}
          </Button>
        </Form>
      </div>
    </>
  );
}