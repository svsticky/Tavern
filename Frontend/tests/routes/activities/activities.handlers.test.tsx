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

  it("shows an error toast on failure", async () => {
    getActivities.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const setActivities = vi.fn();

    await loadActivities({ setLoading: vi.fn(), setActivities });

    expect(setActivities).not.toHaveBeenCalled();
    expect(toastFn).toHaveBeenCalledWith("error", expect.anything());
    consoleError.mockRestore();
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
