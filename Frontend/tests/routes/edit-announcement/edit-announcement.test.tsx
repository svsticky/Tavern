import { fireEvent, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import * as route from "~/routes/edit-announcement/edit-announcement";
import {
  handleAnnouncementSubmit,
  handleDeleteAnnouncement,
} from "~/routes/edit-announcement/edit-announcement.handlers";
import { makeTokenParsed, renderRoute } from "~/testUtils";

const { getAnnouncementsById, requireTokenParsed } = vi.hoisted(() => ({
  getAnnouncementsById: vi.fn(),
  requireTokenParsed: vi.fn(),
}));

vi.mock("~/api", () => ({ getAnnouncementsById }));
vi.mock("~/util/loaderAuth.util", () => ({ requireTokenParsed }));
vi.mock("~/routes/edit-announcement/edit-announcement.handlers", () => ({
  handleAnnouncementSubmit: vi.fn(),
  handleDeleteAnnouncement: vi.fn(),
}));

function renderCreate() {
  return renderRoute(route, { path: "/announcements/create" });
}

function renderEdit(id = "3") {
  return renderRoute(route, {
    path: "/announcements/edit/:id",
    route: `/announcements/edit/${id}`,
  });
}

describe("AnnouncementFormPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    requireTokenParsed.mockResolvedValue(makeTokenParsed());
    getAnnouncementsById.mockResolvedValue({
      data: {
        id: 3,
        titleDutch: "Titel",
        titleEnglish: "Title",
        contentDutch: "Inhoud",
        contentEnglish: "Content",
      },
    });
  });

  it("renders an empty create form when not editing", async () => {
    renderCreate();

    expect(await screen.findByLabelText(/title_nl/)).toHaveValue("");
    expect(screen.getByText("create")).toBeInTheDocument();
    expect(screen.queryByText("delete")).not.toBeInTheDocument();
    expect(getAnnouncementsById).not.toHaveBeenCalled();
  });

  it("pre-fills the form with the announcement in edit mode", async () => {
    renderEdit();

    expect(await screen.findByDisplayValue("Titel")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Content")).toBeInTheDocument();
    expect(screen.getByText("update")).toBeInTheDocument();
    expect(screen.getByText("delete")).toBeInTheDocument();
    expect(getAnnouncementsById).toHaveBeenCalledWith({ path: { id: 3 } });
  });

  it("shows the error page when the announcement fails to load", async () => {
    getAnnouncementsById.mockResolvedValue({ error: "fail" });
    renderEdit();

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Failed to load announcements",
    );
  });

  it("calls handleAnnouncementSubmit on form submission", async () => {
    renderEdit();

    fireEvent.submit((await screen.findByText("update")).closest("form")!);

    expect(handleAnnouncementSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ isEdit: true, id: "3" }),
    );
  });

  it("calls handleDeleteAnnouncement when the delete button is clicked", async () => {
    renderEdit();

    fireEvent.click(await screen.findByText("delete"));

    expect(handleDeleteAnnouncement).toHaveBeenCalledWith(
      "3",
      expect.any(Function),
      expect.any(Function),
    );
  });
});
