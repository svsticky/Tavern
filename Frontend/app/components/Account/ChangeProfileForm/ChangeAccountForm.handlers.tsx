import { t } from "i18next";
import type { Dispatch, SetStateAction } from "react";
import toast from "react-hot-toast";
import {
  getMembersByIdMailinglists,
  type MemberMailinglistDto,
  type MemberResponseDto,
  patchMembersById,
  putMembersByIdMailinglists,
} from "~/api";
import type { IAuthService } from "~/auth/IAuthService";
import i18n from "~/i18n";
import { appendErrorMessage } from "~/util/error.util";
import type { ChangeAccountFormData } from "./ChangeAccountForm.types";

/**
 * A member's mailing lists, as loaded by `loadMemberMailinglists`.
 * @typedef {Object} MemberMailinglistsLoaderData
 * @property {MemberMailinglistDto[]} mailingLists - The member's mailing lists, with whether they're subscribed.
 * @property {boolean} mailingListsUnavailable - Whether loading the mailing lists failed.
 */
export type MemberMailinglistsLoaderData = {
  mailingLists: MemberMailinglistDto[];
  mailingListsUnavailable: boolean;
};

/**
 * Loads a member's mailing lists, including the yearly-renewal ones, for use in route loaders.
 * A failure doesn't fail the page: it shows a toast and reports the lists as unavailable.
 * @param {string} memberId - The ID of the member whose mailing lists are loaded.
 * @returns {Promise<MemberMailinglistsLoaderData>} The mailing lists, or none and `mailingListsUnavailable` on failure.
 */
export const loadMemberMailinglists = async (
  memberId: string,
): Promise<MemberMailinglistsLoaderData> => {
  try {
    const response = await getMembersByIdMailinglists({
      path: { id: memberId },
      query: { includeYearlyRenewal: true },
    });

    if (response.error || !response.data) {
      throw response.error ?? new Error("Failed to fetch mailing lists");
    }

    return { mailingLists: response.data, mailingListsUnavailable: false };
  } catch (error) {
    console.error("Error fetching mailing lists:", error);
    toast.error(appendErrorMessage(t("fetch_mailinglists_failed"), error));
    return { mailingLists: [], mailingListsUnavailable: true };
  }
};

/**
 * Returns the ids of the mailing lists the member is subscribed to.
 * @param {MemberMailinglistDto[]} mailingLists - The member's mailing lists.
 * @returns {Set<string>} The ids of the subscribed lists.
 */
export const getSubscribedMailinglistIds = (
  mailingLists: MemberMailinglistDto[],
): Set<string> =>
  new Set(
    mailingLists.filter((list) => list.subscribed).map((list) => list.id!),
  );

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
        query: { includeYearlyRenewal: true },
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
