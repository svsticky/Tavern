import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import ChangeProfilePicture, {
  getStoredProfileFrame,
} from "~/components/Account/ChangeProfilePicture/ChangeProfilePicture";

const {
  getMembersByIdProfilePicture,
  handleProfilePictureUpload,
  handleProfilePictureDelete,
} = vi.hoisted(() => ({
  getMembersByIdProfilePicture: vi.fn(),
  handleProfilePictureUpload: vi.fn(),
  handleProfilePictureDelete: vi.fn(),
}));

vi.mock("~/api", () => ({ getMembersByIdProfilePicture }));
vi.mock(
  "~/components/Account/ChangeProfilePicture/ChangeProfilePicture.handlers",
  () => ({
    handleProfilePictureUpload,
    handleProfilePictureDelete,
  }),
);

describe("ChangeProfilePicture", () => {
  it("shows the default avatar while no profile picture is available", async () => {
    getMembersByIdProfilePicture.mockResolvedValue({
      status: 404,
      data: undefined,
    });

    render(<ChangeProfilePicture userId="user-1" />);

    await waitFor(() =>
      expect(screen.getByAltText("Profile")).toHaveAttribute(
        "src",
        "/profile-picture.svg",
      ),
    );
  });

  it("logs an error and falls back to the default avatar when the fetch fails", async () => {
    getMembersByIdProfilePicture.mockRejectedValue(new Error("boom"));
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    render(<ChangeProfilePicture userId="user-1" />);

    await waitFor(() =>
      expect(screen.getByAltText("Profile")).toHaveAttribute(
        "src",
        "/profile-picture.svg",
      ),
    );
    expect(consoleError).toHaveBeenCalled();
    consoleError.mockRestore();
  });

  it("shows the fetched profile picture as an object URL", async () => {
    getMembersByIdProfilePicture.mockResolvedValue({
      status: 200,
      data: new Blob(["fake"], { type: "image/png" }),
    });

    render(<ChangeProfilePicture userId="user-1" />);

    await waitFor(() =>
      expect(screen.getByAltText("Profile")).toHaveAttribute(
        "src",
        expect.stringContaining("blob:"),
      ),
    );
  });

  it("renders children below the avatar", () => {
    getMembersByIdProfilePicture.mockResolvedValue({ status: 404 });
    render(
      <ChangeProfilePicture userId="user-1">
        <span>Change photo</span>
      </ChangeProfilePicture>,
    );
    expect(screen.getByText("Change photo")).toBeInTheDocument();
  });

  it("triggers the hidden file input when the avatar is clicked", async () => {
    getMembersByIdProfilePicture.mockResolvedValue({ status: 404 });
    const user = userEvent.setup();
    render(<ChangeProfilePicture userId="user-1" />);

    const fileInput = document.querySelector(
      "input[type='file']",
    ) as HTMLInputElement;
    const clickSpy = vi.spyOn(fileInput, "click");

    await user.click(screen.getByAltText("Profile"));

    expect(clickSpy).toHaveBeenCalled();
  });

  it("calls handleProfilePictureUpload when a file is chosen", async () => {
    getMembersByIdProfilePicture.mockResolvedValue({ status: 404 });
    const user = userEvent.setup();
    render(<ChangeProfilePicture userId="user-1" />);

    const fileInput = document.querySelector(
      "input[type='file']",
    ) as HTMLInputElement;
    const file = new File(["data"], "avatar.png", { type: "image/png" });
    await user.upload(fileInput, file);

    expect(handleProfilePictureUpload).toHaveBeenCalledTimes(1);
    expect(handleProfilePictureUpload.mock.calls[0][1]).toBe("user-1");
  });

  it("renders a gold border when isHonoraryOrMerit is true", async () => {
    getMembersByIdProfilePicture.mockResolvedValue({ status: 404 });
    const { container } = render(
      <ChangeProfilePicture userId="user-1" isHonoraryOrMerit={true} />,
    );
    await waitFor(() =>
      expect(screen.getByAltText("Profile")).toBeInTheDocument(),
    );
    expect(container.querySelector(".border-amber-400")).toBeInTheDocument();
  });

  it("shows remove button when a custom profile picture is uploaded and triggers handleProfilePictureDelete", async () => {
    getMembersByIdProfilePicture.mockResolvedValue({
      status: 200,
      data: new Blob(["custom-picture"], { type: "image/png" }),
    });
    const user = userEvent.setup();

    handleProfilePictureDelete.mockImplementation(
      (_userId: string, onSuccess?: () => void) => {
        onSuccess?.();
      },
    );

    render(<ChangeProfilePicture userId="user-1" />);

    const removeBtn = await screen.findByRole("button", {
      name: /remove_profile_picture|remove profile picture/i,
    });
    expect(removeBtn).toBeInTheDocument();

    await user.click(removeBtn);
    expect(handleProfilePictureDelete).toHaveBeenCalledTimes(1);
    expect(handleProfilePictureDelete.mock.calls[0][0]).toBe("user-1");
  });

  it("does not show remove button when default profile picture is active", async () => {
    getMembersByIdProfilePicture.mockResolvedValue({ status: 404 });
    render(<ChangeProfilePicture userId="user-1" />);

    await waitFor(() =>
      expect(screen.getByAltText("Profile")).toHaveAttribute(
        "src",
        "/profile-picture.svg",
      ),
    );
    expect(
      screen.queryByRole("button", {
        name: /remove_profile_picture|remove profile picture/i,
      }),
    ).not.toBeInTheDocument();
  });

  it("allows switching profile frames and persists to localStorage", async () => {
    getMembersByIdProfilePicture.mockResolvedValue({ status: 404 });
    const user = userEvent.setup();
    const { container } = render(
      <ChangeProfilePicture
        userId="user-frame-test"
        isHonoraryOrMerit={true}
      />,
    );

    await waitFor(() =>
      expect(screen.getByAltText("Profile")).toBeInTheDocument(),
    );

    // Initial is gold because isHonoraryOrMerit is true
    expect(container.querySelector(".border-amber-400")).toBeInTheDocument();

    // Click Default frame button
    const defaultBtn = screen.getByRole("button", {
      name: /frame_default|^default$/i,
    });
    await user.click(defaultBtn);

    expect(container.querySelector(".border-white")).toBeInTheDocument();
    expect(localStorage.getItem("profile_frame_user-frame-test")).toBe(
      "default",
    );

    // Click Primary frame button
    const primaryBtn = screen.getByRole("button", {
      name: /frame_primary|sticky primary/i,
    });
    await user.click(primaryBtn);

    expect(localStorage.getItem("profile_frame_user-frame-test")).toBe(
      "primary",
    );

    // Click Gold frame button
    const goldBtn = screen.getByRole("button", {
      name: /frame_gold/i,
    });
    await user.click(goldBtn);

    expect(localStorage.getItem("profile_frame_user-frame-test")).toBe("gold");
  });

  describe("getStoredProfileFrame", () => {
    it("returns default when gold is saved but user is not honorary or merit", () => {
      localStorage.setItem("profile_frame_u1", "gold");
      expect(getStoredProfileFrame("u1", false)).toBe("default");
      localStorage.removeItem("profile_frame_u1");
    });

    it("returns gold when gold is saved and user is honorary or merit", () => {
      localStorage.setItem("profile_frame_u1", "gold");
      expect(getStoredProfileFrame("u1", true)).toBe("gold");
      localStorage.removeItem("profile_frame_u1");
    });

    it("returns primary when primary is saved", () => {
      localStorage.setItem("profile_frame_u1", "primary");
      expect(getStoredProfileFrame("u1", false)).toBe("primary");
      localStorage.removeItem("profile_frame_u1");
    });

    it("handles localStorage errors gracefully", () => {
      const getItemSpy = vi
        .spyOn(Storage.prototype, "getItem")
        .mockImplementation(() => {
          throw new Error("Storage failure");
        });
      expect(getStoredProfileFrame("u1", false)).toBe("default");
      expect(getStoredProfileFrame("u1", true)).toBe("gold");
      getItemSpy.mockRestore();
    });

    it("handles undefined window environment", () => {
      const originalWindow = global.window;
      // @ts-expect-error - testing SSR
      delete global.window;
      expect(getStoredProfileFrame("u1", false)).toBe("default");
      expect(getStoredProfileFrame("u1", true)).toBe("gold");
      global.window = originalWindow;
    });
  });
});
