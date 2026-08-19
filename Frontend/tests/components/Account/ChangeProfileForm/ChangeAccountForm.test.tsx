import { fireEvent, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { MemberResponseDto } from "~/api/types.gen";
import ChangeAccountForm from "~/components/Account/ChangeProfileForm/ChangeAccountForm";
import { createMockAuthService, renderWithProviders } from "~/testUtils";

const { getMembersByIdMailinglists, deleteMembersById } = vi.hoisted(() => ({
  getMembersByIdMailinglists: vi.fn(),
  deleteMembersById: vi.fn(),
}));

const {
  handleChangeEmail,
  handleChangePassword,
  handleConfigureMFA,
  handleSaveAccount,
  handleSubscriptionToggle,
} = vi.hoisted(() => ({
  handleChangeEmail: vi.fn(),
  handleChangePassword: vi.fn(),
  handleConfigureMFA: vi.fn(),
  handleSaveAccount: vi.fn(),
  handleSubscriptionToggle: vi.fn(),
}));

vi.mock("~/api", () => ({ getMembersByIdMailinglists, deleteMembersById }));
vi.mock(
  "~/components/Account/ChangeProfileForm/ChangeAccountForm.handlers",
  () => ({
    handleChangeEmail,
    handleChangePassword,
    handleConfigureMFA,
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

describe("ChangeAccountForm", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getMembersByIdMailinglists.mockResolvedValue({ data: [] });
  });

  it("pre-fills the form fields from the member", () => {
    renderWithProviders(<ChangeAccountForm member={buildMember()} />);

    expect(screen.getByDisplayValue("0612345678")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Street")).toBeInTheDocument();
    expect(screen.getByDisplayValue("member@example.com")).toBeInTheDocument();
  });

  it("enables the save button once all required fields are filled for an adult member", () => {
    renderWithProviders(<ChangeAccountForm member={buildMember()} />);
    expect(screen.getByRole("button", { name: "save" })).toBeEnabled();
  });

  it("requires a parent phone number for a member under 18 and disables save without it", () => {
    const under18 = new Date();
    under18.setFullYear(under18.getFullYear() - 16);
    renderWithProviders(
      <ChangeAccountForm
        member={buildMember({
          dateOfBirth: under18.toISOString(),
          parentPhoneNumber: "",
        })}
      />,
    );

    expect(screen.getByRole("button", { name: "save" })).toBeDisabled();
  });

  it("fetches and renders mailing lists, and toggles subscriptions", async () => {
    getMembersByIdMailinglists.mockResolvedValue({
      data: [{ id: "list-1", name: "Newsletter", subscribed: false }],
    });
    const user = userEvent.setup();
    renderWithProviders(<ChangeAccountForm member={buildMember()} />);

    await waitFor(() =>
      expect(screen.getByText("Newsletter")).toBeInTheDocument(),
    );

    await user.click(screen.getByLabelText("Newsletter"));
    expect(handleSubscriptionToggle).toHaveBeenCalledWith(
      "list-1",
      true,
      expect.any(Function),
    );
  });

  it("shows an error toast when fetching mailing lists fails", async () => {
    getMembersByIdMailinglists.mockResolvedValue({ error: { title: "Boom" } });
    const toast = (await import("react-hot-toast")).default;

    renderWithProviders(<ChangeAccountForm member={buildMember()} />);

    await waitFor(() => expect(toast.error).toHaveBeenCalled());
  });

  it("switches the preferred language when a language button is clicked", async () => {
    const user = userEvent.setup();
    renderWithProviders(<ChangeAccountForm member={buildMember()} />);

    await user.click(screen.getByRole("button", { name: "dutch" }));
    expect(screen.getByRole("button", { name: "dutch" })).toHaveClass(
      "bg-(--board-primary)",
    );
  });

  it("calls handleSaveAccount when save is clicked", async () => {
    const user = userEvent.setup();
    const member = buildMember();
    renderWithProviders(<ChangeAccountForm member={member} />);

    await user.click(screen.getByRole("button", { name: "save" }));

    expect(handleSaveAccount).toHaveBeenCalledWith(
      member.id,
      expect.objectContaining({ phoneNumber: member.phoneNumber }),
      [],
      expect.any(Function),
      expect.any(Function),
    );
  });

  it("calls handleChangePassword, handleChangeEmail, and handleConfigureMFA with the auth service", async () => {
    const user = userEvent.setup();
    const authService = createMockAuthService();
    renderWithProviders(<ChangeAccountForm member={buildMember()} />, {
      authService,
    });

    await user.click(screen.getByRole("button", { name: "change_password" }));
    expect(handleChangePassword).toHaveBeenCalledWith(authService);

    await user.click(screen.getByRole("button", { name: "setup_mfa" }));
    expect(handleConfigureMFA).toHaveBeenCalledWith(authService);

    await user.click(screen.getByText("change_email"));
    expect(handleChangeEmail).toHaveBeenCalledWith(authService);
  });

  it("does not delete the account when the confirmation dialog is cancelled", async () => {
    const user = userEvent.setup();
    vi.spyOn(window, "confirm").mockReturnValue(false);
    renderWithProviders(<ChangeAccountForm member={buildMember()} />);

    await user.click(screen.getByRole("button", { name: "delete_account" }));

    expect(deleteMembersById).not.toHaveBeenCalled();
  });

  it("deletes the account and logs out when confirmed", async () => {
    const user = userEvent.setup();
    vi.spyOn(window, "confirm").mockReturnValue(true);
    deleteMembersById.mockResolvedValue({ status: 204 });
    const authService = createMockAuthService();
    const member = buildMember();

    renderWithProviders(<ChangeAccountForm member={member} />, {
      authService,
    });

    await user.click(screen.getByRole("button", { name: "delete_account" }));

    await waitFor(() =>
      expect(deleteMembersById).toHaveBeenCalledWith({
        path: { id: member.id },
      }),
    );
    await waitFor(() => expect(authService.logout).toHaveBeenCalled());
  });

  it("shows an error toast when account deletion fails", async () => {
    const user = userEvent.setup();
    vi.spyOn(window, "confirm").mockReturnValue(true);
    deleteMembersById.mockResolvedValue({ status: 500, error: "fail" });
    const toast = (await import("react-hot-toast")).default;
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    renderWithProviders(<ChangeAccountForm member={buildMember()} />);

    await user.click(screen.getByRole("button", { name: "delete_account" }));

    await waitFor(() => expect(toast.error).toHaveBeenCalled());
    expect(consoleError).toHaveBeenCalled();
    consoleError.mockRestore();
  });

  it("updates every remaining contact/address field as the user edits it", () => {
    renderWithProviders(<ChangeAccountForm member={buildMember()} />);

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
    renderWithProviders(
      <ChangeAccountForm member={buildMember({ preferredLanguage: "NL" })} />,
    );

    await user.click(screen.getByRole("button", { name: "english" }));
    expect(screen.getByRole("button", { name: "english" })).toHaveClass(
      "bg-(--board-primary)",
    );
  });
});
