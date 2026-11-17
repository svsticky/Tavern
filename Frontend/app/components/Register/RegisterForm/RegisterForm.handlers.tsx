import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import {
  getMailinglists,
  getRegistrationdocuments,
  getSettingsById,
  getStudies,
  type MailinglistDto,
  type PostMemberDto,
  postMembers,
  postPaymentsMembership,
  type RegistrationDocumentResponseDto,
  type Study,
} from "~/api";
import i18n from "~/i18n";
import { appendErrorMessage, getFriendlyErrorMessage } from "~/util/error.util";

/**
 * Data structure representing the registration form fields for a new member.
 * @typedef {Object} RegisterFormData
 * @property {string} firstname - The member's first name.
 * @property {string} lastname - The member's last name.
 * @property {string} email - The member's email address.
 * @property {string} birthDate - The member's date of birth.
 * @property {string} phone - The member's phone number.
 * @property {string} parentPhone - A parent's phone number, required for members under 18.
 * @property {string} street - The street of the member's address.
 * @property {string} houseNumber - The house number of the member's address.
 * @property {string} postalCode - The postal code of the member's address.
 * @property {string} city - The city of the member's address.
 * @property {string} studentNumber - The member's student number.
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
 * Fetches the list of available studies from the API.
 * @returns {Promise<Study[]>} The studies, or an empty list (with an error toast) on failure.
 */
export const loadStudies = async (): Promise<Study[]> => {
  try {
    const response = await getStudies();

    if (response.error || !response.data) {
      throw response.error ?? new Error("Failed to fetch studies");
    }

    return response.data;
  } catch (error) {
    console.error("Failed to fetch studies", error);
    toast.error(appendErrorMessage(t("failed_to_load_studies"), error));
    return [];
  }
};

/**
 * Fetches if master students should pay or not.
 * @returns {Promise<boolean | null>} The setting, or `null` (with an error toast) on failure.
 */
export const loadMastersMustPay = async (): Promise<boolean | null> => {
  try {
    const response = await getSettingsById({
      path: { id: "MastersShouldPayMembership" },
    });
    if (response.error || !response.data)
      throw (
        response.error ?? new Error("Failed to fetch masters must pay setting")
      );
    return response.data.value === "1";
  } catch (error) {
    console.error("Failed to fetch masters must pay setting", error);
    toast.error(appendErrorMessage(t("failed_to_load_settings"), error));
    return null;
  }
};

/**
 * The membership price settings.
 * @typedef {Object} MembershipPrice
 * @property {number | null} price - The price of a membership, or `null` when it failed to load.
 * @property {number | null} membershipPaymentExpirationTime - How long the payment link stays valid, or `null` when not set.
 */
export type MembershipPrice = {
  price: number | null;
  membershipPaymentExpirationTime: number | null;
};

/**
 * Fetches the price of a membership and how long its payment link stays valid.
 * @returns {Promise<MembershipPrice>} Both values, or `null` for each (with an error toast) on failure.
 */
export const loadPrice = async (): Promise<MembershipPrice> => {
  try {
    const [priceResponse, membershipPaymentExpirationTimeResponse] =
      await Promise.all([
        getSettingsById({ path: { id: "MembershipPrice" } }),
        getSettingsById({ path: { id: "MembershipPaymentExpirationTime" } }),
      ]);
    if (
      priceResponse.data?.value === undefined ||
      membershipPaymentExpirationTimeResponse.data?.value === undefined
    )
      throw new Error("Failed to load data.");

    const expirationTime = membershipPaymentExpirationTimeResponse.data.value;
    return {
      price: Number.parseFloat(priceResponse.data.value),
      membershipPaymentExpirationTime:
        expirationTime.trim() !== ""
          ? Number.parseInt(expirationTime, 10)
          : null,
    };
  } catch (error) {
    console.error("Failed to fetch membership price settings", error);
    toast.error(appendErrorMessage(t("failed_to_load_settings"), error));
    return { price: null, membershipPaymentExpirationTime: null };
  }
};

/**
 * Fetches the mailing lists a new member can subscribe to.
 * @returns {Promise<MailinglistDto[]>} The mailing lists, or an empty list (with an error toast) on failure.
 */
export const loadMailingLists = async (): Promise<MailinglistDto[]> => {
  try {
    const response = await getMailinglists();

    if (response.error || !response.data) {
      throw response.error ?? new Error("Failed to fetch mailing lists");
    }

    return response.data;
  } catch (error) {
    console.error("Failed to fetch mailing lists", error);
    toast.error(appendErrorMessage(t("fetch_mailinglists_failed"), error));
    return [];
  }
};

/**
 * Fetches the registration documents a new member has to agree to.
 * @returns {Promise<RegistrationDocumentResponseDto[]>} The documents sorted by `sortOrder`, or an empty list on failure.
 */
export const loadRegistrationDocuments = async (): Promise<
  RegistrationDocumentResponseDto[]
> => {
  try {
    const response = await getRegistrationdocuments();
    if (!response.data) return [];
    return [...response.data].sort((a, b) => a.sortOrder - b.sortOrder);
  } catch (error) {
    console.error("Failed to fetch registration documents", error);
    return [];
  }
};

/**
 * Fetches the configured study start dates (comma separated `MM-DD` values) from settings.
 * @returns {Promise<string | null>} The raw setting, or `null` when it isn't set or fails to load.
 */
export const loadStudyStartDates = async (): Promise<string | null> => {
  try {
    const response = await getSettingsById({
      path: { id: "StudyStartDates" },
    });
    return response.data?.value || null;
  } catch (error) {
    console.error("Failed to fetch study start dates", error);
    return null;
  }
};

/**
 * Everything the registration form needs, loaded by the register page's `clientLoader`.
 * Besides the fields below, it holds the `MembershipPrice` fields.
 * @typedef {Object} RegisterFormLoaderData
 * @property {Study[]} studies - The studies a new member can enroll in.
 * @property {boolean | null} mastersMustPay - Whether master students have to pay, or `null` when it failed to load.
 * @property {MailinglistDto[]} mailingLists - The mailing lists a new member can subscribe to.
 * @property {RegistrationDocumentResponseDto[]} documents - The documents a new member has to agree to.
 * @property {string | null} studyStartDates - The configured study start dates (comma separated `MM-DD` values), or `null` when not set.
 */
export type RegisterFormLoaderData = MembershipPrice & {
  studies: Study[];
  mastersMustPay: boolean | null;
  mailingLists: MailinglistDto[];
  documents: RegistrationDocumentResponseDto[];
  studyStartDates: string | null;
};

/**
 * Loads everything the registration form needs, for use in route loaders.
 * @returns {Promise<RegisterFormLoaderData>} The form's data.
 */
export const loadRegisterFormData =
  async (): Promise<RegisterFormLoaderData> => {
    const [
      studies,
      mastersMustPay,
      price,
      mailingLists,
      documents,
      studyStartDates,
    ] = await Promise.all([
      loadStudies(),
      loadMastersMustPay(),
      loadPrice(),
      loadMailingLists(),
      loadRegistrationDocuments(),
      loadStudyStartDates(),
    ]);

    return {
      studies,
      mastersMustPay,
      ...price,
      mailingLists,
      documents,
      studyStartDates,
    };
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
 * @property {React.FormEvent} e - The submit event of the registration form.
 * @property {boolean} isFormValid - Whether all required fields are filled in correctly.
 * @property {(loading: boolean) => void} setLoading - A function to update the loading state.
 * @property {RegisterFormData} formData - The filled-in registration fields.
 * @property {number[]} selectedStudies - The IDs of the selected studies.
 * @property {string} selectedStartDate - The selected study start date.
 * @property {string[]} subscribedMailinglistIds - The ids of the mailing lists to subscribe to.
 * @property {Study[]} studies - All available studies.
 * @property {NavigateFunction} navigate - The navigate function from react-router.
 * @property {boolean | null} mastersMustPay - Whether master students have to pay for a membership.
 */
type RegisterSubmitArgs = {
  e: React.FormEvent;
  isFormValid: boolean;
  setLoading: (loading: boolean) => void;
  formData: RegisterFormData;
  selectedStudies: number[];
  selectedStartDate: string;
  subscribedMailinglistIds: string[];
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
  selectedStartDate,
  subscribedMailinglistIds,
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
      const enrollmentDateIso = selectedStartDate
        ? new Date(selectedStartDate).toISOString()
        : new Date().toISOString();

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
        subscribedMailinglistIds,
        studyEnrollments: selectedStudies.map((id) => ({
          studyId: id,
          memberId: "00000000-0000-0000-0000-000000000000",
          enrollmentDate: enrollmentDateIso,
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
          navigate(`/confirm-mail?memberId=${response.data.id}`);
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
    error: (error) => getFriendlyErrorMessage(t("registration_failed"), error),
  });
};
