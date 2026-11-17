import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import {
  copyWeekOverview,
  downloadPosters,
  handleCreateActivityClick,
} from "~/routes/activities/activities.handlers";

const { getEnv } = vi.hoisted(() => ({
  getEnv: vi.fn((key?: string) =>
    key === "AssociationTimeZone" ? "Europe/Amsterdam" : "https://example.com",
  ),
}));
vi.mock("~/util/config.utils", () => ({ getEnv }));

const { generateA3Pdf } = vi.hoisted(() => ({
  generateA3Pdf: vi.fn(),
}));
vi.mock("~/util/pdf.util", () => ({ generateA3Pdf }));

const toastFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: {
    error: (...args: unknown[]) => toastFn("error", ...args),
    success: (...args: unknown[]) => toastFn("success", ...args),
    loading: vi.fn(() => "toast-id"),
  },
}));

function buildActivity(
  overrides: Partial<ActivityResponseDto> = {},
): ActivityResponseDto {
  return {
    id: 1,
    name: "Party",
    dateTimeStart: "2026-08-18T10:00:00Z",
    dateTimeEnd: "2026-08-18T12:00:00Z",
    location: "Enschede",
    dutchDescription: "Beschrijving",
    englishDescription: "Description",
    isWeeklyDrinks: false,
    ...overrides,
  } as ActivityResponseDto;
}

describe("copyWeekOverview", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    // Pin "today" to the week of the test activity (Tuesday 18 Aug 2026),
    // otherwise the activity falls outside the overview as time passes.
    vi.useFakeTimers({ toFake: ["Date"] });
    vi.setSystemTime(new Date("2026-08-18T08:00:00Z"));
    Object.assign(navigator, {
      clipboard: { writeText: vi.fn().mockResolvedValue(undefined) },
    });
  });

  afterEach(() => {
    vi.useRealTimers();
  });

  it("copies an English week overview to the clipboard", async () => {
    await copyWeekOverview("EN", [buildActivity()]);

    expect(navigator.clipboard.writeText).toHaveBeenCalled();
    const message = (navigator.clipboard.writeText as any).mock.calls[0][0];
    expect(message).toContain("*Week overview*");
    expect(toastFn).toHaveBeenCalledWith("success", "copied_to_clipboard");
  });

  it("copies a Dutch week overview to the clipboard", async () => {
    await copyWeekOverview("NL", [buildActivity()]);

    const message = (navigator.clipboard.writeText as any).mock.calls[0][0];
    expect(message).toContain("*Weekoverzicht*");
    expect(message).toContain("_Wekelijkse borrel_");
  });

  it("includes the weekly drinks location when present in the target week", async () => {
    const now = new Date();
    const currentDay = now.getDay();
    const targetDate = new Date(now);
    if (currentDay > 3 || currentDay === 0) {
      targetDate.setDate(now.getDate() + (8 - (currentDay || 7)));
    } else {
      targetDate.setDate(now.getDate() - (currentDay - 1));
    }
    const drinkDate = new Date(targetDate);
    drinkDate.setDate(targetDate.getDate() + 1); // Tuesday

    await copyWeekOverview("EN", [
      buildActivity({
        isWeeklyDrinks: true,
        location: "Café",
        dateTimeStart: drinkDate.toISOString(),
        dateTimeEnd: drinkDate.toISOString(),
      }),
    ]);

    const message = (navigator.clipboard.writeText as any).mock.calls[0][0];
    expect(message).toContain("_Weekly drinks_");
    expect(message).toContain("Tuesday: Café");
  });

  it("targets next week from Thursday onwards", async () => {
    vi.setSystemTime(new Date("2026-08-20T08:00:00Z"));

    await copyWeekOverview("EN", [
      buildActivity({
        dateTimeStart: "2026-08-25T10:00:00Z",
        dateTimeEnd: "2026-08-25T12:00:00Z",
      }),
    ]);

    const message = (navigator.clipboard.writeText as any).mock.calls[0][0];
    expect(message).toContain("Description");
    expect(message).toContain("No drinks this week");
  });

  it("formats day activities with summary, short description, and link, and omits empty days", async () => {
    // Tuesday 19:00 - 22:00 in the association's time zone (CEST, UTC+2),
    // independent of the time zone the tests run in.
    const start = "2026-08-18T17:00:00Z";
    const end = "2026-08-18T20:00:00Z";

    const activity = buildActivity({
      name: "BactoberFest",
      location: "Poema",
      price: 9.5,
      shortEnglishDescription: "Short promo description",
      dateTimeStart: start,
      dateTimeEnd: end,
    });

    await copyWeekOverview("EN", [activity]);

    const message = (navigator.clipboard.writeText as any).mock.calls[0][0];
    expect(message).toContain("_Tuesday_");
    expect(message).toContain("BactoberFest | 19:00 - 22:00 | Poema | € 9.50");
    expect(message).toContain("Short promo description");
    expect(message).toContain("https://example.com/activities/1");
    // Monday should be omitted since it has no activities
    expect(message).not.toContain("_Monday_");
  });

  it("shows an error toast when the clipboard write fails", async () => {
    (navigator.clipboard.writeText as any).mockRejectedValue(
      new Error("denied"),
    );

    await copyWeekOverview("EN", []);

    expect(toastFn).toHaveBeenCalledWith("error", expect.anything());
  });
});

describe("downloadPosters", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows an error toast when there are no eligible posters", async () => {
    await downloadPosters([buildActivity({ showInKoala: false })], "token");

    expect(generateA3Pdf).not.toHaveBeenCalled();
    expect(toastFn).toHaveBeenCalledWith("error", expect.anything());
  });

  it("generates and downloads a PDF for eligible posters", async () => {
    generateA3Pdf.mockResolvedValue(undefined);

    await downloadPosters(
      [
        buildActivity({
          showInKoala: true,
          posterPath: "poster.jpg",
        } as Partial<ActivityResponseDto>),
      ],
      "token",
    );

    expect(generateA3Pdf).toHaveBeenCalled();
    expect(toastFn).toHaveBeenCalledWith(
      "success",
      "pdf_downloaded",
      expect.objectContaining({ id: "toast-id" }),
    );
  });

  it("shows an error toast when PDF generation fails", async () => {
    generateA3Pdf.mockRejectedValue(new Error("boom"));
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    await downloadPosters(
      [
        buildActivity({
          showInKoala: true,
          posterPath: "poster.jpg",
        } as Partial<ActivityResponseDto>),
      ],
      "token",
    );

    expect(toastFn).toHaveBeenCalledWith(
      "error",
      expect.anything(),
      expect.objectContaining({ id: "toast-id" }),
    );
    consoleError.mockRestore();
  });
});

describe("handleCreateActivityClick", () => {
  it("navigates to the create-activity page", () => {
    const navigate = vi.fn();
    handleCreateActivityClick(navigate);
    expect(navigate).toHaveBeenCalledWith("/activities/create");
  });
});
