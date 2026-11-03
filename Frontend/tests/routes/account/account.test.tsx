import { screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { MemberMailinglistDto, MemberResponseDto } from "~/api";
import { loadMemberMailinglists } from "~/components/Account/ChangeProfileForm/ChangeAccountForm.handlers";
import { loadProfilePictureSrc } from "~/components/Account/ChangeProfilePicture/ChangeProfilePicture.handlers";
import * as route from "~/routes/account/account";
import { makeTokenParsed, renderRoute } from "~/testUtils";

const { useApp, requireTokenParsed } = vi.hoisted(() => ({
  useApp: vi.fn(),
  requireTokenParsed: vi.fn(),
}));

vi.mock("~/context/AppContext", () => ({ useApp }));
vi.mock("~/util/loaderAuth.util", () => ({ requireTokenParsed }));

vi.mock(
  "~/components/Account/ChangeProfilePicture/ChangeProfilePicture.handlers",
  () => ({ loadProfilePictureSrc: vi.fn() }),
);

vi.mock(
  "~/components/Account/ChangeProfileForm/ChangeAccountForm.handlers",
  () => ({ loadMemberMailinglists: vi.fn() }),
);

vi.mock(
  "~/components/Account/ChangeProfilePicture/ChangeProfilePicture",
  () => ({
    default: ({
      userId,
      profilePictureSrc,
      children,
    }: {
      userId: string;
      profilePictureSrc: string;
      children: React.ReactNode;
    }) => (
      <div
        data-testid="change-profile-picture"
        data-user-id={userId}
        data-src={profilePictureSrc}
      >
        {children}
      </div>
    ),
  }),
);

vi.mock("~/components/Account/ChangeProfileForm/ChangeAccountForm", () => ({
  default: ({
    member,
    mailingLists,
    mailingListsUnavailable,
  }: {
    member: MemberResponseDto;
    mailingLists: MemberMailinglistDto[];
    mailingListsUnavailable: boolean;
  }) => (
    <div>
      change-account-form-{member.firstName}-lists-{mailingLists.length}
      -unavailable-{String(mailingListsUnavailable)}
    </div>
  ),
}));

const token = makeTokenParsed({ given_name: "Jane", family_name: "Doe" });

const member = {
  id: token.UserId,
  firstName: "Jane",
  lastName: "Doe",
  studentNumber: "s1234567",
} as MemberResponseDto;

describe("AccountPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    requireTokenParsed.mockResolvedValue(token);
    useApp.mockReturnValue({ member });
    vi.mocked(loadProfilePictureSrc).mockResolvedValue("blob:picture");
    vi.mocked(loadMemberMailinglists).mockResolvedValue({
      mailingLists: [{ id: "list-1" }] as MemberMailinglistDto[],
      mailingListsUnavailable: false,
    });
  });

  it("loads the profile picture and mailing lists of the logged-in user", async () => {
    renderRoute(route, { withAppProvider: false });

    const picture = await screen.findByTestId("change-profile-picture");
    expect(picture).toHaveAttribute("data-user-id", token.UserId);
    expect(picture).toHaveAttribute("data-src", "blob:picture");
    expect(loadProfilePictureSrc).toHaveBeenCalledWith(token.UserId);
    expect(loadMemberMailinglists).toHaveBeenCalledWith(token.UserId);
  });

  it("renders the member's name and the account form with the mailing lists", async () => {
    renderRoute(route, { withAppProvider: false });

    expect(await screen.findByText("Jane Doe")).toBeInTheDocument();
    expect(screen.getByText("s1234567")).toBeInTheDocument();
    expect(
      screen.getByText(/change-account-form-Jane-lists-1\s*-unavailable-false/),
    ).toBeInTheDocument();
  });

  it("tells the account form when the mailing lists are unavailable", async () => {
    vi.mocked(loadMemberMailinglists).mockResolvedValue({
      mailingLists: [],
      mailingListsUnavailable: true,
    });
    renderRoute(route, { withAppProvider: false });

    expect(
      await screen.findByText(/lists-0\s*-unavailable-true/),
    ).toBeInTheDocument();
  });

  it("renders nothing while the member data has not loaded", async () => {
    useApp.mockReturnValue({ member: null });
    renderRoute(route, { withAppProvider: false });

    await vi.waitFor(() => expect(loadMemberMailinglists).toHaveBeenCalled());
    await new Promise((r) => setTimeout(r, 0));
    expect(
      screen.queryByTestId("change-profile-picture"),
    ).not.toBeInTheDocument();
    expect(screen.queryByText(/change-account-form-/)).not.toBeInTheDocument();
  });
});
