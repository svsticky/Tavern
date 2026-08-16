import { beforeEach, describe, expect, it, vi } from "vitest";
import type { EnrollmentResponseDto } from "~/api";
import {
  handleMoveFromWaitinglist,
  handleWaitinglistUnenroll,
} from "~/components/Activity/Edit/EditParticipantsTile/EditWaitinglistParticipantTile/EditWaitinglistParticipantTile.handlers";

const {
  deleteEnrollmentsByActivityIdByMemberId,
  patchEnrollmentsByActivityIdByMemberId,
} = vi.hoisted(() => ({
  deleteEnrollmentsByActivityIdByMemberId: vi.fn(),
  patchEnrollmentsByActivityIdByMemberId: vi.fn(),
}));

vi.mock("~/api", () => ({
  deleteEnrollmentsByActivityIdByMemberId,
  patchEnrollmentsByActivityIdByMemberId,
}));

vi.mock("react-hot-toast", () => ({
  default: Object.assign((...args: unknown[]) => args, {
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

function buildEnrollment(): EnrollmentResponseDto {
  return {
    member: { id: "member-1", firstName: "Alice", lastName: "Smith" },
  } as EnrollmentResponseDto;
}

describe("handleWaitinglistUnenroll", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("calls onUnenroll on success", async () => {
    deleteEnrollmentsByActivityIdByMemberId.mockResolvedValue({});
    const onUnenroll = vi.fn();

    handleWaitinglistUnenroll({
      activityId: 1,
      enrollment: buildEnrollment(),
      setLoading: vi.fn(),
      onUnenroll,
    });

    await vi.waitFor(() => expect(onUnenroll).toHaveBeenCalled());
    expect(deleteEnrollmentsByActivityIdByMemberId).toHaveBeenCalledWith({
      path: { activityId: 1, memberId: "member-1" },
    });
  });

  it("logs and does not call onUnenroll on failure", async () => {
    deleteEnrollmentsByActivityIdByMemberId.mockResolvedValue({
      error: "fail",
    });
    const onUnenroll = vi.fn();
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    handleWaitinglistUnenroll({
      activityId: 1,
      enrollment: buildEnrollment(),
      setLoading: vi.fn(),
      onUnenroll,
    });

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    expect(onUnenroll).not.toHaveBeenCalled();
    consoleError.mockRestore();
  });
});

describe("handleMoveFromWaitinglist", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("calls onUnenroll on success", async () => {
    patchEnrollmentsByActivityIdByMemberId.mockResolvedValue({});
    const onUnenroll = vi.fn();

    handleMoveFromWaitinglist({
      activityId: 1,
      enrollment: buildEnrollment(),
      setLoading: vi.fn(),
      onUnenroll,
    });

    await vi.waitFor(() => expect(onUnenroll).toHaveBeenCalled());
    expect(patchEnrollmentsByActivityIdByMemberId).toHaveBeenCalledWith({
      path: { activityId: 1, memberId: "member-1" },
      body: [{ op: "replace", path: "/isOnWaitingList", value: false }],
    });
  });

  it("logs and does not call onUnenroll on failure", async () => {
    patchEnrollmentsByActivityIdByMemberId.mockResolvedValue({
      error: "fail",
    });
    const onUnenroll = vi.fn();
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    handleMoveFromWaitinglist({
      activityId: 1,
      enrollment: buildEnrollment(),
      setLoading: vi.fn(),
      onUnenroll,
    });

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    expect(onUnenroll).not.toHaveBeenCalled();
    consoleError.mockRestore();
  });
});
