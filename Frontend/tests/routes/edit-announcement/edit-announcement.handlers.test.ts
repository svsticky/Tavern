import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  handleAnnouncementSubmit,
  handleDeleteAnnouncement,
  loadAnnouncementData,
} from "~/routes/edit-announcement/edit-announcement.handlers";

const {
  deleteAnnouncementsById,
  getAnnouncementsById,
  postAnnouncements,
  putAnnouncementsById,
} = vi.hoisted(() => ({
  deleteAnnouncementsById: vi.fn(),
  getAnnouncementsById: vi.fn(),
  postAnnouncements: vi.fn(),
  putAnnouncementsById: vi.fn(),
}));

vi.mock("~/api", () => ({
  deleteAnnouncementsById,
  getAnnouncementsById,
  postAnnouncements,
  putAnnouncementsById,
}));

vi.mock("react-hot-toast", () => ({
  default: {
    promise: vi.fn((p: Promise<unknown>, opts: any) => {
      p.then(
        (data) => opts.success?.(data),
        (err) => opts.error?.(err),
      ).catch(() => {});
      return p;
    }),
  },
}));

function makeEvent(fields: Record<string, string>) {
  const form = document.createElement("form");
  Object.entries(fields).forEach(([name, value]) => {
    const input = document.createElement("input");
    input.name = name;
    input.value = value;
    form.appendChild(input);
  });
  return {
    preventDefault: vi.fn(),
    currentTarget: form,
  } as unknown as React.FormEvent<HTMLFormElement>;
}

describe("loadAnnouncementData", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("does nothing when not editing", async () => {
    const setLoading = vi.fn();
    await loadAnnouncementData({
      isEdit: false,
      id: undefined,
      setInitialData: vi.fn(),
      setLoading,
    });
    expect(getAnnouncementsById).not.toHaveBeenCalled();
    expect(setLoading).not.toHaveBeenCalled();
  });

  it("does nothing when editing but id is missing", async () => {
    await loadAnnouncementData({
      isEdit: true,
      id: undefined,
      setInitialData: vi.fn(),
      setLoading: vi.fn(),
    });
    expect(getAnnouncementsById).not.toHaveBeenCalled();
  });

  it("populates initial data and stops loading on success", async () => {
    getAnnouncementsById.mockResolvedValue({
      data: {
        titleDutch: "Titel",
        titleEnglish: "Title",
        contentDutch: "Inhoud",
        contentEnglish: "Content",
      },
    });
    const setInitialData = vi.fn();
    const setLoading = vi.fn();

    await loadAnnouncementData({
      isEdit: true,
      id: "1",
      setInitialData,
      setLoading,
    });

    expect(getAnnouncementsById).toHaveBeenCalledWith({ path: { id: 1 } });
    expect(setInitialData).toHaveBeenCalledWith({
      TitleDutch: "Titel",
      TitleEnglish: "Title",
      ContentDutch: "Inhoud",
      ContentEnglish: "Content",
    });
    expect(setLoading).toHaveBeenCalledWith(false);
  });

  it("stops loading without setting data when the response has no data", async () => {
    getAnnouncementsById.mockResolvedValue({});
    const setInitialData = vi.fn();
    const setLoading = vi.fn();

    await loadAnnouncementData({
      isEdit: true,
      id: "1",
      setInitialData,
      setLoading,
    });

    expect(setInitialData).not.toHaveBeenCalled();
    expect(setLoading).toHaveBeenCalledWith(false);
  });
});

describe("handleAnnouncementSubmit", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("updates an existing announcement and navigates on success", async () => {
    putAnnouncementsById.mockResolvedValue({});
    const navigate = vi.fn();
    const setSaving = vi.fn();
    const e = makeEvent({
      TitleDutch: "Titel",
      TitleEnglish: "Title",
      ContentDutch: "Inhoud",
      ContentEnglish: "Content",
    });

    await handleAnnouncementSubmit({
      e,
      isEdit: true,
      id: "3",
      setSaving,
      navigate,
    });

    expect(e.preventDefault).toHaveBeenCalled();
    expect(putAnnouncementsById).toHaveBeenCalledWith({
      path: { id: 3 },
      body: {
        titleDutch: "Titel",
        titleEnglish: "Title",
        contentDutch: "Inhoud",
        contentEnglish: "Content",
      },
    });
    await vi.waitFor(() =>
      expect(navigate).toHaveBeenCalledWith("/announcements"),
    );
    expect(setSaving).toHaveBeenCalledWith(true);
    expect(setSaving).toHaveBeenCalledWith(false);
  });

  it("creates a new announcement and navigates on success", async () => {
    postAnnouncements.mockResolvedValue({});
    const navigate = vi.fn();

    await handleAnnouncementSubmit({
      e: makeEvent({
        TitleDutch: "T",
        TitleEnglish: "T",
        ContentDutch: "C",
        ContentEnglish: "C",
      }),
      isEdit: false,
      id: undefined,
      setSaving: vi.fn(),
      navigate,
    });

    expect(postAnnouncements).toHaveBeenCalled();
    await vi.waitFor(() =>
      expect(navigate).toHaveBeenCalledWith("/announcements"),
    );
  });

  it("logs and does not navigate when the update fails", async () => {
    putAnnouncementsById.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const navigate = vi.fn();

    await handleAnnouncementSubmit({
      e: makeEvent({
        TitleDutch: "T",
        TitleEnglish: "T",
        ContentDutch: "C",
        ContentEnglish: "C",
      }),
      isEdit: true,
      id: "3",
      setSaving: vi.fn(),
      navigate,
    });

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    expect(navigate).not.toHaveBeenCalled();
    consoleError.mockRestore();
  });
});

describe("handleDeleteAnnouncement", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("deletes the announcement and navigates on success", async () => {
    deleteAnnouncementsById.mockResolvedValue({});
    const navigate = vi.fn();
    const setDeleting = vi.fn();

    await handleDeleteAnnouncement("4", setDeleting, navigate);

    expect(deleteAnnouncementsById).toHaveBeenCalledWith({
      path: { id: 4 },
    });
    await vi.waitFor(() =>
      expect(navigate).toHaveBeenCalledWith("/announcements"),
    );
    expect(setDeleting).toHaveBeenCalledWith(true);
    expect(setDeleting).toHaveBeenCalledWith(false);
  });

  it("logs and does not navigate when deletion fails", async () => {
    deleteAnnouncementsById.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const navigate = vi.fn();

    await handleDeleteAnnouncement("4", vi.fn(), navigate);

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    expect(navigate).not.toHaveBeenCalled();
    consoleError.mockRestore();
  });
});
