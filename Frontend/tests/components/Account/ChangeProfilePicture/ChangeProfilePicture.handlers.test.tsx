import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  handleProfilePictureDelete,
  handleProfilePictureUpload,
} from "~/components/Account/ChangeProfilePicture/ChangeProfilePicture.handlers";

const {
  deleteMembersByIdProfilePicture,
  postProfilepictureByIdProfilePicture,
} = vi.hoisted(() => ({
  deleteMembersByIdProfilePicture: vi.fn(),
  postProfilepictureByIdProfilePicture: vi.fn(),
}));

vi.mock("~/api", () => ({
  deleteMembersByIdProfilePicture,
  postProfilepictureByIdProfilePicture,
}));

vi.mock("react-hot-toast", () => ({
  // Mirror react-hot-toast's real behavior of internally handling the promise's rejection
  // (it updates the toast UI on failure) so a rejected saveProcess doesn't surface as an
  // unhandled rejection in the test.
  default: {
    promise: vi.fn((p: Promise<unknown>, opts: any) => {
      p.then(
        (data) => opts?.success?.(data),
        (err) => opts?.error?.(err),
      ).catch(() => {});
      return p;
    }),
  },
}));

function buildEvent(file?: File) {
  const input = document.createElement("input");
  input.type = "file";
  return {
    target: { files: file ? [file] : [] },
  } as unknown as React.ChangeEvent<HTMLInputElement>;
}

describe("handleProfilePictureUpload", () => {
  const reloadMock = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    Object.defineProperty(window, "location", {
      value: { reload: reloadMock },
      writable: true,
    });
  });

  it("does nothing when no file is selected", async () => {
    await handleProfilePictureUpload(buildEvent(undefined), "user-1");
    expect(postProfilepictureByIdProfilePicture).not.toHaveBeenCalled();
  });

  it("uploads the selected file and reloads the page on success", async () => {
    postProfilepictureByIdProfilePicture.mockResolvedValue({});
    const file = new File(["data"], "avatar.png", { type: "image/png" });

    await handleProfilePictureUpload(buildEvent(file), "user-1");

    expect(postProfilepictureByIdProfilePicture).toHaveBeenCalledWith({
      path: { id: "user-1" },
      body: { image: file },
    });
    await vi.waitFor(() => expect(reloadMock).toHaveBeenCalledTimes(1));
  });

  it("does not reload and logs when the upload fails", async () => {
    postProfilepictureByIdProfilePicture.mockResolvedValue({
      error: { title: "Boom" },
    });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const file = new File(["data"], "avatar.png", { type: "image/png" });

    await handleProfilePictureUpload(buildEvent(file), "user-1");

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    expect(reloadMock).not.toHaveBeenCalled();
    consoleError.mockRestore();
  });
});

describe("handleProfilePictureDelete", () => {
  const reloadMock = vi.fn();

  beforeEach(() => {
    vi.clearAllMocks();
    Object.defineProperty(window, "location", {
      value: { reload: reloadMock },
      writable: true,
    });
  });

  it("calls deleteMembersByIdProfilePicture and onSuccess callback when provided", async () => {
    deleteMembersByIdProfilePicture.mockResolvedValue({});
    const onSuccess = vi.fn();

    await handleProfilePictureDelete("user-1", onSuccess);

    expect(deleteMembersByIdProfilePicture).toHaveBeenCalledWith({
      path: { id: "user-1" },
    });
    await vi.waitFor(() => expect(onSuccess).toHaveBeenCalledTimes(1));
    expect(reloadMock).not.toHaveBeenCalled();
  });

  it("calls deleteMembersByIdProfilePicture and reloads page when no callback is provided", async () => {
    deleteMembersByIdProfilePicture.mockResolvedValue({});

    await handleProfilePictureDelete("user-1");

    expect(deleteMembersByIdProfilePicture).toHaveBeenCalledWith({
      path: { id: "user-1" },
    });
    await vi.waitFor(() => expect(reloadMock).toHaveBeenCalledTimes(1));
  });

  it("logs error and does not reload when deletion fails", async () => {
    deleteMembersByIdProfilePicture.mockResolvedValue({
      error: { title: "Cannot delete" },
    });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    await handleProfilePictureDelete("user-1");

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    expect(reloadMock).not.toHaveBeenCalled();
    consoleError.mockRestore();
  });
});
