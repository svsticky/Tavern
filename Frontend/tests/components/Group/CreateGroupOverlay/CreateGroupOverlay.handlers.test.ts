import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  handleCreateGroupSubmit,
  handleFileChange,
  resetCreateGroupForm,
} from "~/components/Group/CreateGroupOverlay/CreateGroupOverlay.handlers";

const { postGroups } = vi.hoisted(() => ({ postGroups: vi.fn() }));

vi.mock("~/api", () => ({ postGroups }));
vi.mock("react-hot-toast", () => ({
  default: {
    error: vi.fn(),
    promise: vi.fn((p: Promise<unknown>) => {
      p.catch(() => {});
      return p;
    }),
  },
}));

describe("handleFileChange", () => {
  it("updates the form data and creates an image preview URL for the selected file", () => {
    const file = new File(["data"], "group.png", { type: "image/png" });
    const setFormData = vi.fn();
    const setImagePreview = vi.fn();

    handleFileChange(
      {
        target: { files: [file] },
      } as unknown as React.ChangeEvent<HTMLInputElement>,
      { name: "", type: "Committee", groupPicture: null },
      setFormData,
      setImagePreview,
    );

    expect(setFormData).toHaveBeenCalledWith({
      name: "",
      type: "Committee",
      groupPicture: file,
    });
    expect(setImagePreview).toHaveBeenCalledWith(
      expect.stringContaining("blob:"),
    );
  });

  it("does nothing when no file is selected", () => {
    const setFormData = vi.fn();
    const setImagePreview = vi.fn();

    handleFileChange(
      {
        target: { files: [] },
      } as unknown as React.ChangeEvent<HTMLInputElement>,
      { name: "", type: "Committee", groupPicture: null },
      setFormData,
      setImagePreview,
    );

    expect(setFormData).not.toHaveBeenCalled();
    expect(setImagePreview).not.toHaveBeenCalled();
  });
});

describe("resetCreateGroupForm", () => {
  it("resets form data to defaults and clears the image preview", () => {
    const setFormData = vi.fn();
    const setImagePreview = vi.fn();

    resetCreateGroupForm(setFormData, setImagePreview);

    expect(setFormData).toHaveBeenCalledWith({
      name: "",
      type: "Committee",
      groupPicture: null,
    });
    expect(setImagePreview).toHaveBeenCalledWith(null);
  });
});

describe("handleCreateGroupSubmit", () => {
  const baseArgs = {
    e: { preventDefault: vi.fn() } as unknown as React.FormEvent,
    setLoading: vi.fn(),
    onSuccess: vi.fn(),
    resetForm: vi.fn(),
  };

  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("prevents the default form submission", () => {
    const preventDefault = vi.fn();
    handleCreateGroupSubmit({
      ...baseArgs,
      e: { preventDefault } as unknown as React.FormEvent,
      formData: { name: "", type: "Committee", groupPicture: null },
    });
    expect(preventDefault).toHaveBeenCalled();
  });

  it("shows a validation error and does not call the API when required fields are missing", async () => {
    const toast = (await import("react-hot-toast")).default;
    handleCreateGroupSubmit({
      ...baseArgs,
      formData: { name: "", type: "Committee", groupPicture: null },
    });

    await vi.waitFor(() => expect(toast.error).toHaveBeenCalled());
    expect(postGroups).not.toHaveBeenCalled();
  });

  it("creates the group and calls onSuccess + resetForm on success", async () => {
    postGroups.mockResolvedValue({});
    const file = new File(["data"], "group.png", { type: "image/png" });
    const onSuccess = vi.fn();
    const resetForm = vi.fn();

    handleCreateGroupSubmit({
      ...baseArgs,
      onSuccess,
      resetForm,
      formData: { name: "Web", type: "Committee", groupPicture: file },
    });

    await vi.waitFor(() => expect(onSuccess).toHaveBeenCalled());
    expect(postGroups).toHaveBeenCalledWith({
      body: { Name: "Web", Type: "Committee", GroupPicture: file },
    });
    expect(resetForm).toHaveBeenCalled();
  });

  it("does not call onSuccess when the API returns an error", async () => {
    postGroups.mockResolvedValue({ error: { title: "Boom" } });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const file = new File(["data"], "group.png", { type: "image/png" });
    const onSuccess = vi.fn();

    handleCreateGroupSubmit({
      ...baseArgs,
      onSuccess,
      formData: { name: "Web", type: "Committee", groupPicture: file },
    });

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    expect(onSuccess).not.toHaveBeenCalled();
    consoleError.mockRestore();
  });
});
