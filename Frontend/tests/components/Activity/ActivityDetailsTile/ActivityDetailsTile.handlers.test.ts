import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import {
  escapeIcsText,
  generateIcsContent,
  handleAddToCalendar,
  handleCopyForWhatsapp,
  handleDownloadIcs,
  handleEnrollment,
  handleUnenrollment,
  handleUpdateEnrollment,
} from "~/components/Activity/ActivityDetailsTile/ActivityDetailsTile.handlers";
import { createMockAuthService } from "~/testUtils";
import { formatDate } from "~/util/date.util";
import { capitalizeFirst } from "~/util/string.util";

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

  it("appends a disclaimer noting the copy is a one-time snapshot", () => {
    const openSpy = vi.spyOn(window, "open").mockImplementation(() => null);
    handleAddToCalendar(buildActivity());

    const url = openSpy.mock.calls[0][0] as string;
    expect(decodeURIComponent(url)).toContain("calendar_copy_disclaimer");
    openSpy.mockRestore();
  });

  it("uses the Dutch description when isDutch is true", () => {
    const openSpy = vi.spyOn(window, "open").mockImplementation(() => null);
    handleAddToCalendar(buildActivity(), true);

    const url = openSpy.mock.calls[0][0] as string;
    expect(decodeURIComponent(url)).toContain("Beschrijving");
    openSpy.mockRestore();
  });

  it("uses the English description when isDutch is false", () => {
    const openSpy = vi.spyOn(window, "open").mockImplementation(() => null);
    handleAddToCalendar(buildActivity(), false);

    const url = openSpy.mock.calls[0][0] as string;
    expect(decodeURIComponent(url)).toContain("Description");
    openSpy.mockRestore();
  });
});

describe("escapeIcsText", () => {
  it("escapes backslashes, semicolons, commas, and newlines", () => {
    const input = "Hello; World, this \\ is\na\r\ntest";
    expect(escapeIcsText(input)).toBe(
      "Hello\\; World\\, this \\\\ is\\na\\ntest",
    );
  });
});

describe("generateIcsContent", () => {
  it("generates a valid RFC 5545 iCalendar string", () => {
    const fixedNow = new Date("2026-09-01T12:00:00Z");
    const activity = buildActivity({
      id: 42,
      name: "Sticky BBQ",
      location: "Sticky Room, Buys Ballot",
      dateTimeStart: "2026-09-10T17:00:00Z",
      dateTimeEnd: "2026-09-10T22:00:00Z",
      dutchDescription: "Gezellige BBQ!",
      englishDescription: "Fun BBQ!",
    });

    const ics = generateIcsContent(activity, true, fixedNow);

    expect(ics).toContain("BEGIN:VCALENDAR");
    expect(ics).toContain("VERSION:2.0");
    expect(ics).toContain("PRODID:-//Study association Sticky//Tavern//EN");
    expect(ics).toContain("BEGIN:VEVENT");
    expect(ics).toContain("UID:activity-42@tavern.svsticky.nl");
    expect(ics).toContain("DTSTAMP:20260901T120000Z");
    expect(ics).toContain("DTSTART:20260910T170000Z");
    expect(ics).toContain("DTEND:20260910T220000Z");
    expect(ics).toContain("SUMMARY:Sticky BBQ");
    expect(ics).toContain("LOCATION:Sticky Room\\, Buys Ballot");
    expect(ics).toContain("Gezellige BBQ!");
    expect(ics).toContain("END:VEVENT");
    expect(ics).toContain("END:VCALENDAR");
  });

  it("uses the English description when isDutch is false", () => {
    const fixedNow = new Date("2026-09-01T12:00:00Z");
    const activity = buildActivity({
      dutchDescription: "Nederlands",
      englishDescription: "English text",
    });

    const ics = generateIcsContent(activity, false, fixedNow);
    expect(ics).toContain("English text");
  });
});

describe("handleDownloadIcs", () => {
  it("creates an anchor and downloads the .ics file", () => {
    const createObjectURLMock = vi
      .fn()
      .mockReturnValue("blob:http://localhost/mock-uuid");
    const revokeObjectURLMock = vi.fn();
    window.URL.createObjectURL = createObjectURLMock;
    window.URL.revokeObjectURL = revokeObjectURLMock;

    const clickSpy = vi.fn();
    const appendChildSpy = vi
      .spyOn(document.body, "appendChild")
      .mockImplementation((node) => {
        if (node instanceof HTMLAnchorElement) {
          node.click = clickSpy;
        }
        return node;
      });
    const removeChildSpy = vi
      .spyOn(document.body, "removeChild")
      .mockImplementation((node) => node);

    const activity = buildActivity({ name: "Lan Party 2026" });
    handleDownloadIcs(activity, true);

    expect(createObjectURLMock).toHaveBeenCalled();
    expect(appendChildSpy).toHaveBeenCalled();
    expect(clickSpy).toHaveBeenCalled();
    expect(removeChildSpy).toHaveBeenCalled();
    expect(revokeObjectURLMock).toHaveBeenCalledWith(
      "blob:http://localhost/mock-uuid",
    );

    appendChildSpy.mockRestore();
    removeChildSpy.mockRestore();
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

  it("only includes the end time (not a repeated date) when start and end are on the same day", async () => {
    const activity = buildActivity({
      dateTimeStart: "2026-08-01T10:00:00Z",
      dateTimeEnd: "2026-08-01T12:00:00Z",
    });

    await handleCopyForWhatsapp(activity, "EN" as any);

    const start = new Date(activity.dateTimeStart);
    const end = new Date(activity.dateTimeEnd);
    const expectedRange = `${capitalizeFirst(formatDate(start, "weekdayDate"))} ${formatDate(start, "timeOnly")} - ${formatDate(end, "timeOnly")}`;
    expect(navigator.clipboard.writeText).toHaveBeenCalledWith(
      expect.stringContaining(expectedRange),
    );
  });

  it("includes the full end date when start and end are on different days", async () => {
    const activity = buildActivity({
      dateTimeStart: "2026-08-01T10:00:00Z",
      dateTimeEnd: "2026-08-03T12:00:00Z",
    });

    await handleCopyForWhatsapp(activity, "EN" as any);

    const start = new Date(activity.dateTimeStart);
    const end = new Date(activity.dateTimeEnd);
    const expectedRange = `${capitalizeFirst(formatDate(start, "weekdayDate"))} ${formatDate(start, "timeOnly")} - ${capitalizeFirst(formatDate(end, "weekdayDate"))} ${formatDate(end, "timeOnly")}`;
    expect(navigator.clipboard.writeText).toHaveBeenCalledWith(
      expect.stringContaining(expectedRange),
    );
  });
});
