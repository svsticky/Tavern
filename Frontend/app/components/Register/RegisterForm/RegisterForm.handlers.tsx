import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import {
  getApiMailinglists,
  getApiStudies,
  type Mailinglist,
  type PostMemberDto,
  postApiMembers,
  postApiPaymentsMembership,
  type Study,
} from "~/api";
import i18n from "~/i18n";

/**
 * Data structure representing the registration form fields for a new member.
 * @typedef {Object} RegisterFormData
 */
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

/**
 * Fetches the list of available studies from the API and updates the local state.
 * @param {function} setStudies - State setter function for the studies array.
 * @returns {Promise<void>}
 */
export const loadStudies = async (setStudies: (studies: Study[]) => void) => {
  try {
    const response = await getApiStudies();

    if (response.error || !response.data)
      throw new Error("Failed to fetch studies");

    setStudies(response.data);
  } catch (error) {
    console.error("Failed to fetch studies", error);
    toast.error(t("failed_to_load_studies"));
  }
};

/**
 * Fetches available mailing lists from the API and updates the local state.
 * @param {function} setMailingLists - State setter function for the mailing lists array.
 * @returns {Promise<void>}
 */
export const loadMailingLists = async (
  setMailingLists: (lists: Mailinglist[]) => void,
) => {
  try {
    const response = await getApiMailinglists();

    if (response.error || !response.data)
      throw new Error("Failed to fetch mailing lists");

    setMailingLists(response.data);
  } catch (error) {
    console.error("Failed to fetch mailing lists", error);
    toast.error(t("fetch_mailinglists_failed"));
  }
};

/**
 * Handles updates for standard text and date input fields in the registration form.
 * @param {React.ChangeEvent<HTMLInputElement>} e - The change event from the input.
 * @param {React.Dispatch<React.SetStateAction<RegisterFormData>>} setFormData - State setter for form data.
 */
export const handleRegisterInputChange = (
  e: React.ChangeEvent<HTMLInputElement>,
  setFormData: React.Dispatch<React.SetStateAction<RegisterFormData>>,
) => {
  const { name, value } = e.target;
  setFormData((prev) => ({ ...prev, [name]: value }));
};

/**
 * Toggles a study ID within the selected studies selection array.
 * @param {number} id - The ID of the study to toggle.
 * @param {React.Dispatch<React.SetStateAction<number[]>>} setSelectedStudies - State setter for selected study IDs.
 */
export const handleStudyToggle = (
  id: number,
  setSelectedStudies: React.Dispatch<React.SetStateAction<number[]>>,
) => {
  setSelectedStudies((prev) =>
    prev.includes(id) ? prev.filter((s) => s !== id) : [...prev, id],
  );
};

/**
 * Arguments for the handleRegisterSubmit function.
 * @typedef {Object} RegisterSubmitArgs
 */
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

/**
 * Orchestrates the member registration process, including API submission,
 * toast notifications, and conditional redirection to payment or confirmation.
 *
 * @param {RegisterSubmitArgs} args - Configuration and state objects required for submission.
 * @returns {Promise<void>}
 */
export const handleRegisterSubmit = async ({
  e,
  isFormValid,
  setLoading,
  formData,
  selectedStudies,
  subscriptions,
  studies,
  navigate,
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
        studentNumber: parseInt(formData.studentNumber, 10),
        parentPhoneNumber: formData.parentPhone || null,
        preferredLanguage: isDutch ? "NL" : "EN",
        mailSubscriptions: subscriptions,
        studyEnrollments: selectedStudies.map((id) => ({
          studyId: id,
          memberId: "00000000-0000-0000-0000-000000000000",
          enrollmentDate: new Date().toISOString(),
        })),
      };

      const response = await postApiMembers({ body: payload });

      if (response.status === 201 && response.data) {
        if (
          !studies.some(
            (s) => selectedStudies.includes(s.id!) && s.type === "Master",
          )
        ) {
          const paymentResponse = await postApiPaymentsMembership({
            body: {
              memberId: response.data.id,
            },
          });

          if (
            paymentResponse.status === 200 &&
            paymentResponse.data &&
            paymentResponse.data.checkoutUrl
          ) {
            window.location.href = paymentResponse.data.checkoutUrl;
          }
        } else {
          navigate("/confirm-mail");
        }
      } else {
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
    error: t("registration_failed"),
  });
};
