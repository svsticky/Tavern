import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import {
  deleteMembersById,
  deleteStudyenrollmentsById,
  type GroupMembershipResponseDto,
  getGroupmemberships,
  getMembersById,
  getPaymentsMemberByFromUserIdStatus,
  getStudies,
  getStudyenrollments,
  patchMembersById,
  patchStudyenrollmentsById,
  postPaymentsBegunstiger,
  postPaymentsMembership,
  postStudyenrollments,
  type Study,
  type StudyEnrollmentResponseDto,
  type StudyStatus,
} from "~/api";
import { appendErrorMessage } from "~/util/error.util";

/**
 * Interface representing the comprehensive form state for editing a member.
 */
export type EditMemberFormData = {
  firstName: string;
  lastName: string;
  studentNumber: string;
  phoneNumber: string;
  street: string;
  houseNumber: string;
  postalCode: string;
  city: string;
  parentPhoneNumber: string;
  preferredLanguage: string;
  notes: string;
  gratie: boolean;
  lidVanVerdienste: boolean;
  ereLid: boolean;
  begunstiger: boolean;
  suspended: boolean;
  dateOfBirth: string;
};

/** Everything the edit-member page shows, as loaded by the route's `clientLoader`. */
export type MemberPageData = {
  formData: EditMemberFormData;
  email: string;
  enrollments: StudyEnrollmentResponseDto[];
  groupMemberships: GroupMembershipResponseDto[];
  availableStudies: Study[];
  hasPaidMembership: boolean;
  isBegunstiger: boolean;
};

/**
 * Fetches the member profile, study enrollments, group memberships (all
 * years), the available study programs and the membership/begunstiger payment
 * status - in parallel, since none depends on another.
 *
 * Throws when any of the first four fail, so React Router's error boundary
 * handles it. The payment status is non-critical: it falls back to assuming
 * the fee is paid (hiding the manual mark-as-paid action) rather than failing
 * the whole page. `hasPaidMembershipBeforeExpirationTime` already reflects the
 * begunstiger fee status instead of the regular membership one when
 * `isBegunstiger`.
 */
export const fetchMemberPageData = async (
  memberId: string,
): Promise<MemberPageData> => {
  const [
    memberResponse,
    studyEnrollmentsResponse,
    // No MembershipYear filter - all years, same as the home page's "my groups" overview.
    groupMembershipsResponse,
    studiesResponse,
    paymentStatusResponse,
  ] = await Promise.all([
    getMembersById({ path: { id: memberId } }),
    getStudyenrollments({ query: { MemberId: memberId } }),
    getGroupmemberships({ query: { MemberId: memberId } }),
    getStudies(),
    getPaymentsMemberByFromUserIdStatus({ path: { fromUserId: memberId } }),
  ]);

  if (memberResponse.error || !memberResponse.data) {
    throw memberResponse.error ?? new Error("Failed to load member data");
  }
  if (studyEnrollmentsResponse.error || !studyEnrollmentsResponse.data) {
    throw (
      studyEnrollmentsResponse.error ??
      new Error("Failed to load study enrollments")
    );
  }
  if (groupMembershipsResponse.error || !groupMembershipsResponse.data) {
    throw (
      groupMembershipsResponse.error ??
      new Error("Failed to load group memberships")
    );
  }
  if (studiesResponse.error || !studiesResponse.data) {
    throw (
      studiesResponse.error ?? new Error("Failed to load available studies")
    );
  }

  const member = memberResponse.data;
  return {
    formData: {
      firstName: member.firstName || "",
      lastName: member.lastName || "",
      studentNumber: member.studentNumber || "",
      phoneNumber: member.phoneNumber || "",
      street: member.street || "",
      houseNumber: member.houseNumber || "",
      postalCode: member.postalCode || "",
      city: member.city || "",
      parentPhoneNumber: member.parentPhoneNumber || "",
      preferredLanguage: member.preferredLanguage ?? "NL",
      notes: member.notes || "",
      gratie: !!member.gratie,
      lidVanVerdienste: !!member.lidVanVerdienste,
      ereLid: !!member.ereLid,
      begunstiger: !!member.begunstiger,
      suspended: !!member.suspended,
      dateOfBirth: member.dateOfBirth
        ? new Date(member.dateOfBirth).toISOString().split("T")[0]
        : "",
    },
    email: member.email ?? "",
    enrollments: studyEnrollmentsResponse.data,
    groupMemberships: groupMembershipsResponse.data,
    availableStudies: studiesResponse.data,
    hasPaidMembership:
      paymentStatusResponse.data?.hasPaidMembershipBeforeExpirationTime ?? true,
    isBegunstiger: paymentStatusResponse.data?.isBegunstiger ?? false,
  };
};

/**
 * Updates member profile information using JSON Patch based on the current form state.
 *
 * @async
 * @param {string | undefined} memberId - The ID of the member to update.
 * @param {EditMemberFormData} formData - The structured data containing modified profile fields.
 * @param {Function} setSaving - React state setter to track the submission progress.
 */
export const handleSaveMember = async (
  memberId: string | undefined,
  formData: EditMemberFormData,
  setSaving: (saving: boolean) => void,
) => {
  if (!memberId) return;

  if (
    !formData.studentNumber.trim() ||
    !formData.postalCode.trim() ||
    !formData.city.trim()
  ) {
    toast.error(t("please_fill_all_fields"));
    return;
  }

  const saveProcess = async () => {
    try {
      setSaving(true);

      const patchDoc = Object.keys(formData).map((key) => ({
        op: "replace",
        path: `/${key}`,
        value: formData[key as keyof typeof formData],
      }));

      const response = await patchMembersById({
        path: { id: memberId },
        body: patchDoc as any,
      });

      if (response.error) {
        throw response.error ?? new Error("Failed to save member data");
      }
    } catch (err) {
      console.error("Failed to save member data:", err);
      throw err;
    } finally {
      setSaving(false);
    }
  };

  toast
    .promise(saveProcess(), {
      loading: t("saving"),
      success: t("save_success"),
      error: (error) => appendErrorMessage(t("save_error"), error),
    })
    .finally(() => setSaving(false));
};

/**
 * Deletes and anonymizes a member account.
 *
 * @async
 * @param {string | undefined} memberId - The ID of the member to delete.
 * @param {Function} setLoading - State setter to track loading state.
 * @param {Function} onSuccess - Callback invoked on successful deletion.
 */
export const handleDeleteMember = async (
  memberId: string | undefined,
  setLoading: (loading: boolean) => void,
  onSuccess: () => void,
) => {
  if (!memberId) return;

  const deleteProcess = async () => {
    try {
      setLoading(true);
      const response = await deleteMembersById({ path: { id: memberId } });

      if (response.error) {
        throw response.error ?? new Error("Failed to delete member");
      }

      onSuccess();
    } catch (err) {
      console.error("Failed to delete member:", err);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(deleteProcess(), {
    loading: t("deleting"),
    success: t("delete_success"),
    error: (error) => appendErrorMessage(t("delete_error"), error),
  });
};

/**
 * Manually marks a member's membership fee as paid (e.g., if the member paid in cash).
 *
 * @async
 * @param {string | undefined} memberId - The ID of the member whose membership payment is recorded.
 * @param {Function} setLoading - State setter to track the request.
 * @param {Function} onSuccess - Callback invoked once the payment has been recorded.
 */
export const handleMarkMembershipAsPaid = async (
  memberId: string | undefined,
  setLoading: (loading: boolean) => void,
  onSuccess: () => void,
) => {
  if (!memberId) return;

  const process = async () => {
    try {
      setLoading(true);
      const response = await postPaymentsMembership({
        body: {
          memberId,
          manuallyMarkedAsPaid: true,
        },
      });

      if (response.error) {
        throw response.error ?? new Error("Failed to mark membership as paid");
      }

      onSuccess();
    } catch (err) {
      console.error("Failed to mark membership as paid:", err);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(process(), {
    loading: t("marking_as_paid"),
    success: t("marked_as_paid"),
    error: (error) => appendErrorMessage(t("mark_as_paid_failed"), error),
  });
};

/**
 * Manually marks a member's "Begunstiger" (benefactor) fee as paid (e.g., if they paid in cash).
 * Posts to the dedicated begunstiger payment endpoint rather than the membership one, since
 * begunstigers pay their own separate fee.
 *
 * @async
 * @param {string | undefined} memberId - The ID of the member whose begunstiger payment is recorded.
 * @param {Function} setLoading - State setter to track the request.
 * @param {Function} onSuccess - Callback invoked once the payment has been recorded.
 */
export const handleMarkBegunstigerFeeAsPaid = async (
  memberId: string | undefined,
  setLoading: (loading: boolean) => void,
  onSuccess: () => void,
) => {
  if (!memberId) return;

  const process = async () => {
    try {
      setLoading(true);
      const response = await postPaymentsBegunstiger({
        body: {
          memberId,
          manuallyMarkedAsPaid: true,
        },
      });

      if (response.error) {
        throw (
          response.error ?? new Error("Failed to mark begunstiger fee as paid")
        );
      }

      onSuccess();
    } catch (err) {
      console.error("Failed to mark begunstiger fee as paid:", err);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(process(), {
    loading: t("marking_as_paid"),
    success: t("marked_as_paid"),
    error: (error) => appendErrorMessage(t("mark_as_paid_failed"), error),
  });
};

/**
 * Removes a specific study enrollment record from the member's profile.
 *
 * @async
 * @param {number} id - The unique ID of the study enrollment to delete.
 * @param {Function} setLoading - State setter to track the deletion request.
 * @param {Function} setEnrollments - State setter to update the local list of enrollments.
 */
export const handleDeleteEnrollment = async (
  id: number,
  setLoading: (loading: boolean) => void,
  setEnrollments: React.Dispatch<
    React.SetStateAction<StudyEnrollmentResponseDto[]>
  >,
) => {
  const deleteProcess = async () => {
    try {
      setLoading(true);
      const response = await deleteStudyenrollmentsById({ path: { id } });

      if (response.error) {
        throw response.error ?? new Error("Failed to delete enrollment");
      }

      setEnrollments((prev) => prev.filter((e) => e.id !== id));
    } catch (err) {
      console.error("Failed to delete enrollment:", err);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(deleteProcess(), {
    loading: t("deleting"),
    success: t("delete_success"),
    error: (error) => appendErrorMessage(t("delete_error"), error),
  });
};

/**
 * Adds a new study enrollment record to the member's profile.
 *
 * @async
 * @param {string | undefined} memberId - The ID of the member receiving the enrollment.
 * @param {number | ""} selectedStudyId - The ID of the chosen study program.
 * @param {Function} setLoading - State setter to track the creation request.
 * @param {Function} setEnrollments - State setter to add the new record to the local list.
 */
export const handleAddEnrollment = async (
  memberId: string | undefined,
  selectedStudyId: number | "",
  setLoading: (loading: boolean) => void,
  setEnrollments: React.Dispatch<
    React.SetStateAction<StudyEnrollmentResponseDto[]>
  >,
  startDate?: string,
) => {
  if (!memberId || !selectedStudyId) return;
  const executeProcess = async () => {
    try {
      setLoading(true);
      const res = await postStudyenrollments({
        body: {
          memberId,
          studyId: selectedStudyId,
          enrollmentDate: startDate
            ? new Date(startDate).toISOString()
            : new Date().toISOString(),
        },
      });

      if (res.error || !res.data) {
        throw res.error ?? new Error("Failed to add enrollment");
      }

      setEnrollments((prev) => [...prev, res.data]);
    } catch (err) {
      console.error("Failed to add enrollment:", err);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(executeProcess(), {
    loading: t("adding"),
    success: t("add_success"),
    error: (error) => appendErrorMessage(t("add_error"), error),
  });
};

/**
 * Updates the status (e.g., Active, Graduated) of an existing study enrollment via JSON Patch.
 *
 * @async
 * @param {number} enrollmentId - The unique ID of the enrollment record to update.
 * @param {StudyStatus} newStatus - The new status to be assigned.
 * @param {Function} setLoading - State setter to track the update request.
 * @param {Function} setEnrollments - State setter to refresh the status in the local list.
 */
export const handleUpdateEnrollmentStatus = async (
  enrollmentId: number,
  newStatus: StudyStatus,
  setLoading: (loading: boolean) => void,
  setEnrollments: React.Dispatch<
    React.SetStateAction<StudyEnrollmentResponseDto[]>
  >,
) => {
  const saveProcess = async () => {
    try {
      setLoading(true);
      const response = await patchStudyenrollmentsById({
        path: { id: enrollmentId },
        body: [{ op: "replace", path: "/status", value: newStatus }] as any,
      });

      if (response.error) {
        throw response.error ?? new Error("Failed to update status");
      }

      setEnrollments((prev) =>
        prev.map((e) =>
          e.id === enrollmentId ? { ...e, status: newStatus as any } : e,
        ),
      );
    } catch (err) {
      console.error("Failed to update enrollment status:", err);
      throw err;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(saveProcess(), {
    loading: t("updating_status"),
    success: t("status_updated"),
    error: (error) => appendErrorMessage(t("status_update_failed"), error),
  });
};
