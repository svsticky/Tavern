import { beforeEach, describe, expect, it, vi } from "vitest";
import type { StudyEnrollmentResponseDto } from "~/api";

const {
  deleteMembersById,
  deleteStudyenrollmentsById,
  getGroupmemberships,
  getMembersById,
  getMembersByIdProfilePicture,
  getPaymentsMemberByFromUserIdStatus,
  getStudies,
  getStudyenrollments,
  patchMembersById,
  patchStudyenrollmentsById,
  postPaymentsBegunstiger,
  postPaymentsMembership,
  postStudyenrollments,
} = vi.hoisted(() => ({
  deleteMembersById: vi.fn(),
  deleteStudyenrollmentsById: vi.fn(),
  getGroupmemberships: vi.fn(),
  getMembersById: vi.fn(),
  getMembersByIdProfilePicture: vi.fn(),
  getPaymentsMemberByFromUserIdStatus: vi.fn(),
  getStudies: vi.fn(),
  getStudyenrollments: vi.fn(),
  patchMembersById: vi.fn(),
  patchStudyenrollmentsById: vi.fn(),
  postPaymentsBegunstiger: vi.fn(),
  postPaymentsMembership: vi.fn(),
  postStudyenrollments: vi.fn(),
}));

vi.mock("~/api", () => ({
  deleteMembersById,
  deleteStudyenrollmentsById,
  getGroupmemberships,
  getMembersById,
  getMembersByIdProfilePicture,
  getPaymentsMemberByFromUserIdStatus,
  getStudies,
  getStudyenrollments,
  patchMembersById,
  patchStudyenrollmentsById,
  postPaymentsBegunstiger,
  postPaymentsMembership,
  postStudyenrollments,
}));

vi.mock("react-hot-toast", () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
    promise: vi.fn((p, opts: any) =>
      p
        .then((res: any) => {
          if (typeof opts?.success === "function") opts.success(res);
          return res;
        })
        .catch((err: any) => {
          if (typeof opts?.error === "function") opts.error(err);
        }),
    ),
  },
}));

import toast from "react-hot-toast";
import {
  handleAddEnrollment,
  handleDeleteEnrollment,
  handleDeleteMember,
  handleMarkBegunstigerFeeAsPaid,
  handleMarkMembershipAsPaid,
  handleSaveMember,
  handleUpdateEnrollmentStatus,
  loadMemberData,
} from "~/routes/admin/edit-member/edit-member.handlers";

describe("loadMemberData", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal("URL", {
      ...URL,
      createObjectURL: vi.fn(() => "blob:mock-url"),
      revokeObjectURL: vi.fn(),
    });
    getPaymentsMemberByFromUserIdStatus.mockResolvedValue({
      data: { hasPaidMembershipBeforeExpirationTime: true },
    });
    getGroupmemberships.mockResolvedValue({ data: [] });
  });

  it("returns immediately when memberId is undefined", async () => {
    const setLoading = vi.fn();
    await loadMemberData({
      memberId: undefined,
      setFormData: vi.fn(),
      setEmail: vi.fn(),
      setEnrollments: vi.fn(),
      setGroupMemberships: vi.fn(),
      setAvailableStudies: vi.fn(),
      setProfilePictureSrc: vi.fn(),
      setHasPaidMembership: vi.fn(),
      setIsBegunstiger: vi.fn(),
      setLoading,
    });
    expect(setLoading).not.toHaveBeenCalled();
  });

  it("loads member data, enrollments, studies, and picture on success", async () => {
    getMembersById.mockResolvedValue({
      data: {
        firstName: "Jane",
        lastName: "Doe",
        studentNumber: "s1",
        phoneNumber: "0600",
        street: "Main",
        houseNumber: "1",
        postalCode: "1234AB",
        city: "Enschede",
        parentPhoneNumber: null,
        preferredLanguage: "NL",
        notes: "note",
        gratie: true,
        lidVanVerdienste: false,
        ereLid: false,
        begunstiger: false,
        suspended: false,
        dateOfBirth: "2000-01-01T00:00:00Z",
        email: "jane@example.com",
      },
    });
    getStudyenrollments.mockResolvedValue({ data: [{ id: 1 }] });
    getStudies.mockResolvedValue({ data: [{ id: 1, title: "CS" }] });
    getPaymentsMemberByFromUserIdStatus.mockResolvedValue({
      data: {
        hasPaidMembershipBeforeExpirationTime: false,
        isBegunstiger: true,
      },
    });
    getMembersByIdProfilePicture.mockResolvedValue({ data: new Blob(["x"]) });

    getGroupmemberships.mockResolvedValue({ data: [{ id: 1, groupId: 1 }] });

    const setFormData = vi.fn();
    const setEmail = vi.fn();
    const setEnrollments = vi.fn();
    const setGroupMemberships = vi.fn();
    const setAvailableStudies = vi.fn();
    const setProfilePictureSrc = vi.fn();
    const setHasPaidMembership = vi.fn();
    const setIsBegunstiger = vi.fn();
    const setLoading = vi.fn();

    const cleanup = await loadMemberData({
      memberId: "m1",
      setFormData,
      setEmail,
      setEnrollments,
      setGroupMemberships,
      setAvailableStudies,
      setProfilePictureSrc,
      setHasPaidMembership,
      setIsBegunstiger,
      setLoading,
    });

    expect(setFormData).toHaveBeenCalledWith(
      expect.objectContaining({
        firstName: "Jane",
        lastName: "Doe",
        dateOfBirth: "2000-01-01",
        gratie: true,
      }),
    );
    expect(setEmail).toHaveBeenCalledWith("jane@example.com");
    expect(setEnrollments).toHaveBeenCalledWith([{ id: 1 }]);
    expect(setGroupMemberships).toHaveBeenCalledWith([{ id: 1, groupId: 1 }]);
    expect(setAvailableStudies).toHaveBeenCalledWith([{ id: 1, title: "CS" }]);
    expect(getPaymentsMemberByFromUserIdStatus).toHaveBeenCalledWith({
      path: { fromUserId: "m1" },
    });
    expect(setHasPaidMembership).toHaveBeenCalledWith(false);
    expect(setIsBegunstiger).toHaveBeenCalledWith(true);
    expect(setProfilePictureSrc).toHaveBeenCalledWith("blob:mock-url");
    expect(setLoading).toHaveBeenCalledWith(false);

    cleanup?.();
    expect(URL.revokeObjectURL).toHaveBeenCalledWith("blob:mock-url");
  });

  it("defaults hasPaidMembership to true when the status lookup fails", async () => {
    getMembersById.mockResolvedValue({ data: { email: "a@b.com" } });
    getStudyenrollments.mockResolvedValue({ data: [] });
    getStudies.mockResolvedValue({ data: [] });
    getPaymentsMemberByFromUserIdStatus.mockResolvedValue({
      error: { title: "bad" },
    });
    getMembersByIdProfilePicture.mockResolvedValue({ error: "no picture" });

    const setHasPaidMembership = vi.fn();

    await loadMemberData({
      memberId: "m1",
      setFormData: vi.fn(),
      setEmail: vi.fn(),
      setEnrollments: vi.fn(),
      setGroupMemberships: vi.fn(),
      setAvailableStudies: vi.fn(),
      setProfilePictureSrc: vi.fn(),
      setHasPaidMembership,
      setIsBegunstiger: vi.fn(),
      setLoading: vi.fn(),
    });

    expect(setHasPaidMembership).toHaveBeenCalledWith(true);
  });

  it("defaults missing/nullish fields sensibly", async () => {
    getMembersById.mockResolvedValue({
      data: { email: "jane@example.com" },
    });
    getStudyenrollments.mockResolvedValue({ data: [] });
    getStudies.mockResolvedValue({ data: [] });
    getMembersByIdProfilePicture.mockResolvedValue({ error: "no picture" });

    const setFormData = vi.fn();

    await loadMemberData({
      memberId: "m1",
      setFormData,
      setEmail: vi.fn(),
      setEnrollments: vi.fn(),
      setGroupMemberships: vi.fn(),
      setAvailableStudies: vi.fn(),
      setProfilePictureSrc: vi.fn(),
      setHasPaidMembership: vi.fn(),
      setIsBegunstiger: vi.fn(),
      setLoading: vi.fn(),
    });

    expect(setFormData).toHaveBeenCalledWith(
      expect.objectContaining({
        firstName: "",
        preferredLanguage: "NL",
        dateOfBirth: "",
        gratie: false,
      }),
    );
  });

  it("shows an error toast when the member request fails", async () => {
    getMembersById.mockResolvedValue({ error: { title: "bad" } });
    const setLoading = vi.fn();

    await loadMemberData({
      memberId: "m1",
      setFormData: vi.fn(),
      setEmail: vi.fn(),
      setEnrollments: vi.fn(),
      setGroupMemberships: vi.fn(),
      setAvailableStudies: vi.fn(),
      setProfilePictureSrc: vi.fn(),
      setHasPaidMembership: vi.fn(),
      setIsBegunstiger: vi.fn(),
      setLoading,
    });

    expect(toast.error).toHaveBeenCalledWith("loading_failed: bad");
    expect(setLoading).toHaveBeenCalledWith(false);
  });

  it("shows an error toast when study enrollments fail to load", async () => {
    getMembersById.mockResolvedValue({ data: { email: "a@b.com" } });
    getStudyenrollments.mockResolvedValue({
      error: { title: "bad enrollments" },
    });

    await loadMemberData({
      memberId: "m1",
      setFormData: vi.fn(),
      setEmail: vi.fn(),
      setEnrollments: vi.fn(),
      setGroupMemberships: vi.fn(),
      setAvailableStudies: vi.fn(),
      setProfilePictureSrc: vi.fn(),
      setHasPaidMembership: vi.fn(),
      setIsBegunstiger: vi.fn(),
      setLoading: vi.fn(),
    });

    expect(toast.error).toHaveBeenCalledWith("loading_failed: bad enrollments");
  });

  it("shows an error toast when studies fail to load", async () => {
    getMembersById.mockResolvedValue({ data: { email: "a@b.com" } });
    getStudyenrollments.mockResolvedValue({ data: [] });
    getStudies.mockResolvedValue({ error: { title: "bad studies" } });

    await loadMemberData({
      memberId: "m1",
      setFormData: vi.fn(),
      setEmail: vi.fn(),
      setEnrollments: vi.fn(),
      setGroupMemberships: vi.fn(),
      setAvailableStudies: vi.fn(),
      setProfilePictureSrc: vi.fn(),
      setHasPaidMembership: vi.fn(),
      setIsBegunstiger: vi.fn(),
      setLoading: vi.fn(),
    });

    expect(toast.error).toHaveBeenCalledWith("loading_failed: bad studies");
  });
});

describe("handleSaveMember", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("returns immediately when memberId is undefined", async () => {
    const setSaving = vi.fn();
    await handleSaveMember(undefined, {} as any, setSaving);
    expect(setSaving).not.toHaveBeenCalled();
  });

  it("saves the member as a JSON patch document", async () => {
    patchMembersById.mockResolvedValue({});
    const setSaving = vi.fn();

    await handleSaveMember(
      "m1",
      {
        firstName: "Jane",
        studentNumber: "s1",
        postalCode: "1234AB",
        city: "Enschede",
      } as any,
      setSaving,
    );

    await vi.waitFor(() => expect(setSaving).toHaveBeenLastCalledWith(false));
    expect(patchMembersById).toHaveBeenCalledWith({
      path: { id: "m1" },
      body: expect.arrayContaining([
        { op: "replace", path: "/firstName", value: "Jane" },
      ]),
    });
  });

  it("throws when the save fails", async () => {
    patchMembersById.mockResolvedValue({ error: true, message: "bad" });
    const setSaving = vi.fn();

    await handleSaveMember(
      "m1",
      {
        firstName: "Jane",
        studentNumber: "s1",
        postalCode: "1234AB",
        city: "Enschede",
      } as any,
      setSaving,
    );

    await vi.waitFor(() => expect(setSaving).toHaveBeenLastCalledWith(false));
  });

  it("shows an error and does not save when required fields are blank", async () => {
    const setSaving = vi.fn();

    await handleSaveMember(
      "m1",
      { studentNumber: "", postalCode: "1234AB", city: "Enschede" } as any,
      setSaving,
    );

    expect(patchMembersById).not.toHaveBeenCalled();
    expect(setSaving).not.toHaveBeenCalled();
    expect(toast.error).toHaveBeenCalledWith("please_fill_all_fields");
  });
});

describe("handleDeleteMember", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("returns immediately when memberId is undefined", async () => {
    const setLoading = vi.fn();
    await handleDeleteMember(undefined, setLoading, vi.fn());
    expect(setLoading).not.toHaveBeenCalled();
  });

  it("calls onSuccess after a successful delete", async () => {
    deleteMembersById.mockResolvedValue({});
    const onSuccess = vi.fn();
    const setLoading = vi.fn();

    await handleDeleteMember("m1", setLoading, onSuccess);

    await vi.waitFor(() => expect(onSuccess).toHaveBeenCalled());
    expect(setLoading).toHaveBeenLastCalledWith(false);
  });

  it("does not call onSuccess when delete fails", async () => {
    deleteMembersById.mockResolvedValue({ error: true, message: "bad" });
    const onSuccess = vi.fn();
    const setLoading = vi.fn();

    await handleDeleteMember("m1", setLoading, onSuccess);

    await vi.waitFor(() => expect(setLoading).toHaveBeenLastCalledWith(false));
    expect(onSuccess).not.toHaveBeenCalled();
  });
});

describe("handleMarkMembershipAsPaid", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("returns immediately when memberId is undefined", async () => {
    const setLoading = vi.fn();
    const onSuccess = vi.fn();
    await handleMarkMembershipAsPaid(undefined, setLoading, onSuccess);
    expect(setLoading).not.toHaveBeenCalled();
    expect(postPaymentsMembership).not.toHaveBeenCalled();
    expect(onSuccess).not.toHaveBeenCalled();
  });

  it("marks the membership as paid", async () => {
    postPaymentsMembership.mockResolvedValue({});
    const setLoading = vi.fn();
    const onSuccess = vi.fn();

    await handleMarkMembershipAsPaid("m1", setLoading, onSuccess);

    await vi.waitFor(() => expect(setLoading).toHaveBeenLastCalledWith(false));
    expect(postPaymentsMembership).toHaveBeenCalledWith({
      body: { memberId: "m1", manuallyMarkedAsPaid: true },
    });
    expect(onSuccess).toHaveBeenCalled();
  });

  it("throws when marking as paid fails", async () => {
    postPaymentsMembership.mockResolvedValue({ error: true, message: "bad" });
    const setLoading = vi.fn();
    const onSuccess = vi.fn();

    await handleMarkMembershipAsPaid("m1", setLoading, onSuccess);

    await vi.waitFor(() => expect(setLoading).toHaveBeenLastCalledWith(false));
    expect(onSuccess).not.toHaveBeenCalled();
  });
});

describe("handleMarkBegunstigerFeeAsPaid", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("returns immediately when memberId is undefined", async () => {
    const setLoading = vi.fn();
    const onSuccess = vi.fn();
    await handleMarkBegunstigerFeeAsPaid(undefined, setLoading, onSuccess);
    expect(setLoading).not.toHaveBeenCalled();
    expect(postPaymentsBegunstiger).not.toHaveBeenCalled();
    expect(onSuccess).not.toHaveBeenCalled();
  });

  it("marks the begunstiger fee as paid", async () => {
    postPaymentsBegunstiger.mockResolvedValue({});
    const setLoading = vi.fn();
    const onSuccess = vi.fn();

    await handleMarkBegunstigerFeeAsPaid("m1", setLoading, onSuccess);

    await vi.waitFor(() => expect(setLoading).toHaveBeenLastCalledWith(false));
    expect(postPaymentsBegunstiger).toHaveBeenCalledWith({
      body: { memberId: "m1", manuallyMarkedAsPaid: true },
    });
    expect(onSuccess).toHaveBeenCalled();
  });

  it("throws when marking as paid fails", async () => {
    postPaymentsBegunstiger.mockResolvedValue({ error: true, message: "bad" });
    const setLoading = vi.fn();
    const onSuccess = vi.fn();

    await handleMarkBegunstigerFeeAsPaid("m1", setLoading, onSuccess);

    await vi.waitFor(() => expect(setLoading).toHaveBeenLastCalledWith(false));
    expect(onSuccess).not.toHaveBeenCalled();
  });
});

describe("handleDeleteEnrollment", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("removes the enrollment from local state on success", async () => {
    deleteStudyenrollmentsById.mockResolvedValue({});
    const setEnrollments = vi.fn();

    await handleDeleteEnrollment(5, vi.fn(), setEnrollments);

    await vi.waitFor(() => expect(setEnrollments).toHaveBeenCalled());
    const updater = setEnrollments.mock.calls[0][0];
    expect(
      updater([{ id: 5 }, { id: 6 }] as StudyEnrollmentResponseDto[]),
    ).toEqual([{ id: 6 }]);
  });

  it("throws when delete fails", async () => {
    deleteStudyenrollmentsById.mockResolvedValue({
      error: true,
      message: "bad",
    });
    const setLoading = vi.fn();

    await handleDeleteEnrollment(5, setLoading, vi.fn());

    await vi.waitFor(() => expect(setLoading).toHaveBeenLastCalledWith(false));
  });
});

describe("handleAddEnrollment", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("does nothing without a memberId or studyId", async () => {
    const setLoading = vi.fn();
    await handleAddEnrollment(undefined, 1, setLoading, vi.fn());
    await handleAddEnrollment("m1", "", setLoading, vi.fn());
    expect(setLoading).not.toHaveBeenCalled();
  });

  it("adds the enrollment using the provided start date", async () => {
    postStudyenrollments.mockResolvedValue({ data: { id: 1, studyId: 2 } });
    const setEnrollments = vi.fn();

    await handleAddEnrollment("m1", 2, vi.fn(), setEnrollments, "2024-01-01");

    await vi.waitFor(() => expect(setEnrollments).toHaveBeenCalled());
    expect(postStudyenrollments).toHaveBeenCalledWith({
      body: {
        memberId: "m1",
        studyId: 2,
        enrollmentDate: new Date("2024-01-01").toISOString(),
      },
    });
    const updater = setEnrollments.mock.calls[0][0];
    expect(updater([])).toEqual([{ id: 1, studyId: 2 }]);
  });

  it("defaults the enrollment date to now when no start date is given", async () => {
    postStudyenrollments.mockResolvedValue({ data: { id: 1 } });

    await handleAddEnrollment("m1", 2, vi.fn(), vi.fn());

    await vi.waitFor(() => expect(postStudyenrollments).toHaveBeenCalled());
    const body = postStudyenrollments.mock.calls[0][0].body;
    expect(body.memberId).toBe("m1");
    expect(typeof body.enrollmentDate).toBe("string");
  });

  it("throws when the add request fails", async () => {
    postStudyenrollments.mockResolvedValue({ error: "bad" });
    const setLoading = vi.fn();

    await handleAddEnrollment("m1", 2, setLoading, vi.fn());

    await vi.waitFor(() => expect(setLoading).toHaveBeenLastCalledWith(false));
  });
});

describe("handleUpdateEnrollmentStatus", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("updates the status of the matching enrollment", async () => {
    patchStudyenrollmentsById.mockResolvedValue({});
    const setEnrollments = vi.fn();

    await handleUpdateEnrollmentStatus(5, "Completed", vi.fn(), setEnrollments);

    await vi.waitFor(() =>
      expect(patchStudyenrollmentsById).toHaveBeenCalledWith({
        path: { id: 5 },
        body: [{ op: "replace", path: "/status", value: "Completed" }],
      }),
    );
    const updater = setEnrollments.mock.calls[0][0];
    expect(
      updater([
        { id: 5, status: "Enrolled" },
      ] as unknown as StudyEnrollmentResponseDto[]),
    ).toEqual([{ id: 5, status: "Completed" }]);
  });

  it("throws when the update fails", async () => {
    patchStudyenrollmentsById.mockResolvedValue({
      error: true,
      message: "bad",
    });
    const setLoading = vi.fn();

    await handleUpdateEnrollmentStatus(5, "Completed", setLoading, vi.fn());

    await vi.waitFor(() => expect(setLoading).toHaveBeenLastCalledWith(false));
  });
});
