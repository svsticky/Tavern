import { fireEvent, screen } from "@testing-library/react";
import { Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import AnnouncementFormPage from "~/routes/edit-announcement/edit-announcement";
import {
  handleAnnouncementSubmit,
  handleDeleteAnnouncement,
  loadAnnouncementData,
} from "~/routes/edit-announcement/edit-announcement.handlers";
import { renderWithProviders } from "~/testUtils";

vi.mock("~/routes/edit-announcement/edit-announcement.handlers", () => ({
  loadAnnouncementData: vi.fn(),
  handleAnnouncementSubmit: vi.fn(),
  handleDeleteAnnouncement: vi.fn(),
}));

function renderCreate() {
  return renderWithProviders(
    <Routes>
      <Route path="/announcements/create" element={<AnnouncementFormPage />} />
    </Routes>,
    { route: "/announcements/create" },
  );
}

function renderEdit(id = "3") {
  return renderWithProviders(
    <Routes>
      <Route
        path="/announcements/edit/:id"
        element={<AnnouncementFormPage />}
      />
    </Routes>,
    { route: `/announcements/edit/${id}` },
  );
}

describe("AnnouncementFormPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders the create form immediately when not editing", () => {
    renderCreate();
    expect(screen.getByLabelText(/title_nl/)).toBeInTheDocument();
    expect(screen.getByText("create")).toBeInTheDocument();
    expect(screen.queryByText("delete")).not.toBeInTheDocument();
  });

  it("shows a loading state while editing until data has loaded", () => {
    vi.mocked(loadAnnouncementData).mockImplementation(
      () => new Promise(() => {}),
    );
    renderEdit();
    expect(screen.getByText("loading")).toBeInTheDocument();
  });

  it("pre-fills the form once announcement data has loaded in edit mode", async () => {
    vi.mocked(loadAnnouncementData).mockImplementation(
      async ({ setInitialData, setLoading }) => {
        setInitialData({
          TitleDutch: "Titel",
          TitleEnglish: "Title",
          ContentDutch: "Inhoud",
          ContentEnglish: "Content",
        });
        setLoading(false);
      },
    );
    renderEdit();

    expect(await screen.findByDisplayValue("Titel")).toBeInTheDocument();
    expect(screen.getByText("update")).toBeInTheDocument();
    expect(screen.getByText("delete")).toBeInTheDocument();
  });

  it("calls handleAnnouncementSubmit on form submission", async () => {
    vi.mocked(loadAnnouncementData).mockImplementation(async ({ setLoading }) =>
      setLoading(false),
    );
    renderEdit();

    await screen.findByText("update");
    fireEvent.submit(screen.getByText("update").closest("form")!);

    expect(handleAnnouncementSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ isEdit: true, id: "3" }),
    );
  });

  it("calls handleDeleteAnnouncement when the delete button is clicked", async () => {
    vi.mocked(loadAnnouncementData).mockImplementation(async ({ setLoading }) =>
      setLoading(false),
    );
    renderEdit();

    fireEvent.click(await screen.findByText("delete"));

    expect(handleDeleteAnnouncement).toHaveBeenCalledWith(
      "3",
      expect.any(Function),
      expect.any(Function),
    );
  });
});
