import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto, MemberResponseDto } from "~/api";
import {
  handleDownloadEnrollments,
  handleEnrollParticipant,
  handleMoveToParticipants,
  handleUnenrollParticipant,
} from "~/components/Activity/Edit/EditParticipantsTile/EditParticipantsTile.handlers";

const { getActivitiesByIdEnrollmentsExport, postEnrollments } = vi.hoisted(
  () => ({
    getActivitiesByIdEnrollmentsExport: vi.fn(),
    postEnrollments: vi.fn(),
  }),
);

vi.mock("~/api", () => ({
  getActivitiesByIdEnrollmentsExport,
  postEnrollments,
}));

const toastFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: Object.assign((...args: unknown[]) => toastFn(...args), {
    success: vi.fn(),
    error: vi.fn(),
    promise: vi.fn((p: Promise<unknown>, opts: any) => {
      p.then(
        (data) => opts.success?.(data),
        (err) => opts.error?.(err),
      ).catch(() => {});
      return p;
    }),
  }),
}));

function buildActivity(
  overrides: Partial<ActivityResponseDto> = {},
): ActivityResponseDto {
  return {
    id: 1,
    name: "Party",
    enrollments: [],
    ...overrides,
  } as ActivityResponseDto;
}

describe("handleDownloadEnrollments", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("triggers a CSV download on success", async () => {
    getActivitiesByIdEnrollmentsExport.mockResolvedValue({
      data: new Blob(["a,b"]),
    });
    const createObjectURL = vi.fn(() => "blob:url");
    const revokeObjectURL = vi.fn();
    vi.stubGlobal("URL", { createObjectURL, revokeObjectURL });
    const clickSpy = vi
      .spyOn(HTMLAnchorElement.prototype, "click")
      .mockImplementation(() => {});

    handleDownloadEnrollments(buildActivity());

    await vi.waitFor(() => expect(clickSpy).toHaveBeenCalled());
    expect(createObjectURL).toHaveBeenCalled();
    expect(revokeObjectURL).toHaveBeenCalledWith("blob:url");
    clickSpy.mockRestore();
    vi.unstubAllGlobals();
  });

  it("logs and rethrows when the response has an error", async () => {
    getActivitiesByIdEnrollmentsExport.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    handleDownloadEnrollments(buildActivity());

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });
});

describe("handleEnrollParticipant", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("adds the new enrollment and closes the search overlay on success", async () => {
    postEnrollments.mockResolvedValue({
      data: {
        member: { id: "m1" },
        activity: {},
        isOnWaitingList: false,
        price: 5,
      },
    });
    const activity = buildActivity();
    const setActivity = vi.fn();
    const setLoading = vi.fn();
    const setIsSearchOpen = vi.fn();

    await handleEnrollParticipant({
      member: { id: "m1" } as MemberResponseDto,
      activity,
      setActivity,
      setLoading,
      setIsSearchOpen,
    });

    await vi.waitFor(() => expect(setActivity).toHaveBeenCalled());
    expect(activity.enrollments).toHaveLength(1);
    expect(setIsSearchOpen).toHaveBeenCalledWith(false);
    expect(setLoading).toHaveBeenCalledWith(true);
    expect(setLoading).toHaveBeenCalledWith(false);
  });

  it("logs and rethrows when enrollment fails", async () => {
    postEnrollments.mockResolvedValue({ data: undefined });
    const consoleLog = vi.spyOn(console, "log").mockImplementation(() => {});

    await handleEnrollParticipant({
      member: { id: "m1" } as MemberResponseDto,
      activity: buildActivity(),
      setActivity: vi.fn(),
      setLoading: vi.fn(),
      setIsSearchOpen: vi.fn(),
    });

    await vi.waitFor(() => expect(consoleLog).toHaveBeenCalled());
    consoleLog.mockRestore();
  });
});

describe("handleUnenrollParticipant", () => {
  it("filters out the member's enrollment and updates state", () => {
    const activity = buildActivity({
      enrollments: [
        { member: { id: "m1" } },
        { member: { id: "m2" } },
      ] as ActivityResponseDto["enrollments"],
    });
    const setActivity = vi.fn();

    handleUnenrollParticipant("m1", activity, setActivity);

    expect(activity.enrollments).toEqual([{ member: { id: "m2" } }]);
    expect(setActivity).toHaveBeenCalledWith(activity);
  });
});

describe("handleMoveToParticipants", () => {
  it("clears isOnWaitingList for the matching enrollment", () => {
    const activity = buildActivity({
      enrollments: [
        { member: { id: "m1" }, isOnWaitingList: true },
      ] as ActivityResponseDto["enrollments"],
    });
    const setActivity = vi.fn();

    handleMoveToParticipants("m1", activity, setActivity);

    expect(activity.enrollments[0].isOnWaitingList).toBe(false);
    expect(setActivity).toHaveBeenCalledWith(activity);
  });

  it("does nothing when no matching enrollment is found", () => {
    const activity = buildActivity({ enrollments: [] });
    const setActivity = vi.fn();

    handleMoveToParticipants("m1", activity, setActivity);

    expect(setActivity).not.toHaveBeenCalled();
  });
});
