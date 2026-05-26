import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import {
  getMailinglists,
  getSettingsById,
  getStudies,
  type Mailinglist,
  type PostMemberDto,
  postMembers,
  postPaymentsMembership,
  type Study,
} from "~/api";
import i18n from "~/i18n";
import { appendErrorMessage } from "~/util/error.util";

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
    const response = await getStudies();

    if (response.error || !response.data) {
      throw response.error ?? new Error("Failed to fetch studies");
    }

    setStudies(response.data);
  } catch (error) {
    console.error("Failed to fetch studies", error);
    toast.error(appendErrorMessage(t("failed_to_load_studies"), error));
  }
};

/**
 * Fetches if master students should pay or not
 * @param {function} setMastersMustPay - State setter function for the mastersMustPay property.
 * @returns {Promise<void>}
 */
export const loadMastersMustPay = async (
  setMastersMustPay: (value: boolean) => void,
) => {
  try {
    const response = await getSettingsById({
      path: { id: "MastersShouldPayMembership" },
    });
    if (response.error || !response.data)
      throw (
        response.error ?? new Error("Failed to fetch masters must pay setting")
      );
    setMastersMustPay(response.data?.value === "1");
  } catch (error) {
    console.error("Failed to fetch masters must pay setting", error);
    toast.error(appendErrorMessage(t("failed_to_load_settings"), error));
  }
};

/**
 * Fetches the price of a membership.
 * @param {function} setPrice - State setter function for the price property.
 * @param {function} setMembershipPaymentExpirationTime - State setter function for the membershipPaymentExpirationTime property.
 * @returns {Promise<void>}
 */
export const loadPrice = async (
  setPrice: (value: number) => void,
  setMembershipPaymentExpirationTime: (value: number) => void,
) => {
  try {
    const priceResponse = await getSettingsById({
      path: { id: "MembershipPrice" },
    });
    const membershipPaymentExpirationTimeResponse = await getSettingsById({
      path: { id: "MembershipPaymentExpirationTime" },
    });
    if (
      priceResponse.data?.value === undefined ||
      membershipPaymentExpirationTimeResponse.data?.value === undefined
    )
      throw new Error("Failed to load data.");
    if (membershipPaymentExpirationTimeResponse.data.value.trim() !== "") {
      setMembershipPaymentExpirationTime(
        Number.parseInt(membershipPaymentExpirationTimeResponse.data.value, 10),
      );
    }
    setPrice(Number.parseFloat(priceResponse.data.value));
  } catch (error) {
    console.error("Failed to fetch masters must pay setting", error);
    toast.error(appendErrorMessage(t("failed_to_load_settings"), error));
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
    const response = await getMailinglists();

    if (response.error || !response.data) {
      throw response.error ?? new Error("Failed to fetch mailing lists");
    }

    setMailingLists(response.data);
  } catch (error) {
    console.error("Failed to fetch mailing lists", error);
    toast.error(appendErrorMessage(t("fetch_mailinglists_failed"), error));
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
  mastersMustPay: boolean | null;
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
  mastersMustPay,
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
        studentNumber: formData.studentNumber,
        parentPhoneNumber: formData.parentPhone || null,
        preferredLanguage: isDutch ? "NL" : "EN",
        mailSubscriptions: subscriptions,
        studyEnrollments: selectedStudies.map((id) => ({
          studyId: id,
          memberId: "00000000-0000-0000-0000-000000000000",
          enrollmentDate: new Date().toISOString(),
        })),
      };

      const response = await postMembers({ body: payload });

      if (response.status === 201 && response.data) {
        if (
          mastersMustPay ||
          !studies.some(
            (s) => selectedStudies.includes(s.id!) && s.type === "Master",
          )
        ) {
          const paymentResponse = await postPaymentsMembership({
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
          } else {
            throw (
              paymentResponse.error ?? new Error("Payment initiation failed")
            );
          }
        } else {
          navigate("/confirm-mail");
        }
      } else {
        throw response.error ?? new Error("Registration failed");
      }
    } catch (error) {
      console.error("Registration failed", error);
      throw error;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(registerProcess(), {
    loading: t("registering"),
    success: t("registration_successful"),
    error: (error) => appendErrorMessage(t("registration_failed"), error),
  });
};
