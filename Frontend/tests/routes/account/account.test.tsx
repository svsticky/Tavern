import { screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { MemberResponseDto } from "~/api";
import AccountPage from "~/routes/account/account";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

const { useApp } = vi.hoisted(() => ({
  useApp: vi.fn(),
}));

vi.mock("~/context/AppContext", () => ({ useApp }));

vi.mock(
  "~/components/Account/ChangeProfilePicture/ChangeProfilePicture",
  () => ({
    default: ({
      userId,
      children,
    }: {
      userId: string;
      children: React.ReactNode;
    }) => (
      <div data-testid="change-profile-picture" data-user-id={userId}>
        {children}
      </div>
    ),
  }),
);

vi.mock("~/components/Account/ChangeProfileForm/ChangeAccountForm", () => ({
  default: ({ member }: { member: MemberResponseDto }) => (
    <div>change-account-form-{member.firstName}</div>
  ),
}));

const token: TokenParsed = {
  locale: "en",
  UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
  access_level: "member",
  given_name: "Jane",
  family_name: "Doe",
  name: "Jane Doe",
};

describe("AccountPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    useApp.mockReturnValue({ member: null });
  });

  it("renders nothing while the user id has not loaded", () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(() => new Promise<TokenParsed | null>(() => {})),
    });
    const { container } = renderWithProviders(<AccountPage />, {
      authService,
      withAppProvider: false,
    });
    expect(container).toBeEmptyDOMElement();
  });

  it("logs an error when the token has no UserId", async () => {
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => null),
    });
    renderWithProviders(<AccountPage />, {
      authService,
      withAppProvider: false,
    });

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("renders the profile picture uploader once the user id loads", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<AccountPage />, {
      authService,
      withAppProvider: false,
    });

    const picture = await screen.findByTestId("change-profile-picture");
    expect(picture).toHaveAttribute("data-user-id", token.UserId);
  });

  it("does not render the account form while member data has not loaded", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<AccountPage />, {
      authService,
      withAppProvider: false,
    });

    await screen.findByTestId("change-profile-picture");
    expect(screen.queryByText(/change-account-form-/)).not.toBeInTheDocument();
  });

  it("renders the member's name and account form once member data is available", async () => {
    useApp.mockReturnValue({
      member: {
        firstName: "Jane",
        lastName: "Doe",
        studentNumber: "s1234567",
      } as MemberResponseDto,
    });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<AccountPage />, {
      authService,
      withAppProvider: false,
    });

    expect(await screen.findByText("Jane Doe")).toBeInTheDocument();
    expect(screen.getByText("s1234567")).toBeInTheDocument();
    expect(screen.getByText("change-account-form-Jane")).toBeInTheDocument();
  });
});
