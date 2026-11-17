import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import ChangeProfilePicture from "~/components/Account/ChangeProfilePicture/ChangeProfilePicture";

const { handleProfilePictureUpload, handleProfilePictureDelete } = vi.hoisted(
  () => ({
    handleProfilePictureUpload: vi.fn(),
    handleProfilePictureDelete: vi.fn(),
  }),
);

vi.mock(
  "~/components/Account/ChangeProfilePicture/ChangeProfilePicture.handlers",
  () => ({
    handleProfilePictureUpload,
    handleProfilePictureDelete,
  }),
);

describe("ChangeProfilePicture", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows the default avatar when there is no profile picture", () => {
    render(<ChangeProfilePicture userId="user-1" profilePictureSrc={null} />);

    expect(screen.getByAltText("Profile")).toHaveAttribute(
      "src",
      "/profile-picture.svg",
    );
  });

  it("shows the given profile picture", () => {
    render(
      <ChangeProfilePicture userId="user-1" profilePictureSrc="blob:picture" />,
    );

    expect(screen.getByAltText("Profile")).toHaveAttribute(
      "src",
      "blob:picture",
    );
  });

  it("renders children below the avatar", () => {
    render(
      <ChangeProfilePicture userId="user-1" profilePictureSrc={null}>
        <span>Change photo</span>
      </ChangeProfilePicture>,
    );
    expect(screen.getByText("Change photo")).toBeInTheDocument();
  });

  it("triggers the hidden file input when the avatar is clicked", async () => {
    const user = userEvent.setup();
    render(<ChangeProfilePicture userId="user-1" profilePictureSrc={null} />);

    const fileInput = document.querySelector(
      "input[type='file']",
    ) as HTMLInputElement;
    const clickSpy = vi.spyOn(fileInput, "click");

    await user.click(screen.getByAltText("Profile"));

    expect(clickSpy).toHaveBeenCalled();
  });

  it("calls handleProfilePictureUpload when a file is chosen", async () => {
    const user = userEvent.setup();
    render(<ChangeProfilePicture userId="user-1" profilePictureSrc={null} />);

    const fileInput = document.querySelector(
      "input[type='file']",
    ) as HTMLInputElement;
    const file = new File(["data"], "avatar.png", { type: "image/png" });
    await user.upload(fileInput, file);

    expect(handleProfilePictureUpload).toHaveBeenCalledTimes(1);
    expect(handleProfilePictureUpload.mock.calls[0][1]).toBe("user-1");
  });

  it.each([
    ["there is no profile picture", null],
    ["the default avatar is shown", "/profile-picture.svg"],
  ])("does not show the remove button when %s", (_, profilePictureSrc) => {
    render(
      <ChangeProfilePicture
        userId="user-1"
        profilePictureSrc={profilePictureSrc}
      />,
    );

    expect(
      screen.queryByRole("button", { name: /remove/i }),
    ).not.toBeInTheDocument();
  });

  it("shows the remove button and calls handleProfilePictureDelete when clicked", async () => {
    const user = userEvent.setup();
    render(
      <ChangeProfilePicture userId="user-1" profilePictureSrc="blob:picture" />,
    );

    await user.click(screen.getByRole("button", { name: /remove/i }));

    expect(handleProfilePictureDelete).toHaveBeenCalledWith("user-1");
  });
});
