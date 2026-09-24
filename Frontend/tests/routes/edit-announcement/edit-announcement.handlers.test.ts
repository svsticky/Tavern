import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  fetchAnnouncementFormData,
  handleAnnouncementSubmit,
  handleDeleteAnnouncement,
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

describe("fetchAnnouncementFormData", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("returns a blank form without fetching when creating", async () => {
    await expect(fetchAnnouncementFormData(undefined)).resolves.toEqual({
      TitleDutch: "",
      TitleEnglish: "",
      ContentDutch: "",
      ContentEnglish: "",
    });
    expect(getAnnouncementsById).not.toHaveBeenCalled();
  });

  it("returns the announcement's values when editing", async () => {
    getAnnouncementsById.mockResolvedValue({
      data: {
        titleDutch: "Titel",
        titleEnglish: "Title",
        contentDutch: "Inhoud",
        contentEnglish: "Content",
      },
    });

    await expect(fetchAnnouncementFormData("1")).resolves.toEqual({
      TitleDutch: "Titel",
      TitleEnglish: "Title",
      ContentDutch: "Inhoud",
      ContentEnglish: "Content",
    });
    expect(getAnnouncementsById).toHaveBeenCalledWith({ path: { id: 1 } });
  });

  it("throws the API error so React Router's error boundary handles it", async () => {
    getAnnouncementsById.mockResolvedValue({ error: new Error("fail") });

    await expect(fetchAnnouncementFormData("1")).rejects.toThrow("fail");
  });

  it("throws when the response has no data", async () => {
    getAnnouncementsById.mockResolvedValue({});

    await expect(fetchAnnouncementFormData("1")).rejects.toThrow(
      "Failed to load announcement",
    );
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
