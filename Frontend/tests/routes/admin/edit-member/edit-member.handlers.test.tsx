import { beforeEach, describe, expect, it, vi } from "vitest";
import type { StudyEnrollmentResponseDto } from "~/api";

const {
  deleteMembersById,
  deleteStudyenrollmentsById,
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
} = vi.hoisted(() => ({
  deleteMembersById: vi.fn(),
  deleteStudyenrollmentsById: vi.fn(),
  getGroupmemberships: vi.fn(),
  getMembersById: vi.fn(),
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
  getPaymentsMemberByFromUserIdStatus,
  getStudies,
  getStudyenrollments,
  patchMembersById,
  patchStudyenrollmentsById,
  postPaymentsBegunstiger,
  postPaymentsMembership,
  postStudyenrollments,
}));

const { loadProfilePictureSrc } = vi.hoisted(() => ({
  loadProfilePictureSrc: vi.fn(),
}));

vi.mock(
  "~/components/Account/ChangeProfilePicture/ChangeProfilePicture.handlers",
  () => ({ loadProfilePictureSrc }),
);

vi.mock("react-hot-toast", () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
    promise: vi.fn((p) => p.catch(() => {})),
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
  loadEditMemberLoaderData,
} from "~/routes/admin/edit-member/edit-member.handlers";

describe("loadEditMemberLoaderData", () => {
  const member = {
    id: "m1",
    firstName: "Jane",
    lastName: "Doe",
    email: "jane@example.com",
    studentNumber: "s1",
    phoneNumber: "0612345678",
    street: "Street",
    houseNumber: "1",
    postalCode: "1234AB",
    city: "Enschede",
    parentPhoneNumber: "0687654321",
    preferredLanguage: "EN",
    notes: "Note",
    gratie: true,
    lidVanVerdienste: false,
    ereLid: false,
    begunstiger: false,
    suspended: false,
    dateOfBirth: "2000-05-06T00:00:00Z",
  };

  beforeEach(() => {
    vi.clearAllMocks();
    getMembersById.mockResolvedValue({ data: member });
    getStudyenrollments.mockResolvedValue({ data: [{ id: 1 }] });
    getGroupmemberships.mockResolvedValue({ data: [{ id: 2 }] });
    getStudies.mockResolvedValue({ data: [{ id: 3 }] });
    getPaymentsMemberByFromUserIdStatus.mockResolvedValue({
      data: {
        hasPaidMembershipBeforeExpirationTime: false,
        isBegunstiger: true,
      },
    });
    loadProfilePictureSrc.mockResolvedValue("blob:picture");
  });

  it("throws a 404 response when there is no id", async () => {
    await expect(loadEditMemberLoaderData({})).rejects.toMatchObject({
      init: { status: 404 },
    });
    expect(getMembersById).not.toHaveBeenCalled();
  });

  it("returns the member's details, enrollments, groups, studies and payment status", async () => {
    await expect(loadEditMemberLoaderData({ id: "m1" })).resolves.toEqual({
      memberId: "m1",
      formData: {
        firstName: "Jane",
        lastName: "Doe",
        studentNumber: "s1",
        phoneNumber: "0612345678",
        street: "Street",
        houseNumber: "1",
        postalCode: "1234AB",
        city: "Enschede",
        parentPhoneNumber: "0687654321",
        preferredLanguage: "EN",
        notes: "Note",
        gratie: true,
        lidVanVerdienste: false,
        ereLid: false,
        begunstiger: false,
        suspended: false,
        dateOfBirth: "2000-05-06",
      },
      email: "jane@example.com",
      enrollments: [{ id: 1 }],
      groupMemberships: [{ id: 2 }],
      availableStudies: [{ id: 3 }],
      profilePictureSrc: "blob:picture",
      hasPaidMembership: false,
      isBegunstiger: true,
    });
    expect(getStudyenrollments).toHaveBeenCalledWith({
      query: { MemberId: "m1" },
    });
    expect(getGroupmemberships).toHaveBeenCalledWith({
      query: { MemberId: "m1" },
    });
    expect(getPaymentsMemberByFromUserIdStatus).toHaveBeenCalledWith({
      path: { fromUserId: "m1" },
    });
    expect(loadProfilePictureSrc).toHaveBeenCalledWith("m1");
  });

  it("defaults missing fields sensibly", async () => {
    getMembersById.mockResolvedValue({ data: { id: "m1" } });

    const data = await loadEditMemberLoaderData({ id: "m1" });

    expect(data.formData).toEqual(
      expect.objectContaining({
        firstName: "",
        preferredLanguage: "NL",
        gratie: false,
        dateOfBirth: "",
      }),
    );
    expect(data.email).toBe("");
  });

  it("assumes the fee is paid when the payment status fails to load", async () => {
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    getPaymentsMemberByFromUserIdStatus.mockRejectedValue(new Error("boom"));

    const data = await loadEditMemberLoaderData({ id: "m1" });

    expect(data.hasPaidMembership).toBe(true);
    expect(data.isBegunstiger).toBe(false);
    consoleError.mockRestore();
  });

  it.each([
    ["member data", () => getMembersById.mockResolvedValue({ data: null })],
    [
      "study enrollments",
      () => getStudyenrollments.mockResolvedValue({ data: null }),
    ],
    [
      "group memberships",
      () => getGroupmemberships.mockResolvedValue({ data: null }),
    ],
    ["available studies", () => getStudies.mockResolvedValue({ data: null })],
  ])("throws when the %s fail to load", async (name, mockFailure) => {
    mockFailure();

    await expect(loadEditMemberLoaderData({ id: "m1" })).rejects.toThrow(
      `Failed to load ${name}`,
    );
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
