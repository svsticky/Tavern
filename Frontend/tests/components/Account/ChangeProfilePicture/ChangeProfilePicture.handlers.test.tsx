import { beforeEach, describe, expect, it, vi } from "vitest";
import { handleProfilePictureUpload } from "~/components/Account/ChangeProfilePicture/ChangeProfilePicture.handlers";

const { postProfilepictureByIdProfilePicture } = vi.hoisted(() => ({
  postProfilepictureByIdProfilePicture: vi.fn(),
}));

vi.mock("~/api", () => ({ postProfilepictureByIdProfilePicture }));

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
