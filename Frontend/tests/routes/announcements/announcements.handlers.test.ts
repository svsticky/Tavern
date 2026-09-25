import { describe, expect, it, vi } from "vitest";
import {
  handleCreateAnnouncementClick,
  loadAnnouncements,
} from "~/routes/announcements/announcements.handlers";

const { getAnnouncements } = vi.hoisted(() => ({
  getAnnouncements: vi.fn(),
}));

vi.mock("~/api", () => ({ getAnnouncements }));

describe("loadAnnouncements", () => {
  it("returns the announcements on success", async () => {
    const data = [{ id: 1, titleDutch: "Titel" }];
    getAnnouncements.mockResolvedValue({ data });

    await expect(loadAnnouncements()).resolves.toEqual(data);
  });

  it("throws when the API returns an error", async () => {
    getAnnouncements.mockResolvedValue({ error: "boom" });

    await expect(loadAnnouncements()).rejects.toThrow(
      "Failed to load announcements",
    );
  });

  it("throws when the API returns no data", async () => {
    getAnnouncements.mockResolvedValue({});

    await expect(loadAnnouncements()).rejects.toThrow(
      "Failed to load announcements",
    );
  });
});

describe("handleCreateAnnouncementClick", () => {
  it("navigates to the announcement creation route", () => {
    const navigate = vi.fn();
    handleCreateAnnouncementClick(navigate);
    expect(navigate).toHaveBeenCalledWith("/announcements/create");
  });
});
