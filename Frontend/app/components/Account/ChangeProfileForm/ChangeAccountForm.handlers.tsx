import { t } from "i18next";
import type { Dispatch, SetStateAction } from "react";
import toast from "react-hot-toast";
import { type MemberResponseDto, patchMembersById } from "~/api";
import type { IAuthService } from "~/auth/IAuthService";
import i18n from "~/i18n";
import { appendErrorMessage } from "~/util/error.util";
import type { ChangeAccountFormData } from "./ChangeAccountForm.types";

/**
 * Handles the change in subscription status for a mailing list.
 * @param {number} flag - The bit value representing the mailing list.
 * @param {boolean} checked - Whether the checkbox is checked or not.
 * @param {(formData: SetStateAction<ChangeAccountFormData>) => void} setFormData - A function to update the form data state.
 */
export const handleSubscriptionChange = (
  flag: number,
  checked: boolean,
  setFormData: (formData: SetStateAction<ChangeAccountFormData>) => void,
) => {
  setFormData((prev) => ({
    ...prev,
    mailSubscriptions: checked
      ? prev.mailSubscriptions | flag
      : prev.mailSubscriptions & ~flag,
  }));
};

/**
 * Handles changing the password for a user.
 * @param {IAuthService} authService - The authentication service instance.
 */
export const handleChangePassword = async (authService: IAuthService) => {
  if (authService) {
    const url = await authService.getUpdatePasswordUrl();
    window.location.href = url;
  } else {
    window.location.href = "/logout";
  }
};

/**
 * Handles changing the email for a user.
 * @param {IAuthService} authService - The authentication service instance.
 */
export const handleChangeEmail = async (authService: IAuthService) => {
  if (authService) {
    const url = await authService.getUpdateEmailUrl();
    window.location.href = url;
  } else {
    window.location.href = "/logout";
  }
};

/**
 * Handles configuring multi-factor authentication (MFA) for a user.
 * @param {IAuthService} authService - The authentication service instance.
 */
export const handleConfigureMFA = async (authService: IAuthService) => {
  if (authService) {
    const url = await authService.configureMFA();
    window.location.href = url;
  } else {
    window.location.href = "/logout";
  }
};

/**
 * Saves changes to a user's account.
 * @param {string} userId - The ID of the user whose account is being saved.
 * @param {ChangeAccountFormData} formData - The form data containing the updated account details.
 * @param {(saving: boolean) => void} setSaving - A function to update the saving state.
 * @param {Dispatch<SetStateAction<MemberResponseDto | null>>} setMember - A function to update the member state.
 */
export const handleSaveAccount = async (
  userId: string,
  formData: ChangeAccountFormData,

  setSaving: (saving: boolean) => void,
  setMember: Dispatch<SetStateAction<MemberResponseDto | null>>,
) => {
  setSaving(true);

  const saveProcess = async () => {
    try {
      const response = await patchMembersById({
        path: { id: userId },
        body: [
          { op: "replace", path: "/phoneNumber", value: formData.phoneNumber },
          { op: "replace", path: "/street", value: formData.street },
          { op: "replace", path: "/houseNumber", value: formData.houseNumber },
          { op: "replace", path: "/postalCode", value: formData.postalCode },
          { op: "replace", path: "/city", value: formData.city },
          {
            op: "replace",
            path: "/parentPhoneNumber",
            value: formData.parentPhoneNumber,
          },
          {
            op: "replace",
            path: "/preferredLanguage",
            value: formData.preferredLanguage,
          },
          {
            op: "replace",
            path: "/mailSubscriptions",
            value: formData.mailSubscriptions,
          },
        ],
      });
      if (response.error) {
        throw response.error ?? new Error("Failed to save account changes");
      }

      await i18n.changeLanguage(
        formData.preferredLanguage === "NL" ? "nl" : "en",
      );

      setMember((prev) =>
        prev
          ? {
              ...prev,
              ...formData,
              mailSubscriptions: formData.mailSubscriptions,
            }
          : null,
      );
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
    error: (error) => appendErrorMessage(t("save_failed"), error),
  });
};
