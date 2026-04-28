import type React from "react";
import type { NavigateFunction } from "react-router";
import { t } from "i18next";
import toast from "react-hot-toast";
import i18n from "~/i18n";
import { postApiMembers, postApiPaymentsMembership, getApiStudies, type PostMemberDto, type Study, getApiMailinglists, type Mailinglist } from "~/api";

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

export const loadStudies = async (
  setStudies: (studies: Study[]) => void
) => {
  try {
    const response = await getApiStudies();

    if(response.error || !response.data) throw new Error("Failed to fetch studies");

    setStudies(response.data);
  } catch (error) {
    console.error("Failed to fetch studies", error);
    toast.error(t("failed_to_load_studies"));
  } 
};

export const loadMailingLists = async (
  setMailingLists: (lists: Mailinglist[]) => void
) => {
  try {
    const response = await getApiMailinglists();

    if(response.error || !response.data) throw new Error("Failed to fetch mailing lists");

    setMailingLists(response.data);
  } catch (error) {
    console.error("Failed to fetch mailing lists", error);
    toast.error(t("fetch_mailinglists_failed"));
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

type RegisterSubmitArgs = {
  e: React.FormEvent;
  isFormValid: boolean;
  setLoading: (loading: boolean) => void;
  formData: RegisterFormData;
  selectedStudies: number[];
  subscriptions: number;
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
        mailSubscriptions: subscriptions,
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
      else{
        throw new Error("Registration failed");
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
