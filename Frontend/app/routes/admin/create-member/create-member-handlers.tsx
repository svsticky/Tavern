import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import type { NavigateFunction } from "react-router";
import { type Language, type PostMemberDto, postMembers } from "~/api";
import { appendErrorMessage } from "~/util/error.util";

/**
 * Data structure representing the registration form fields for a new begunstiger.
 * @typedef {Object} CreateBegunstigerFormData
 */
export type CreateBegunstigerFormData = {
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
  language: Language;
  isBegunstiger: boolean;
};

/**
 * Handles updates for standard text and date input fields in the registration form.
 * @param {React.ChangeEvent<HTMLInputElement>} e - The change event from the input.
 * @param {React.Dispatch<React.SetStateAction<CreateBegunstigerFormData>>} setFormData - State setter for form data.
 */
export const handleCreateBegunstigerInputChange = (
  e: React.ChangeEvent<HTMLInputElement | HTMLSelectElement>,
  setFormData: React.Dispatch<React.SetStateAction<CreateBegunstigerFormData>>,
) => {
  const { name, value, type } = e.target;
  const val =
    type === "checkbox" ? (e.target as HTMLInputElement).checked : value;
  setFormData((prev) => ({ ...prev, [name]: val }));
};

/**
 * Arguments for the handleCreateSubmit function.
 * @typedef {Object} RegisterSubmitArgs
 */
type CreateSubmitArgs = {
  e: React.FormEvent;
  isFormValid: boolean;
  setLoading: (loading: boolean) => void;
  formData: CreateBegunstigerFormData;
  navigate: NavigateFunction;
};

/**
 * Orchestrates the member registration process, including API submission,
 * toast notifications, and conditional redirection to payment or confirmation.
 *
 * @param {CreateSubmitArgs} args - Configuration and state objects required for submission.
 * @returns {Promise<void>}
 */
export const handleCreateSubmit = async ({
  e,
  isFormValid,
  setLoading,
  formData,
  navigate,
}: CreateSubmitArgs) => {
  e.preventDefault();
  if (!isFormValid) return;

  setLoading(true);

  const registerProcess = async () => {
    try {
      const studentNum = formData.studentNumber.trim();
      const finalStudentNumber = formData.isBegunstiger
        ? studentNum.startsWith("F_")
          ? studentNum
          : `F_${studentNum}`
        : studentNum;

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
        studentNumber: finalStudentNumber,
        parentPhoneNumber: formData.parentPhone || null,
        preferredLanguage: formData.language,
        studyEnrollments: [],
        begunstiger: formData.isBegunstiger,
      };

      const response = await postMembers({ body: payload });

      if (response.status === 201 && response.data) {
        navigate("/confirm-mail");
      } else {
        throw response.error ?? new Error("Registration failed");
      }
    } catch (error) {
      console.error("Registration failed:", error);
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
