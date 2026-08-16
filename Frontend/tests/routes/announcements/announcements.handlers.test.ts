import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  handleCreateAnnouncementClick,
  loadAnnouncements,
} from "~/routes/announcements/announcements.handlers";

const { getAnnouncements } = vi.hoisted(() => ({
  getAnnouncements: vi.fn(),
}));

vi.mock("~/api", () => ({ getAnnouncements }));

vi.mock("react-hot-toast", () => ({
  default: { error: vi.fn(), success: vi.fn() },
  toast: { error: vi.fn(), success: vi.fn() },
}));

describe("loadAnnouncements", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("sets loading true then false, and populates announcements on success", async () => {
    const data = [{ id: 1, titleDutch: "Titel" }];
    getAnnouncements.mockResolvedValue({ data });
    const setLoading = vi.fn();
    const setAnnouncements = vi.fn();

    await loadAnnouncements({ setLoading, setAnnouncements });

    expect(setLoading).toHaveBeenNthCalledWith(1, true);
    expect(setLoading).toHaveBeenNthCalledWith(2, false);
    expect(setAnnouncements).toHaveBeenCalledWith(data);
  });

  it("shows an error toast and does not set announcements when the API returns an error", async () => {
    const { toast } = await import("react-hot-toast");
    getAnnouncements.mockResolvedValue({ error: "boom" });
    const setLoading = vi.fn();
    const setAnnouncements = vi.fn();

    await loadAnnouncements({ setLoading, setAnnouncements });

    expect(setAnnouncements).not.toHaveBeenCalled();
    expect(toast.error).toHaveBeenCalled();
    expect(setLoading).toHaveBeenLastCalledWith(false);
  });

  it("shows an error toast when the API returns no data", async () => {
    const { toast } = await import("react-hot-toast");
    getAnnouncements.mockResolvedValue({});
    const setLoading = vi.fn();
    const setAnnouncements = vi.fn();

    await loadAnnouncements({ setLoading, setAnnouncements });

    expect(setAnnouncements).not.toHaveBeenCalled();
    expect(toast.error).toHaveBeenCalled();
  });

  it("shows an error toast when the request rejects", async () => {
    const { toast } = await import("react-hot-toast");
    getAnnouncements.mockRejectedValue(new Error("network down"));
    const setLoading = vi.fn();
    const setAnnouncements = vi.fn();

    await loadAnnouncements({ setLoading, setAnnouncements });

    expect(setAnnouncements).not.toHaveBeenCalled();
    expect(toast.error).toHaveBeenCalled();
    expect(setLoading).toHaveBeenLastCalledWith(false);
  });
});

describe("handleCreateAnnouncementClick", () => {
  it("navigates to the announcement creation route", () => {
    const navigate = vi.fn();
    handleCreateAnnouncementClick(navigate);
    expect(navigate).toHaveBeenCalledWith("/announcements/create");
  });
});
