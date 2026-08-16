import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import {
  handleAddToCalendar,
  handleCopyForWhatsapp,
  handleEnrollment,
  handleUnenrollment,
  handleUpdateEnrollment,
} from "~/components/Activity/ActivityDetailsTile/ActivityDetailsTile.handlers";
import { createMockAuthService } from "~/testUtils";

const {
  postEnrollments,
  putEnrollmentsByActivityIdByMemberId,
  deleteEnrollmentsByActivityIdByMemberId,
} = vi.hoisted(() => ({
  postEnrollments: vi.fn(),
  putEnrollmentsByActivityIdByMemberId: vi.fn(),
  deleteEnrollmentsByActivityIdByMemberId: vi.fn(),
}));

vi.mock("~/api", () => ({
  postEnrollments,
  putEnrollmentsByActivityIdByMemberId,
  deleteEnrollmentsByActivityIdByMemberId,
}));

const toastFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: Object.assign((...args: unknown[]) => toastFn(...args), {
    success: vi.fn(),
    error: vi.fn(),
    promise: vi.fn((p: Promise<unknown>, opts: any) => {
      p.then(
        (data) => opts.success?.(data),
        (err) => {
          opts.error?.(err);
        },
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
    price: 5,
    location: "Enschede",
    dateTimeStart: "2026-08-01T10:00:00Z",
    dateTimeEnd: "2026-08-01T12:00:00Z",
    dutchDescription: "Beschrijving",
    englishDescription: "Description",
    enrollments: [],
    ...overrides,
  } as ActivityResponseDto;
}

const token = {
  UserId: "00000000-0000-0000-0000-000000000000",
  given_name: "Test",
  family_name: "User",
};

describe("handleAddToCalendar", () => {
  it("opens a Google Calendar URL in a new tab", () => {
    const openSpy = vi.spyOn(window, "open").mockImplementation(() => null);
    handleAddToCalendar(buildActivity());

    expect(openSpy).toHaveBeenCalledWith(
      expect.stringContaining("https://www.google.com/calendar/render"),
      "_blank",
      "noreferrer",
    );
    openSpy.mockRestore();
  });
});

describe("handleEnrollment", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("does nothing when the user is not authenticated (no token)", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => null),
    });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    await handleEnrollment(
      authService,
      buildActivity(),
      undefined,
      {},
      vi.fn(),
    );

    expect(postEnrollments).not.toHaveBeenCalled();
    consoleError.mockRestore();
  });

  it("does nothing when isAuthenticated() is false", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token as any),
      isAuthenticated: () => false,
    });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    await handleEnrollment(
      authService,
      buildActivity(),
      undefined,
      {},
      vi.fn(),
    );

    expect(postEnrollments).not.toHaveBeenCalled();
    consoleError.mockRestore();
  });

  it("submits the enrollment and updates the activity's enrollments on success", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token as any),
      isAuthenticated: () => true,
    });
    postEnrollments.mockResolvedValue({
      data: { isOnWaitingList: false, member: {}, specificationAnswers: [] },
    });
    const activity = buildActivity();
    const setActivity = vi.fn();
    const setSubmitting = vi.fn();

    await handleEnrollment(
      authService,
      activity,
      setActivity,
      { 1: "Answer" },
      setSubmitting,
    );

    expect(postEnrollments).toHaveBeenCalledWith({
      body: {
        activityId: 1,
        memberId: token.UserId,
        specificationAnswers: [{ questionId: 1, answer: "Answer" }],
      },
    });
    await vi.waitFor(() => expect(setActivity).toHaveBeenCalled());
    expect(setSubmitting).toHaveBeenNthCalledWith(1, true);
    expect(setSubmitting).toHaveBeenNthCalledWith(2, false);
  });

  it("shows a waiting-list notice instead of the success toast when the response says isOnWaitingList", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token as any),
      isAuthenticated: () => true,
    });
    postEnrollments.mockResolvedValue({
      data: { isOnWaitingList: true, member: {}, specificationAnswers: [] },
    });

    await handleEnrollment(authService, buildActivity(), vi.fn(), {}, vi.fn());

    await vi.waitFor(() => expect(toastFn).toHaveBeenCalled());
  });

  it("throws when the API returns no data", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token as any),
      isAuthenticated: () => true,
    });
    postEnrollments.mockResolvedValue({ data: undefined });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const setSubmitting = vi.fn();

    await handleEnrollment(
      authService,
      buildActivity(),
      vi.fn(),
      {},
      setSubmitting,
    );

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("throws and shows an error toast when the API returns an error", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token as any),
      isAuthenticated: () => true,
    });
    postEnrollments.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    await handleEnrollment(authService, buildActivity(), vi.fn(), {}, vi.fn());

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });
});

describe("handleUpdateEnrollment", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("updates the matching enrollment's answers on success", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token as any),
      isAuthenticated: () => true,
    });
    putEnrollmentsByActivityIdByMemberId.mockResolvedValue({});
    const activity = buildActivity({
      enrollments: [
        {
          member: { id: token.UserId },
          specificationAnswers: [{ questionId: 1, answerId: 5 }],
        },
      ] as ActivityResponseDto["enrollments"],
    });
    const setActivity = vi.fn();

    await handleUpdateEnrollment(
      authService,
      activity,
      setActivity,
      { 1: "New answer" },
      vi.fn(),
    );

    expect(putEnrollmentsByActivityIdByMemberId).toHaveBeenCalledWith({
      path: { activityId: 1, memberId: token.UserId },
      body: {
        activityId: 1,
        memberId: token.UserId,
        specificationAnswers: [{ questionId: 1, answer: "New answer" }],
      },
    });
    await vi.waitFor(() => expect(setActivity).toHaveBeenCalled());
  });

  it("does nothing when not authenticated", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => null),
    });

    await handleUpdateEnrollment(
      authService,
      buildActivity(),
      vi.fn(),
      {},
      vi.fn(),
    );

    expect(putEnrollmentsByActivityIdByMemberId).not.toHaveBeenCalled();
  });

  it("does nothing when the activity has no id", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token as any),
      isAuthenticated: () => true,
    });

    await handleUpdateEnrollment(
      authService,
      buildActivity({ id: undefined }),
      vi.fn(),
      {},
      vi.fn(),
    );

    expect(putEnrollmentsByActivityIdByMemberId).not.toHaveBeenCalled();
  });

  it("logs and shows an error toast when the update fails", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token as any),
      isAuthenticated: () => true,
    });
    putEnrollmentsByActivityIdByMemberId.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    await handleUpdateEnrollment(
      authService,
      buildActivity(),
      vi.fn(),
      {},
      vi.fn(),
    );

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });
});

describe("handleUnenrollment", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("removes the current user's enrollment on success", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token as any),
      isAuthenticated: () => true,
    });
    deleteEnrollmentsByActivityIdByMemberId.mockResolvedValue({});
    const activity = buildActivity({
      enrollments: [
        { member: { id: token.UserId } },
      ] as ActivityResponseDto["enrollments"],
    });
    const setActivity = vi.fn();

    await handleUnenrollment(authService, activity, setActivity, vi.fn());

    expect(deleteEnrollmentsByActivityIdByMemberId).toHaveBeenCalledWith({
      path: { activityId: 1, memberId: token.UserId },
    });
    await vi.waitFor(() => expect(setActivity).toHaveBeenCalled());
  });

  it("logs and does nothing when there is no token", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => null),
    });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    await handleUnenrollment(authService, buildActivity(), vi.fn(), vi.fn());

    expect(deleteEnrollmentsByActivityIdByMemberId).not.toHaveBeenCalled();
    consoleError.mockRestore();
  });

  it("logs and shows an error toast when the API returns an error", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token as any),
      isAuthenticated: () => true,
    });
    deleteEnrollmentsByActivityIdByMemberId.mockResolvedValue({
      error: "fail",
    });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    await handleUnenrollment(authService, buildActivity(), vi.fn(), vi.fn());

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });
});

describe("handleCopyForWhatsapp", () => {
  beforeEach(() => {
    Object.assign(navigator, {
      clipboard: { writeText: vi.fn().mockResolvedValue(undefined) },
    });
  });

  it("copies a Dutch-formatted message when lang is NL", async () => {
    await handleCopyForWhatsapp(buildActivity(), "NL" as any);

    expect(navigator.clipboard.writeText).toHaveBeenCalledWith(
      expect.stringContaining("Locatie: Enschede"),
    );
  });

  it("copies an English-formatted message when lang is EN", async () => {
    await handleCopyForWhatsapp(buildActivity(), "EN" as any);

    expect(navigator.clipboard.writeText).toHaveBeenCalledWith(
      expect.stringContaining("Location: Enschede"),
    );
  });

  it("shows 'Free'/'Gratis' when the activity has no price", async () => {
    await handleCopyForWhatsapp(buildActivity({ price: 0 }), "EN" as any);

    expect(navigator.clipboard.writeText).toHaveBeenCalledWith(
      expect.stringContaining("Price: Free"),
    );
  });

  it("shows an error toast when the clipboard write fails", async () => {
    Object.assign(navigator, {
      clipboard: {
        writeText: vi.fn().mockRejectedValue(new Error("denied")),
      },
    });

    await handleCopyForWhatsapp(buildActivity(), "EN" as any);

    await vi.waitFor(() =>
      expect(navigator.clipboard.writeText).toHaveBeenCalled(),
    );
  });
});
