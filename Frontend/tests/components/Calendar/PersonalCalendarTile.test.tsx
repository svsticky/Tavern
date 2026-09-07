import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import PersonalCalendarTile from "~/components/Calendar/PersonalCalendarTile/PersonalCalendarTile";

const { getCalendarsMe, postCalendarsMeRotate } = vi.hoisted(() => ({
  getCalendarsMe: vi.fn(),
  postCalendarsMeRotate: vi.fn(),
}));

vi.mock("~/api", () => ({
  getCalendarsMe,
  postCalendarsMeRotate,
}));

vi.mock("react-hot-toast", () => ({
  default: Object.assign(vi.fn(), {
    success: vi.fn(),
    error: vi.fn(),
    promise: vi.fn((p: Promise<unknown>, opts: any) => {
      p.then(
        (data) =>
          typeof opts?.success === "function"
            ? opts.success(data)
            : opts?.success,
        (err) => opts?.error?.(err),
      ).catch(() => {});
      return p;
    }),
  }),
}));

const FEED_URL = "https://api.tavern.svsticky.nl/calendars/abc-123";
const ROTATED_URL = "https://api.tavern.svsticky.nl/calendars/def-456";

describe("PersonalCalendarTile", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    Object.assign(navigator, {
      clipboard: { writeText: vi.fn().mockResolvedValue(undefined) },
    });
  });

  it("shows the member's feed URL once loaded", async () => {
    getCalendarsMe.mockResolvedValue({ data: { url: FEED_URL } });

    render(<PersonalCalendarTile />);

    expect(await screen.findByText(FEED_URL)).toBeInTheDocument();
  });

  it("always warns that the link is a secret and that it only covers Tavern enrollments", async () => {
    getCalendarsMe.mockResolvedValue({ data: { url: FEED_URL } });

    render(<PersonalCalendarTile />);

    expect(
      await screen.findByText("personal_calendar_secret_warning"),
    ).toBeInTheDocument();
    expect(
      screen.getByText("personal_calendar_scope_warning"),
    ).toBeInTheDocument();
  });

  it("copies the feed URL to the clipboard", async () => {
    getCalendarsMe.mockResolvedValue({ data: { url: FEED_URL } });

    render(<PersonalCalendarTile />);
    await screen.findByText(FEED_URL);
    await userEvent.click(screen.getByText("copy_calendar_link"));

    expect(navigator.clipboard.writeText).toHaveBeenCalledWith(FEED_URL);
  });

  it("replaces the displayed URL after resetting the link", async () => {
    getCalendarsMe.mockResolvedValue({ data: { url: FEED_URL } });
    postCalendarsMeRotate.mockResolvedValue({ data: { url: ROTATED_URL } });

    render(<PersonalCalendarTile />);
    await screen.findByText(FEED_URL);
    await userEvent.click(screen.getByText("reset_calendar_link"));

    expect(await screen.findByText(ROTATED_URL)).toBeInTheDocument();
    expect(screen.queryByText(FEED_URL)).not.toBeInTheDocument();
  });

  it("reports a failure to load the URL instead of showing a stale link", async () => {
    getCalendarsMe.mockResolvedValue({ error: new Error("boom") });

    render(<PersonalCalendarTile />);

    await waitFor(() =>
      expect(screen.getByText("loading_failed")).toBeInTheDocument(),
    );
    expect(screen.queryByText("copy_calendar_link")).not.toBeInTheDocument();
  });

  it("handles copy failure gracefully", async () => {
    getCalendarsMe.mockResolvedValue({ data: { url: FEED_URL } });
    (navigator.clipboard.writeText as any).mockRejectedValue(
      new Error("clipboard denied"),
    );

    render(<PersonalCalendarTile />);
    await screen.findByText(FEED_URL);
    await userEvent.click(screen.getByText("copy_calendar_link"));

    expect(navigator.clipboard.writeText).toHaveBeenCalledWith(FEED_URL);
  });

  it("handles rotate failure gracefully", async () => {
    getCalendarsMe.mockResolvedValue({ data: { url: FEED_URL } });
    postCalendarsMeRotate.mockResolvedValue({
      error: new Error("rotate failed"),
    });

    render(<PersonalCalendarTile />);
    await screen.findByText(FEED_URL);
    await userEvent.click(screen.getByText("reset_calendar_link"));

    expect(postCalendarsMeRotate).toHaveBeenCalled();
  });
});
