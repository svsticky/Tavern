import type React from "react";
import { t } from "i18next";
import toast from "react-hot-toast";
import {
  deleteApiStudyenrollmentsById,
  getApiMembersById,
  getApiMembersByIdProfilePicture,
  getApiStudies,
  getApiStudyenrollments,
  patchApiMembersById,
  patchApiStudyenrollmentsById,
  postApiProfilepictureByIdProfilePicture,
  postApiStudyenrollments,
  type Study,
  type StudyEnrollmentResponseDto,
  type StudyStatus
} from "~/api";

type EditMemberFormData = {
  firstName: string;
  lastName: string;
  studentNumber: number;
  phoneNumber: string;
  street: string;
  houseNumber: string;
  postalCode: string;
  city: string;
  parentPhoneNumber: string;
  preferredLanguage: string;
  mailSubscriptions: number;
  notes: string;
  gratie: boolean;
  lidVanVerdienste: boolean;
  ereLid: boolean;
  begunstiger: boolean;
  suspended: boolean;
  dateOfBirth: string;
};

type LoadMemberArgs = {
  memberId: string | undefined;
  setFormData: React.Dispatch<React.SetStateAction<EditMemberFormData>>;
  setEmail: (value: string) => void;
  setEnrollments: React.Dispatch<React.SetStateAction<StudyEnrollmentResponseDto[]>>;
  setAvailableStudies: React.Dispatch<React.SetStateAction<Study[]>>;
  setProfilePictureSrc: (value: string | null) => void;
  setLoading: (value: boolean) => void;
};

export const loadMemberData = async ({
  memberId,
  setFormData,
  setEmail,
  setEnrollments,
  setAvailableStudies,
  setProfilePictureSrc,
  setLoading
}: LoadMemberArgs) => {
  if (!memberId) return;
  let url = null as string | null;

  try {
    const memberResponse = await getApiMembersById({ path: { id: memberId } });
    if (memberResponse.error || !memberResponse.data) throw new Error("Failed to load member data");
    setFormData({
      firstName: memberResponse.data.firstName || "",
      lastName: memberResponse.data.lastName || "",
      studentNumber: Number(memberResponse.data.studentNumber) || 0,
      phoneNumber: memberResponse.data.phoneNumber || "",
      street: memberResponse.data.street || "",
      houseNumber: memberResponse.data.houseNumber || "",
      postalCode: memberResponse.data.postalCode || "",
      city: memberResponse.data.city || "",
      parentPhoneNumber: memberResponse.data.parentPhoneNumber || "",
      preferredLanguage: memberResponse.data.preferredLanguage ?? "NL",
      mailSubscriptions: Number(memberResponse.data.mailSubscriptions) || 0,
      notes: memberResponse.data.notes || "",
      gratie: !!memberResponse.data.gratie,
      lidVanVerdienste: !!memberResponse.data.lidVanVerdienste,
      ereLid: !!memberResponse.data.ereLid,
      begunstiger: !!memberResponse.data.begunstiger,
      suspended: !!memberResponse.data.suspended,
      dateOfBirth: memberResponse.data.dateOfBirth ? new Date(memberResponse.data.dateOfBirth).toISOString().split("T")[0] : ""
    });

    setEmail(memberResponse.data.email!);

    const studyEnrollmentsResponse = await getApiStudyenrollments({ query: { MemberId: memberId } });
    if (studyEnrollmentsResponse.error || !studyEnrollmentsResponse.data) throw new Error("Failed to load study enrollments");
    setEnrollments(studyEnrollmentsResponse.data);

    const studiesResponse = await getApiStudies();
    if (studiesResponse.error || !studiesResponse.data) throw new Error("Failed to load available studies");
    setAvailableStudies(studiesResponse.data);

    const profilePictureResponse = await getApiMembersByIdProfilePicture({ path: { id: memberId }, responseType: "blob" });
    if (profilePictureResponse.error || !(profilePictureResponse.data instanceof Blob)) return;
    url = URL.createObjectURL(profilePictureResponse.data);
    setProfilePictureSrc(url);
  } catch (err) {
    console.log("Failed to load member data:", err);
    toast.error(t("loading_failed"));
  } finally {
    setLoading(false);
  }

  return () => {
    if (url) URL.revokeObjectURL(url);
  };
};

export const handleSaveMember = async (
  memberId: string | undefined,
  formData: EditMemberFormData,
  setSaving: (saving: boolean) => void
) => {
  if (!memberId) return;
  const saveProcess = async () => {
    try {
      setSaving(true);

      const patchDoc = Object.keys(formData).map((key) => ({
        op: "replace",
        path: `/${key}`,
        value: formData[key as keyof typeof formData]
      }));

      const response = await patchApiMembersById({
        path: { id: memberId },
        body: patchDoc as any
      });

      if (response.error) throw new Error("Failed to save member data");
    } catch (err) {
      console.error("Failed to save member data:", err);
      throw err;
    } finally {
      setSaving(false);
    }
  };

  toast.promise(saveProcess(), {
    loading: t("saving"),
    success: t("save_success"),
    error: t("save_error")
  }).finally(() => setSaving(false));
};

export const handleDeleteEnrollment = async (
  id: number,
  setLoading: (loading: boolean) => void,
  setEnrollments: React.Dispatch<React.SetStateAction<StudyEnrollmentResponseDto[]>>
) => {
  const deleteProcess = async () => {
    try {
      setLoading(true);
      const response = await deleteApiStudyenrollmentsById({ path: { id } });

      if (response.error) throw new Error("Failed to delete enrollment");

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
    error: t("delete_error")
  });
};

export const handleAddEnrollment = async (
  memberId: string | undefined,
  selectedStudyId: number | "",
  setLoading: (loading: boolean) => void,
  setEnrollments: React.Dispatch<React.SetStateAction<StudyEnrollmentResponseDto[]>>
) => {
  if (!memberId || !selectedStudyId) return;
  const executeProcess = async () => {
    try {
      setLoading(true);
      const res = await postApiStudyenrollments({
        body: {
          memberId,
          studyId: selectedStudyId,
          enrollmentDate: new Date().toISOString(),
        }
      });

      if(res.error || !res.data) throw new Error("Failed to add enrollment");
      
      setEnrollments((prev) => [...prev, res.data]);
      toast.success("Studie toegevoegd");
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
    error: t("add_error")
  });
};

export const handleUpdateEnrollmentStatus = async (
  enrollmentId: number,
  newStatus: StudyStatus,
  setLoading: (loading: boolean) => void,
  setEnrollments: React.Dispatch<React.SetStateAction<StudyEnrollmentResponseDto[]>>
) => {
  const saveProcess = async () => {
    try {
      setLoading(true);
      const response = await patchApiStudyenrollmentsById({
        path: { id: enrollmentId },
        body: [
          { op: "replace", path: "/status", value: newStatus }
        ] as any
      });

      if (response.error) throw new Error("Failed to update status");

      setEnrollments((prev) =>
        prev.map((e) =>
          e.id === enrollmentId
            ? { ...e, status: newStatus as any }
            : e
        )
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
    error: t("status_update_failed")
  });
};
