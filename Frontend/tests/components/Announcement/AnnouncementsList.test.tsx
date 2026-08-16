import { screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { GetAnnouncementResponseDto } from "~/api";
import AnnouncementsList from "~/components/Announcement/AnnouncementsList";
import { renderWithProviders } from "~/testUtils";

vi.mock("~/components/Announcement/AnnouncementTile", () => ({
  default: ({ announcement }: { announcement: GetAnnouncementResponseDto }) => (
    <div>announcement-tile-{announcement.id}</div>
  ),
}));

describe("AnnouncementsList", () => {
  it("shows the no-content tile when there are no announcements", () => {
    renderWithProviders(<AnnouncementsList announcements={[]} />);
    expect(screen.getByText("no_announcements")).toBeInTheDocument();
  });

  it("renders an AnnouncementTile for each announcement", () => {
    renderWithProviders(
      <AnnouncementsList
        announcements={[
          { id: 1 } as GetAnnouncementResponseDto,
          { id: 2 } as GetAnnouncementResponseDto,
        ]}
      />,
    );
    expect(screen.getByText("announcement-tile-1")).toBeInTheDocument();
    expect(screen.getByText("announcement-tile-2")).toBeInTheDocument();
  });
});
