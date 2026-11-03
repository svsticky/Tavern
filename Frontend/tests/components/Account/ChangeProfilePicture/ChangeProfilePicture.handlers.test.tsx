import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  handleProfilePictureDelete,
  handleProfilePictureUpload,
  loadProfilePictureSrc,
} from "~/components/Account/ChangeProfilePicture/ChangeProfilePicture.handlers";

const {
  deleteMembersByIdProfilePicture,
  getMembersByIdProfilePicture,
  postProfilepictureByIdProfilePicture,
} = vi.hoisted(() => ({
  deleteMembersByIdProfilePicture: vi.fn(),
  getMembersByIdProfilePicture: vi.fn(),
  postProfilepictureByIdProfilePicture: vi.fn(),
}));

vi.mock("~/api", () => ({
  deleteMembersByIdProfilePicture,
  getMembersByIdProfilePicture,
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

describe("loadProfilePictureSrc", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("returns an object URL for the member's profile picture", async () => {
    getMembersByIdProfilePicture.mockResolvedValue({
      status: 200,
      data: new Blob(["fake"], { type: "image/png" }),
    });

    await expect(loadProfilePictureSrc("user-1")).resolves.toMatch(/^blob:/);
    expect(getMembersByIdProfilePicture).toHaveBeenCalledWith({
      path: { id: "user-1" },
      responseType: "blob",
    });
  });

  it("returns the default avatar when the member has no profile picture", async () => {
    getMembersByIdProfilePicture.mockResolvedValue({
      status: 404,
      data: undefined,
    });

    await expect(loadProfilePictureSrc("user-1")).resolves.toBe(
      "/profile-picture.svg",
    );
  });

  it("logs the error and returns the default avatar when the request fails", async () => {
    getMembersByIdProfilePicture.mockRejectedValue(new Error("boom"));
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    await expect(loadProfilePictureSrc("user-1")).resolves.toBe(
      "/profile-picture.svg",
    );
    expect(consoleError).toHaveBeenCalled();
    consoleError.mockRestore();
  });
});

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

  it("does not reload and logs when the delete fails", async () => {
    deleteMembersByIdProfilePicture.mockResolvedValue({
      error: { title: "Delete failed" },
    });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const setProfilePictureSrc = vi.fn();

    await handleProfilePictureDelete("user-1", setProfilePictureSrc);

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    expect(setProfilePictureSrc).not.toHaveBeenCalled();
    expect(reloadMock).not.toHaveBeenCalled();
    consoleError.mockRestore();
  });
});
