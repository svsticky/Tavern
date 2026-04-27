import type React from "react";
import type { NavigateFunction } from "react-router";
import { t } from "i18next";
import toast from "react-hot-toast";
import i18n from "~/i18n";
import { postApiMembers, postApiPaymentsMembership, getApiStudies, type MailSubscriptions, type PostMemberDto, type Study } from "~/api";
import { mailSubscriptionMap } from "~/types/MailSubscriptionsMap";

export type RegisterFormData = {
  firstname: string;
  lastname: string;
  email: string;
  birthDate: string;
  phone: string;
  parentPhone: string;
  street: string;
  houseNumber: string;
  postalCode: string;
  city: string;
  studentNumber: string;
};

export type RegisterSubscriptions = {
  general: boolean;
  company: boolean;
  monday: boolean;
  lectures: boolean;
  teacher: boolean;
};

export const loadStudies = async (
  setLoading: (loading: boolean) => void,
  setStudies: (studies: Study[]) => void
) => {
  try {
    setLoading(true);
    const response = await getApiStudies();
    if (response.data) {
      setStudies(response.data);
    }
  } catch (error) {
    console.error("Failed to fetch studies", error);
    toast.error(t("failed_to_load_studies"));
  } finally {
    setLoading(false);
  }
};

export const handleRegisterInputChange = (
  e: React.ChangeEvent<HTMLInputElement>,
  setFormData: React.Dispatch<React.SetStateAction<RegisterFormData>>
) => {
  const { name, value } = e.target;
  setFormData((prev) => ({ ...prev, [name]: value }));
};

export const handleStudyToggle = (id: number, setSelectedStudies: React.Dispatch<React.SetStateAction<number[]>>) => {
  setSelectedStudies((prev) =>
    prev.includes(id) ? prev.filter((s) => s !== id) : [...prev, id]
  );
};

export const handleSubscriptionChange = (
  key: keyof RegisterSubscriptions,
  setSubscriptions: React.Dispatch<React.SetStateAction<RegisterSubscriptions>>
) => {
  setSubscriptions((prev) => ({ ...prev, [key]: !prev[key] }));
};

export const calculateMailSubscriptions = (subscriptions: RegisterSubscriptions): MailSubscriptions => {
  let total = 0;
  if (subscriptions.general) total |= 1;
  if (subscriptions.company) total |= 2;
  if (subscriptions.monday) total |= 4;
  if (subscriptions.lectures) total |= 8;
  if (subscriptions.teacher) total |= 16;
  return mailSubscriptionMap[total];
};

type RegisterSubmitArgs = {
  e: React.FormEvent;
  isFormValid: boolean;
  setLoading: (loading: boolean) => void;
  formData: RegisterFormData;
  selectedStudies: number[];
  subscriptions: RegisterSubscriptions;
  studies: Study[];
  navigate: NavigateFunction;
};

export const handleRegisterSubmit = async ({
  e,
  isFormValid,
  setLoading,
  formData,
  selectedStudies,
  subscriptions,
  studies,
  navigate
}: RegisterSubmitArgs) => {
  e.preventDefault();
  if (!isFormValid) return;

  setLoading(true);

  const isDutch = i18n.language.startsWith("nl");

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
        mailSubscriptions: calculateMailSubscriptions(subscriptions),
        studyEnrollments: selectedStudies.map((id) => ({
          studyId: id,
          memberId: "00000000-0000-0000-0000-000000000000",
          enrollmentDate: new Date().toISOString(),
        }))
      };

      const response = await postApiMembers({ body: payload });

      if (response.status === 201 && response.data) {
        if (!studies.some((s) => selectedStudies.includes(s.id!) && s.type === "Master")) {
          const paymentResponse = await postApiPaymentsMembership({
            body: {
              memberId: response.data.id,
            }
          });

          if (paymentResponse.status === 200 && paymentResponse.data && paymentResponse.data.checkoutUrl) {
            window.location.href = paymentResponse.data.checkoutUrl;
          }
        } else {
          navigate("/confirm-mail");
        }
      }
    } catch (error) {
      console.error("Registratie mislukt", error);
      throw error;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(registerProcess(), {
    loading: t("registering"),
    success: t("registration_successful"),
    error: t("registration_failed")
  });
};
