import { describe, expect, it, vi } from "vitest";
import { handleCreateAnnouncementClick } from "~/routes/announcements/announcements.handlers";

describe("handleCreateAnnouncementClick", () => {
  it("navigates to the announcement creation route", () => {
    const navigate = vi.fn();
    handleCreateAnnouncementClick(navigate);
    expect(navigate).toHaveBeenCalledWith("/announcements/create");
  });
});
