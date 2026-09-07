import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import {
  copyWeekOverview,
  downloadPosters,
  handleCreateActivityClick,
  loadActivities,
} from "~/routes/activities/activities.handlers";

const { getActivities } = vi.hoisted(() => ({
  getActivities: vi.fn(),
}));

vi.mock("~/api", () => ({ getActivities }));

const { getEnv } = vi.hoisted(() => ({
  getEnv: vi.fn(() => "https://example.com"),
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
    location: "Enschede",
    dutchDescription: "Beschrijving",
    englishDescription: "Description",
    isWeeklyDrinks: false,
    ...overrides,
  } as ActivityResponseDto;
}

describe("loadActivities", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("sets activities on success", async () => {
    getActivities.mockResolvedValue({ data: [buildActivity()] });
    const setActivities = vi.fn();
    const setLoading = vi.fn();

    await loadActivities({ setLoading, setActivities });

    expect(setActivities).toHaveBeenCalledWith([
      expect.objectContaining({ id: 1 }),
    ]);
    expect(setLoading).toHaveBeenNthCalledWith(1, true);
    expect(setLoading).toHaveBeenNthCalledWith(2, false);
  });

  it("handles error when loading activities fails", async () => {
    getActivities.mockResolvedValue({ error: "Failed to load" });
    const setLoading = vi.fn();
    const setActivities = vi.fn();

    await loadActivities({ setLoading, setActivities });

    expect(setActivities).not.toHaveBeenCalled();
    expect(toastFn).toHaveBeenCalledWith("error", expect.anything());
    expect(setLoading).toHaveBeenLastCalledWith(false);
  });

  it("loads user enrolled activities when filter is enrolled", async () => {
    getActivities.mockResolvedValue({ data: [buildActivity({ id: 10 })] });
    const setActivities = vi.fn();
    const setLoading = vi.fn();

    await loadActivities({
      setLoading,
      setActivities,
      filter: "enrolled",
      userId: "user-123",
    });

    expect(getActivities).toHaveBeenCalledWith({
      query: {
        UserId: "user-123",
        IncludePast: false,
        IncludeFuture: true,
      },
    });
    expect(setActivities).toHaveBeenCalledWith([
      expect.objectContaining({ id: 10 }),
    ]);
  });

  it("loads user historical activities when filter is history", async () => {
    const act1 = buildActivity({
      id: 1,
      dateTimeStart: "2026-01-01T10:00:00Z",
    });
    const act2 = buildActivity({
      id: 2,
      dateTimeStart: "2026-05-01T10:00:00Z",
    });
    getActivities.mockResolvedValue({ data: [act1, act2] });
    const setActivities = vi.fn();
    const setLoading = vi.fn();

    await loadActivities({
      setLoading,
      setActivities,
      filter: "history",
      userId: "user-123",
    });

    expect(getActivities).toHaveBeenCalledWith({
      query: {
        UserId: "user-123",
        IncludePast: true,
        IncludeFuture: false,
      },
    });
    // Should be sorted most recent first: act2 (May) before act1 (Jan)
    expect(setActivities).toHaveBeenCalledWith([
      expect.objectContaining({ id: 2 }),
      expect.objectContaining({ id: 1 }),
    ]);
  });
});

describe("copyWeekOverview", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    Object.assign(navigator, {
      clipboard: { writeText: vi.fn().mockResolvedValue(undefined) },
    });
  });

  it("copies an English week overview to the clipboard", async () => {
    await copyWeekOverview("EN", [buildActivity()]);

    expect(navigator.clipboard.writeText).toHaveBeenCalled();
    expect(toastFn).toHaveBeenCalledWith("success", "copied_to_clipboard");
  });

  it("copies a Dutch week overview to the clipboard", async () => {
    await copyWeekOverview("NL", [buildActivity()]);

    const message = (navigator.clipboard.writeText as any).mock.calls[0][0];
    expect(message).toContain("Weekoverzicht");
  });

  it("includes the weekly drinks location when present", async () => {
    await copyWeekOverview("EN", [
      buildActivity({ isWeeklyDrinks: true, location: "Café" }),
    ]);

    const message = (navigator.clipboard.writeText as any).mock.calls[0][0];
    expect(message).toContain("Weekly Drinks");
  });

  it("handles weekly drinks without location in Dutch and without weekly drinks in Dutch", async () => {
    const targetWednesday = new Date();
    const currentDay = targetWednesday.getDay();
    if (currentDay > 3 || currentDay === 0) {
      targetWednesday.setDate(
        targetWednesday.getDate() + (8 - (currentDay || 7)) + 2,
      );
    } else {
      targetWednesday.setDate(targetWednesday.getDate() - (currentDay - 1) + 2);
    }
    targetWednesday.setHours(12, 0, 0, 0);

    await copyWeekOverview("NL", [
      buildActivity({
        isWeeklyDrinks: true,
        location: "",
        dateTimeStart: targetWednesday.toISOString(),
      }),
    ]);
    let message = (navigator.clipboard.writeText as any).mock.calls[0][0];
    expect(message).toContain("Locatie onbekend");

    await copyWeekOverview("NL", []);
    message = (navigator.clipboard.writeText as any).mock.calls[1][0];
    expect(message).toContain("Geen borrel deze week");
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
