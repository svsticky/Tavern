import { t } from "i18next";
import type { Dispatch, SetStateAction } from "react";
import toast from "react-hot-toast";
import {
  type MemberResponseDto,
  patchMembersById,
  putMembersByIdMailinglists,
} from "~/api";
import type { IAuthService } from "~/auth/IAuthService";
import i18n from "~/i18n";
import { appendErrorMessage } from "~/util/error.util";
import type { ChangeAccountFormData } from "./ChangeAccountForm.types";

/**
 * Toggles a mailing list's membership in the set of subscribed list ids.
 * @param {string} id - The id of the mailing list.
 * @param {boolean} checked - Whether the checkbox is checked or not.
 * @param {(setter: SetStateAction<Set<string>>) => void} setSubscribedIds - A function to update the subscribed-ids state.
 */
export const handleSubscriptionToggle = (
  id: string,
  checked: boolean,
  setSubscribedIds: (setter: SetStateAction<Set<string>>) => void,
) => {
  setSubscribedIds((prev) => {
    const next = new Set(prev);
    if (checked) {
      next.add(id);
    } else {
      next.delete(id);
    }
    return next;
  });
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
 * Handles configuring multi-factor authentication (2FA) for a user.
 * @param {IAuthService} authService - The authentication service instance.
 */
export const handleConfigure2FA = async (authService: IAuthService) => {
  if (authService) {
    const url = await authService.configure2FA();
    window.location.href = url;
  } else {
    window.location.href = "/logout";
  }
};

/**
 * Saves changes to a user's account.
 * @param {string} userId - The ID of the user whose account is being saved.
 * @param {ChangeAccountFormData} formData - The form data containing the updated account details.
 * @param {string[]} subscribedMailinglistIds - The ids of the mailing lists the member should be subscribed to.
 * @param {(saving: boolean) => void} setSaving - A function to update the saving state.
 * @param {Dispatch<SetStateAction<MemberResponseDto | null>>} setMember - A function to update the member state.
 */
export const handleSaveAccount = async (
  userId: string,
  formData: ChangeAccountFormData,
  subscribedMailinglistIds: string[],
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
        ],
      });
      if (response.error) {
        throw response.error ?? new Error("Failed to save account changes");
      }

      const mailinglistsResponse = await putMembersByIdMailinglists({
        path: { id: userId },
        body: subscribedMailinglistIds,
      });
      if (mailinglistsResponse.error) {
        throw (
          mailinglistsResponse.error ??
          new Error("Failed to save mail subscriptions")
        );
      }

      await i18n.changeLanguage(
        formData.preferredLanguage === "NL" ? "nl" : "en",
      );

      setMember((prev) => (prev ? { ...prev, ...formData } : null));
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
