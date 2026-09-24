import { fireEvent, screen } from "@testing-library/react";
import { Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import AnnouncementFormPage, {
  clientLoader,
} from "~/routes/edit-announcement/edit-announcement";
import {
  fetchAnnouncementFormData,
  handleAnnouncementSubmit,
  handleDeleteAnnouncement,
} from "~/routes/edit-announcement/edit-announcement.handlers";
import { renderWithProviders } from "~/testUtils";

vi.mock("~/routes/edit-announcement/edit-announcement.handlers", () => ({
  fetchAnnouncementFormData: vi.fn(),
  handleAnnouncementSubmit: vi.fn(),
  handleDeleteAnnouncement: vi.fn(),
}));

const { requireTokenParsed } = vi.hoisted(() => ({
  requireTokenParsed: vi.fn(),
}));
vi.mock("~/util/loaderAuth.util", () => ({ requireTokenParsed }));

const { useLoaderData } = vi.hoisted(() => ({ useLoaderData: vi.fn() }));
vi.mock("react-router", async (importOriginal) => ({
  ...(await importOriginal<typeof import("react-router")>()),
  useLoaderData,
}));

const blank = {
  TitleDutch: "",
  TitleEnglish: "",
  ContentDutch: "",
  ContentEnglish: "",
};

function renderCreate() {
  useLoaderData.mockReturnValue({ initialData: blank });
  return renderWithProviders(
    <Routes>
      <Route path="/announcements/create" element={<AnnouncementFormPage />} />
    </Routes>,
    { route: "/announcements/create" },
  );
}

function renderEdit(initialData = blank, id = "3") {
  useLoaderData.mockReturnValue({ initialData });
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

describe("announcement form clientLoader", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    requireTokenParsed.mockResolvedValue({ UserId: "user-1" });
  });

  it("loads the announcement named by the URL", async () => {
    vi.mocked(fetchAnnouncementFormData).mockResolvedValue({
      ...blank,
      TitleDutch: "Titel",
    });

    const result = await clientLoader({ params: { id: "3" } });

    expect(fetchAnnouncementFormData).toHaveBeenCalledWith("3");
    expect(result.initialData.TitleDutch).toBe("Titel");
  });

  it("loads a blank form when creating", async () => {
    vi.mocked(fetchAnnouncementFormData).mockResolvedValue(blank);

    await clientLoader({ params: {} });

    expect(fetchAnnouncementFormData).toHaveBeenCalledWith(undefined);
  });

  it("propagates a failed load to React Router's error boundary", async () => {
    vi.mocked(fetchAnnouncementFormData).mockRejectedValue(new Error("fail"));

    await expect(clientLoader({ params: { id: "3" } })).rejects.toThrow("fail");
  });
});

describe("AnnouncementFormPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders the create form when not editing", () => {
    renderCreate();
    expect(screen.getByLabelText(/title_nl/)).toBeInTheDocument();
    expect(screen.getByText("create")).toBeInTheDocument();
    expect(screen.queryByText("delete")).not.toBeInTheDocument();
  });

  it("pre-fills the form with the loaded announcement in edit mode", () => {
    renderEdit({
      TitleDutch: "Titel",
      TitleEnglish: "Title",
      ContentDutch: "Inhoud",
      ContentEnglish: "Content",
    });

    expect(screen.getByDisplayValue("Titel")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Content")).toBeInTheDocument();
    expect(screen.getByText("update")).toBeInTheDocument();
    expect(screen.getByText("delete")).toBeInTheDocument();
  });

  it("calls handleAnnouncementSubmit on form submission", () => {
    renderEdit();

    fireEvent.submit(screen.getByText("update").closest("form")!);

    expect(handleAnnouncementSubmit).toHaveBeenCalledWith(
      expect.objectContaining({ isEdit: true, id: "3" }),
    );
  });

  it("calls handleDeleteAnnouncement when the delete button is clicked", () => {
    renderEdit();

    fireEvent.click(screen.getByText("delete"));

    expect(handleDeleteAnnouncement).toHaveBeenCalledWith(
      "3",
      expect.any(Function),
      expect.any(Function),
    );
  });
});
