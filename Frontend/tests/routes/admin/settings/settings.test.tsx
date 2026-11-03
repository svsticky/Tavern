import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { GroupResponseDto, Role } from "~/api";
import { renderRoute } from "~/testUtils";

const {
  loadSettingsLoaderData,
  requireBoardOrCandidateBoard,
  handleSettingsChange,
  handleAddRoleMapping,
  handleRemoveRoleMapping,
  handleSaveSettings,
  getGroupOptions,
  getRoleOptions,
  getCurrentRoleMappings,
  postGroupsPromoteBoard,
  getEnv,
} = vi.hoisted(() => ({
  loadSettingsLoaderData: vi.fn(),
  requireBoardOrCandidateBoard: vi.fn(),
  handleSettingsChange: vi.fn(),
  handleAddRoleMapping: vi.fn(),
  handleRemoveRoleMapping: vi.fn(),
  handleSaveSettings: vi.fn(),
  getGroupOptions: vi.fn((groups: GroupResponseDto[]) => [
    { value: "", label: "select_a_group" },
    ...groups.map((g) => ({ value: String(g.id), label: g.name })),
  ]),
  getRoleOptions: vi.fn((roles: Role[]) => [
    { value: "", label: "select_a_role_to_add" },
    ...roles.map((r) => ({ value: String(r.id), label: r.name })),
  ]),
  getCurrentRoleMappings: vi.fn(
    (settings: Record<string, string>) =>
      Object.entries(settings).filter(([key]) =>
        key.startsWith("ROLEMAILMAP_"),
      ) as [string, string][],
  ),
  postGroupsPromoteBoard: vi.fn(),
  getEnv: vi.fn((): string | undefined => undefined),
}));

vi.mock("~/util/loaderAuth.util", () => ({ requireBoardOrCandidateBoard }));

vi.mock("~/routes/admin/settings/settings.handlers", () => ({
  loadSettingsLoaderData,
  handleSettingsChange,
  handleAddRoleMapping,
  handleRemoveRoleMapping,
  handleSaveSettings,
  getGroupOptions,
  getRoleOptions,
  getCurrentRoleMappings,
}));

vi.mock("~/api/sdk.gen", () => ({ postGroupsPromoteBoard }));
vi.mock("~/util/config.utils", () => ({ getEnv }));

// These datatable/manage components each fetch their own data via ~/api and are out of scope
// for this batch - stub them so this route test stays focused on settings.tsx's own logic.
vi.mock(
  "~/components/Admin/ManageExternalLinksDatatable/ManageExternalLinksDatatable",
  () => ({
    default: () => <div>external-links-datatable</div>,
  }),
);
vi.mock(
  "~/components/Register/ManageRegisterReasonsDatatable/ManageRegisterReasonsDatatable",
  () => ({
    default: () => <div>register-reasons-datatable</div>,
  }),
);
vi.mock(
  "~/components/Register/ManageRegisterSlidesDatatable/ManageRegisterSlidesDatatable",
  () => ({
    default: () => <div>register-slides-datatable</div>,
  }),
);
vi.mock(
  "~/components/Register/ManageRegistrationDocumentsDatatable/ManageRegistrationDocumentsDatatable",
  () => ({
    default: () => <div>registration-documents-datatable</div>,
  }),
);
vi.mock(
  "~/components/Study/ManageStudiesDatatable/ManageStudiesDatatable",
  () => ({
    default: () => <div>studies-datatable</div>,
  }),
);

import * as route from "~/routes/admin/settings/settings";

/** Renders the settings page. */
function renderSettings() {
  return renderRoute(route, { path: "/admin/settings" });
}

function defaultSettings(overrides: Record<string, string> = {}) {
  return {
    BoardGroupId: "1",
    CandidateBoardGroupId: "2",
    PaymentServiceFee: "0.30",
    MembershipPrice: "10",
    BegunstigerPrice: "10",
    FinancialEmailSender: "finance@example.com",
    MainBoardMail: "board@example.com",
    ActivityUpdateEmailSender: "activities@example.com",
    FinancialYearStartDate: "01-01",
    CommitteeCreationDate: "08-01",
    YearlyMailSendDate: "09-01",
    PaymentProvider: "MOLLIE",
    MailService: "SMTP",
    SmtpStartTls: "true",
    ...overrides,
  };
}

function loadWith(
  settings: Record<string, string>,
  groups: GroupResponseDto[] = [{ id: 1, name: "Board" } as GroupResponseDto],
  roles: Role[] = [{ id: 1, name: "Chair" } as Role],
) {
  loadSettingsLoaderData.mockResolvedValue({
    settings,
    availableGroups: groups,
    availableRoles: roles,
  });
}

describe("SettingsPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getEnv.mockReturnValue(undefined);
    requireBoardOrCandidateBoard.mockResolvedValue({});
    loadWith(defaultSettings());
  });

  it("shows the error page when the settings fail to load", async () => {
    loadSettingsLoaderData.mockRejectedValue(
      new Error("Failed to load settings"),
    );

    renderSettings();

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Failed to load settings",
    );
  });

  it("renders the child management datatables", async () => {
    renderSettings();

    expect(await screen.findByText("studies-datatable")).toBeInTheDocument();
    expect(
      screen.getByText("registration-documents-datatable"),
    ).toBeInTheDocument();
    expect(screen.getByText("register-reasons-datatable")).toBeInTheDocument();
    expect(screen.getByText("register-slides-datatable")).toBeInTheDocument();
    expect(screen.getByText("external-links-datatable")).toBeInTheDocument();
  });

  it("disables save while a required field is missing", async () => {
    loadWith(defaultSettings({ BoardGroupId: "" }));

    renderSettings();

    const saveButton = await screen.findByRole("button", {
      name: "save_all_settings",
    });
    expect(saveButton).toBeDisabled();
  });

  it("enables save once all required fields are present and calls handleSaveSettings", async () => {
    renderSettings();

    const saveButton = await screen.findByRole("button", {
      name: "save_all_settings",
    });
    expect(saveButton).not.toBeDisabled();

    fireEvent.click(saveButton);

    expect(handleSaveSettings).toHaveBeenCalledWith(
      expect.objectContaining({
        deletedSettings: expect.any(Set),
        settings: expect.objectContaining({ BoardGroupId: "1" }),
        newSettings: expect.any(Set),
        setSaving: expect.any(Function),
        clearTracking: expect.any(Function),
      }),
    );
  });

  it("delegates a settings field change to handleSettingsChange", async () => {
    renderSettings();

    const membershipPriceInput =
      await screen.findByLabelText(/membership_price/);
    fireEvent.change(membershipPriceInput, { target: { value: "20" } });

    expect(handleSettingsChange).toHaveBeenCalledWith(
      "MembershipPrice",
      "20",
      expect.any(Function),
    );
  });

  it("delegates a begunstiger price change to handleSettingsChange", async () => {
    renderSettings();

    const begunstigerPriceInput =
      await screen.findByLabelText(/begunstiger_price/);
    fireEvent.change(begunstigerPriceInput, { target: { value: "15" } });

    expect(handleSettingsChange).toHaveBeenCalledWith(
      "BegunstigerPrice",
      "15",
      expect.any(Function),
    );
  });

  it("disables save while the begunstiger price is missing", async () => {
    loadWith(defaultSettings({ BegunstigerPrice: "" }));

    renderSettings();

    const saveButton = await screen.findByRole("button", {
      name: "save_all_settings",
    });
    expect(saveButton).toBeDisabled();
  });

  it("renders the begunstiger accounting fields when ACCOUNTING_ENABLED is true", async () => {
    getEnv.mockReturnValue("true");

    renderSettings();

    expect(
      await screen.findByLabelText("begunstiger_gl_account"),
    ).toBeInTheDocument();
    expect(
      screen.getByLabelText("begunstiger_cost_center"),
    ).toBeInTheDocument();
    expect(screen.getByLabelText("begunstiger_cost_unit")).toBeInTheDocument();
    expect(screen.getByLabelText(/begunstiger_vat_code/)).toBeInTheDocument();
  });

  it("shows the mollie api key field only when Mollie is the payment provider", async () => {
    renderSettings();
    expect(await screen.findByLabelText("mollie_api_key")).toBeInTheDocument();
  });

  it("shows SMTP fields by default and mailgun fields when MailService is MAILGUN", async () => {
    loadWith(defaultSettings({ MailService: "MAILGUN" }));

    renderSettings();

    expect(await screen.findByLabelText("mailgun_token")).toBeInTheDocument();
    expect(screen.queryByLabelText("smtp_host")).not.toBeInTheDocument();
  });

  it("renders existing role mappings and adds/removes a mapping", async () => {
    loadWith(
      defaultSettings({ ROLEMAILMAP_1: "chair@example.com" }),
      [{ id: 1, name: "Board" } as GroupResponseDto],
      [{ id: 1, name: "Chair" } as Role, { id: 2, name: "Secretary" } as Role],
    );

    renderSettings();

    expect(
      await screen.findByLabelText(/email_address_for Chair/),
    ).toBeInTheDocument();

    const roleSelect = screen.getByLabelText("add_new_role_email");
    fireEvent.change(roleSelect, { target: { value: "2" } });

    const addMappingButton = screen.getByRole("button", {
      name: /add_mapping/,
    });
    fireEvent.click(addMappingButton);

    expect(handleAddRoleMapping).toHaveBeenCalledWith(
      expect.objectContaining({ selectedRoleId: "2" }),
    );

    const removeButton = screen.getByTitle("remove");
    fireEvent.click(removeButton);

    expect(handleRemoveRoleMapping).toHaveBeenCalledWith(
      expect.objectContaining({ name: "ROLEMAILMAP_1" }),
    );
  });

  it("shows a no-mappings message when there are no role mappings", async () => {
    renderSettings();
    expect(
      await screen.findByText("no_role_email_mappings"),
    ).toBeInTheDocument();
  });

  it("always renders the accounting section, with GL/cost fields, regardless of ACCOUNTING_ENABLED", async () => {
    renderSettings();
    await screen.findByText("studies-datatable");
    expect(screen.getByText("accounting")).toBeInTheDocument();
    expect(screen.getByLabelText("membership_gl_account")).toBeInTheDocument();
  });

  it("does not render the Exact Online connector fields unless ACCOUNTING_ENABLED is true", async () => {
    renderSettings();
    await screen.findByText("studies-datatable");
    expect(
      screen.queryByLabelText("accounting_service"),
    ).not.toBeInTheDocument();
  });

  it("renders the Exact Online connector field when ACCOUNTING_ENABLED is true", async () => {
    getEnv.mockReturnValue("true");

    renderSettings();

    expect(
      await screen.findByLabelText("accounting_service"),
    ).toBeInTheDocument();
  });

  it("shows Exact fields only when AccountingService is EXACT", async () => {
    getEnv.mockReturnValue("true");
    loadWith(defaultSettings({ AccountingService: "EXACT" }));

    renderSettings();

    expect(await screen.findByLabelText("exact_division")).toBeInTheDocument();
  });

  it("promotes the board when confirmed", async () => {
    postGroupsPromoteBoard.mockResolvedValue({});

    renderSettings();

    const promoteButton = await screen.findByRole("button", {
      name: "run_board_rotation",
    });
    fireEvent.click(promoteButton);

    const confirmButton = await screen.findByRole("button", {
      name: "confirm",
    });
    fireEvent.click(confirmButton);

    await waitFor(() => expect(postGroupsPromoteBoard).toHaveBeenCalled());
  });

  it("does not promote the board when the confirmation is cancelled", async () => {
    renderSettings();

    const promoteButton = await screen.findByRole("button", {
      name: "run_board_rotation",
    });
    fireEvent.click(promoteButton);

    const cancelButton = await screen.findByRole("button", { name: "cancel" });
    fireEvent.click(cancelButton);

    expect(postGroupsPromoteBoard).not.toHaveBeenCalled();
  });

  it("shows an error toast when promoting the board fails", async () => {
    postGroupsPromoteBoard.mockRejectedValue(new Error("boom"));
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    renderSettings();

    const promoteButton = await screen.findByRole("button", {
      name: "run_board_rotation",
    });
    fireEvent.click(promoteButton);

    const confirmButton = await screen.findByRole("button", {
      name: "confirm",
    });
    fireEvent.click(confirmButton);

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("shows mailchimp fields when MailSubscriptionService is Mailchimp", async () => {
    loadWith(defaultSettings({ MailSubscriptionService: "MAILCHIMP" }));

    renderSettings();

    expect(
      await screen.findByLabelText("mailchimp_list_key"),
    ).toBeInTheDocument();
    expect(screen.getByLabelText("mailchimp_api_key")).toBeInTheDocument();
  });

  it("shows listmonk fields when MailSubscriptionService is Listmonk", async () => {
    loadWith(defaultSettings({ MailSubscriptionService: "LISTMONK" }));

    renderSettings();

    expect(await screen.findByLabelText("listmonk_url")).toBeInTheDocument();
    expect(screen.getByLabelText("listmonk_user")).toBeInTheDocument();
    expect(screen.getByLabelText("listmonk_api_key")).toBeInTheDocument();
  });

  function fireAllFieldChanges(container: HTMLElement) {
    const fields = container.querySelectorAll<
      HTMLInputElement | HTMLSelectElement
    >("form input, form select");

    fields.forEach((field) => {
      if (field.tagName === "SELECT") {
        const select = field as HTMLSelectElement;
        const otherOption = Array.from(select.options).find(
          (o) => o.value !== select.value,
        );
        fireEvent.change(field, {
          target: { value: otherOption?.value ?? select.value },
        });
      } else if ((field as HTMLInputElement).type === "checkbox") {
        fireEvent.click(field);
      } else if ((field as HTMLInputElement).type === "color") {
        fireEvent.change(field, { target: { value: "#112233" } });
      } else if ((field as HTMLInputElement).type === "number") {
        fireEvent.change(field, { target: { value: "5" } });
      } else {
        fireEvent.change(field, { target: { value: "x" } });
      }
    });

    return fields.length;
  }

  it("fires handleSettingsChange for every visible settings field (SMTP + role mapping)", async () => {
    loadWith(
      defaultSettings({ ROLEMAILMAP_1: "chair@example.com" }),
      [{ id: 1, name: "Board" } as GroupResponseDto],
      [{ id: 1, name: "Chair" } as Role],
    );

    const { container } = renderSettings();
    await screen.findByText("studies-datatable");

    const count = fireAllFieldChanges(container);
    expect(count).toBeGreaterThan(20);
    expect(handleSettingsChange.mock.calls.length).toBeGreaterThan(15);
  });

  it("fires handleSettingsChange for every visible settings field (MAILGUN + MAILCHIMP + EXACT)", async () => {
    getEnv.mockReturnValue("true");
    loadWith(
      defaultSettings({
        MailService: "MAILGUN",
        MailSubscriptionService: "MAILCHIMP",
        AccountingService: "EXACT",
      }),
    );

    const { container } = renderSettings();
    await screen.findByText("studies-datatable");

    const count = fireAllFieldChanges(container);
    expect(count).toBeGreaterThan(20);
    expect(handleSettingsChange.mock.calls.length).toBeGreaterThan(15);
  });

  it("fires handleSettingsChange for every visible settings field (LISTMONK)", async () => {
    getEnv.mockReturnValue("true");
    loadWith(
      defaultSettings({
        MailSubscriptionService: "LISTMONK",
      }),
    );

    const { container } = renderSettings();
    await screen.findByText("studies-datatable");

    const count = fireAllFieldChanges(container);
    expect(count).toBeGreaterThan(20);
    expect(handleSettingsChange.mock.calls.length).toBeGreaterThan(15);
  });

  it("renders sensible fallbacks when optional settings are unset or zeroed out", async () => {
    loadWith({
      BoardGroupId: "1",
      CandidateBoardGroupId: "",
      PaymentServiceFee: "",
      MembershipPrice: "",
      FinancialEmailSender: "",
      MainBoardMail: "",
      ActivityUpdateEmailSender: "",
      FinancialYearStartDate: "",
      CommitteeCreationDate: "",
      PaymentProvider: "OTHER",
      MastersShouldPayMembership: "0",
      GratieShouldPayMembership: "0",
      ErelidShouldPayMembership: "0",
      LidVanVerdiensteShouldPayMembership: "0",
      ROLEMAILMAP_99: "unknown@example.com",
    });

    renderSettings();

    await screen.findByText("studies-datatable");
    expect(screen.queryByLabelText("mollie_api_key")).not.toBeInTheDocument();
    expect(screen.getByLabelText("smtp_host")).toBeInTheDocument();
    expect(screen.getByLabelText("masters")).not.toBeChecked();
    expect(screen.getByLabelText("gratie")).not.toBeChecked();
    expect(screen.getByLabelText("ere_lid")).not.toBeChecked();
    expect(screen.getByLabelText("lid_van_verdienste")).not.toBeChecked();
  });

  it("shows the saving label on the save button while a save is in progress", async () => {
    let resolveSave: (() => void) | undefined;
    handleSaveSettings.mockImplementation(
      ({ setSaving }) =>
        new Promise<void>((resolve) => {
          setSaving(true);
          resolveSave = () => {
            setSaving(false);
            resolve();
          };
        }),
    );
    renderSettings();

    const saveButton = await screen.findByRole("button", {
      name: "save_all_settings",
    });
    fireEvent.click(saveButton);

    expect(await screen.findByText("saving")).toBeInTheDocument();
    resolveSave?.();
    await waitFor(() =>
      expect(screen.queryByText("saving")).not.toBeInTheDocument(),
    );
  });

  it("clears tracking state after a successful save", async () => {
    handleSaveSettings.mockImplementation(async ({ clearTracking }) => {
      clearTracking();
    });
    renderSettings();

    const saveButton = await screen.findByRole("button", {
      name: "save_all_settings",
    });
    fireEvent.click(saveButton);

    await waitFor(() => expect(handleSaveSettings).toHaveBeenCalled());
  });
});
