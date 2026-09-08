import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import ChangeProfilePicture from "~/components/Account/ChangeProfilePicture/ChangeProfilePicture";

const { getMembersByIdProfilePicture, handleProfilePictureUpload } = vi.hoisted(
  () => ({
    getMembersByIdProfilePicture: vi.fn(),
    handleProfilePictureUpload: vi.fn(),
  }),
);

vi.mock("~/api", () => ({ getMembersByIdProfilePicture }));
vi.mock(
  "~/components/Account/ChangeProfilePicture/ChangeProfilePicture.handlers",
  () => ({
    handleProfilePictureUpload,
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
});
