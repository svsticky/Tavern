import { fireEvent, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { MemberMailinglistDto, MemberResponseDto } from "~/api/types.gen";
import ChangeAccountForm from "~/components/Account/ChangeProfileForm/ChangeAccountForm";
import { createMockAuthService, renderWithProviders } from "~/testUtils";

const { deleteMembersById } = vi.hoisted(() => ({
  deleteMembersById: vi.fn(),
}));

const {
  handleChangeEmail,
  handleChangePassword,
  handleConfigure2FA,
  handleSaveAccount,
  handleSubscriptionToggle,
} = vi.hoisted(() => ({
  handleChangeEmail: vi.fn(),
  handleChangePassword: vi.fn(),
  handleConfigure2FA: vi.fn(),
  handleSaveAccount: vi.fn(),
  handleSubscriptionToggle: vi.fn(),
}));

vi.mock("~/api", () => ({ deleteMembersById }));
vi.mock(
  "~/components/Account/ChangeProfileForm/ChangeAccountForm.handlers",
  async (importOriginal) => ({
    getSubscribedMailinglistIds: (
      await importOriginal<
        typeof import("~/components/Account/ChangeProfileForm/ChangeAccountForm.handlers")
      >()
    ).getSubscribedMailinglistIds,
    handleChangeEmail,
    handleChangePassword,
    handleConfigure2FA,
    handleSaveAccount,
    handleSubscriptionToggle,
  }),
);
vi.mock("react-hot-toast", () => ({
  default: { success: vi.fn(), error: vi.fn() },
}));

function buildMember(
  overrides: Partial<MemberResponseDto> = {},
): MemberResponseDto {
  return {
    id: "00000000-0000-0000-0000-000000000000",
    email: "member@example.com",
    phoneNumber: "0612345678",
    street: "Street",
    houseNumber: "1",
    postalCode: "1234AB",
    city: "Enschede",
    parentPhoneNumber: "",
    preferredLanguage: "EN",
    dateOfBirth: "2000-01-01",
    ...overrides,
  } as MemberResponseDto;
}

/** Renders the form for the member, with the given mailing lists. */
function renderForm(
  member: MemberResponseDto = buildMember(),
  {
    mailingLists = [],
    mailingListsUnavailable = false,
    ...options
  }: {
    mailingLists?: MemberMailinglistDto[];
    mailingListsUnavailable?: boolean;
  } & Parameters<typeof renderWithProviders>[1] = {},
) {
  return renderWithProviders(
    <ChangeAccountForm
      member={member}
      mailingLists={mailingLists}
      mailingListsUnavailable={mailingListsUnavailable}
    />,
    options,
  );
}

describe("ChangeAccountForm", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("pre-fills the form fields from the member", () => {
    renderForm();

    expect(screen.getByDisplayValue("0612345678")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Street")).toBeInTheDocument();
    expect(screen.getByDisplayValue("member@example.com")).toBeInTheDocument();
  });

  it("enables the save button once all required fields are filled for an adult member", () => {
    renderForm();
    expect(screen.getByRole("button", { name: "save" })).toBeEnabled();
  });

  it("requires a parent phone number for a member under 18 and disables save without it", () => {
    const under18 = new Date();
    under18.setFullYear(under18.getFullYear() - 16);
    renderForm(
      buildMember({
        dateOfBirth: under18.toISOString(),
        parentPhoneNumber: "",
      }),
    );

    expect(screen.getByRole("button", { name: "save" })).toBeDisabled();
  });

  it("renders the mailing lists with their subscriptions, and toggles them", async () => {
    const user = userEvent.setup();
    renderForm(buildMember(), {
      mailingLists: [
        { id: "list-1", name: "Newsletter", subscribed: false },
        { id: "list-2", name: "Alumni", subscribed: true },
      ] as MemberMailinglistDto[],
    });

    expect(screen.getByLabelText("Newsletter")).not.toBeChecked();
    expect(screen.getByLabelText("Alumni")).toBeChecked();

    await user.click(screen.getByLabelText("Newsletter"));
    expect(handleSubscriptionToggle).toHaveBeenCalledWith(
      "list-1",
      true,
      expect.any(Function),
    );
  });

  it("shows a message when the mailing lists are unavailable", () => {
    renderForm(buildMember(), { mailingListsUnavailable: true });

    expect(screen.getByText("mailinglists_unavailable")).toBeInTheDocument();
  });

  it("does not show the unavailable message when the mailing lists loaded", () => {
    renderForm();

    expect(
      screen.queryByText("mailinglists_unavailable"),
    ).not.toBeInTheDocument();
  });

  it("switches the preferred language when a language button is clicked", async () => {
    const user = userEvent.setup();
    renderForm();

    await user.click(screen.getByRole("button", { name: "dutch" }));
    expect(screen.getByRole("button", { name: "dutch" })).toHaveClass(
      "bg-(--board-primary)",
    );
  });

  it("calls handleSaveAccount when save is clicked", async () => {
    const user = userEvent.setup();
    const member = buildMember();
    renderForm(member);

    await user.click(screen.getByRole("button", { name: "save" }));

    expect(handleSaveAccount).toHaveBeenCalledWith(
      member.id,
      expect.objectContaining({ phoneNumber: member.phoneNumber }),
      [],
      expect.any(Function),
      expect.any(Function),
    );
  });

  it("calls handleChangePassword, handleChangeEmail, and handleConfigure2FA with the auth service", async () => {
    const user = userEvent.setup();
    const authService = createMockAuthService();
    renderForm(buildMember(), { authService });

    await user.click(screen.getByRole("button", { name: "change_password" }));
    expect(handleChangePassword).toHaveBeenCalledWith(authService);

    await user.click(screen.getByRole("button", { name: "setup_2fa" }));
    expect(handleConfigure2FA).toHaveBeenCalledWith(authService);

    await user.click(screen.getByText("change_email"));
    expect(handleChangeEmail).toHaveBeenCalledWith(authService);
  });

  it("does not delete the account when the confirmation dialog is cancelled", async () => {
    const user = userEvent.setup();
    renderForm();

    await user.click(screen.getByRole("button", { name: "delete_account" }));
    await user.click(screen.getByRole("button", { name: "cancel" }));

    expect(deleteMembersById).not.toHaveBeenCalled();
  });

  it("deletes the account and logs out when confirmed", async () => {
    const user = userEvent.setup();
    deleteMembersById.mockResolvedValue({ status: 204 });
    const authService = createMockAuthService();
    const member = buildMember();

    renderForm(member, { authService });

    await user.click(screen.getByRole("button", { name: "delete_account" }));
    await user.click(screen.getByRole("button", { name: "confirm" }));

    await waitFor(() =>
      expect(deleteMembersById).toHaveBeenCalledWith({
        path: { id: member.id },
      }),
    );
    await waitFor(() => expect(authService.logout).toHaveBeenCalled());
  });

  it("shows an error toast when account deletion fails", async () => {
    const user = userEvent.setup();
    deleteMembersById.mockResolvedValue({ status: 500, error: "fail" });
    const toast = (await import("react-hot-toast")).default;
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    renderForm();

    await user.click(screen.getByRole("button", { name: "delete_account" }));
    await user.click(screen.getByRole("button", { name: "confirm" }));

    await waitFor(() => expect(toast.error).toHaveBeenCalled());
    expect(consoleError).toHaveBeenCalled();
    consoleError.mockRestore();
  });

  it("updates every remaining contact/address field as the user edits it", () => {
    renderForm();

    const fields: [RegExp, string][] = [
      [/^phone_number/, "0698765432"],
      [/^parent_phone_number/, "0611223344"],
      [/^street/, "New street"],
      [/^house_number/, "42"],
      [/^postal_code/, "9999ZZ"],
      [/^city/, "Amsterdam"],
    ];
    fields.forEach(([label, value]) => {
      const input = screen.getByLabelText(label);
      fireEvent.change(input, { target: { value } });
      expect(input).toHaveValue(value);
    });
  });

  it("switches the preferred language to English when the English button is clicked", async () => {
    const user = userEvent.setup();
    renderForm(buildMember({ preferredLanguage: "NL" }));

    await user.click(screen.getByRole("button", { name: "english" }));
    expect(screen.getByRole("button", { name: "english" })).toHaveClass(
      "bg-(--board-primary)",
    );
  });
});
