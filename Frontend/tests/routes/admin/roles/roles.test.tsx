import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Role, RoleAlias } from "~/api";
import Roles from "~/routes/admin/roles/roles";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

const {
  getRoles,
  postRoles,
  deleteRolesById,
  getRolesByIdPermissions,
  putRolesByIdPermissions,
  getRolealiases,
  postRolealiases,
  deleteRolealiasesById,
} = vi.hoisted(() => ({
  getRoles: vi.fn(),
  postRoles: vi.fn(),
  deleteRolesById: vi.fn(),
  getRolesByIdPermissions: vi.fn(),
  putRolesByIdPermissions: vi.fn(),
  getRolealiases: vi.fn(),
  postRolealiases: vi.fn(),
  deleteRolealiasesById: vi.fn(),
}));

vi.mock("~/api", () => ({
  getRoles,
  postRoles,
  deleteRolesById,
  getRolesByIdPermissions,
  putRolesByIdPermissions,
  getRolealiases,
  postRolealiases,
  deleteRolealiasesById,
}));

const toastErrorFn = vi.fn();
const toastSuccessFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: {
    error: (...args: unknown[]) => toastErrorFn(...args),
    success: (...args: unknown[]) => toastSuccessFn(...args),
  },
}));

function makeToken(overrides: Partial<TokenParsed> = {}): TokenParsed {
  return {
    locale: "en",
    UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
    access_level: "member",
    given_name: "Test",
    family_name: "User",
    name: "Test User",
    ...overrides,
  };
}

const boardAuthService = createMockAuthService({
  getTokenParsed: vi.fn(async () => makeToken({ is_admin: true })),
});

function makeRole(overrides: Partial<Role> = {}): Role {
  return { id: 1, name: "Chair", ...overrides };
}

function makeAlias(overrides: Partial<RoleAlias> = {}): RoleAlias {
  return { id: 1, roleId: 1, name: "Voorzitter", ...overrides };
}

describe("Roles (admin)", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    getRoles.mockResolvedValue({ data: [] });
    getRolealiases.mockResolvedValue({ data: [] });
  });

  it("shows a loading state, then the table once roles have loaded", async () => {
    getRoles.mockResolvedValue({ data: [makeRole()] });
    renderWithProviders(<Roles />);

    expect(screen.getByText("loading")).toBeInTheDocument();
    expect(await screen.findByText("Chair")).toBeInTheDocument();
  });

  it("shows an error toast when roles fail to load", async () => {
    getRoles.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    renderWithProviders(<Roles />);

    await waitFor(() => expect(toastErrorFn).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("hides the create button and row actions without the right permissions", async () => {
    getRoles.mockResolvedValue({ data: [makeRole()] });
    renderWithProviders(<Roles />);

    await screen.findByText("Chair");
    expect(document.querySelector("svg.lucide-plus")).toBeNull();
    expect(screen.queryByText("delete_role")).toBeNull();
    expect(screen.queryByText("manage_permissions")).toBeNull();
    expect(screen.queryByText("manage_aliases")).toBeNull();
  });

  it("shows the create button and row actions for a board member", async () => {
    getRoles.mockResolvedValue({ data: [makeRole()] });
    renderWithProviders(<Roles />, { authService: boardAuthService });

    await screen.findByText("Chair");
    expect(
      document.querySelector("svg.lucide-plus")?.closest("button"),
    ).toBeTruthy();
    expect(screen.getByText("delete_role")).toBeInTheDocument();
    expect(screen.getByText("manage_permissions")).toBeInTheDocument();
    expect(screen.getByText("manage_aliases")).toBeInTheDocument();
  });

  it("creates a role and reloads the list", async () => {
    getRoles.mockResolvedValue({ data: [] });
    postRoles.mockResolvedValue({ data: makeRole({ name: "Treasurer" }) });
    renderWithProviders(<Roles />, { authService: boardAuthService });

    await screen.findByText("no_roles_found");

    const plusButton = document
      .querySelector("svg.lucide-plus")
      ?.closest("button");
    fireEvent.click(plusButton!);

    const nameInput = await screen.findByLabelText("role_name");
    fireEvent.change(nameInput, { target: { value: "Treasurer" } });

    getRoles.mockResolvedValue({ data: [makeRole({ name: "Treasurer" })] });
    fireEvent.click(screen.getByRole("button", { name: "create_role" }));

    await waitFor(() =>
      expect(postRoles).toHaveBeenCalledWith({
        body: { name: "Treasurer" },
      }),
    );
    expect(await screen.findByText("Treasurer")).toBeInTheDocument();
  });

  it("shows an error toast when creating a role fails", async () => {
    getRoles.mockResolvedValue({ data: [] });
    postRoles.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    renderWithProviders(<Roles />, { authService: boardAuthService });

    await screen.findByText("no_roles_found");
    fireEvent.click(
      document.querySelector("svg.lucide-plus")!.closest("button")!,
    );
    fireEvent.change(await screen.findByLabelText("role_name"), {
      target: { value: "Treasurer" },
    });
    fireEvent.click(screen.getByRole("button", { name: "create_role" }));

    await waitFor(() => expect(toastErrorFn).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("deletes a role and reloads the list", async () => {
    getRoles.mockResolvedValue({ data: [makeRole()] });
    deleteRolesById.mockResolvedValue({});
    renderWithProviders(<Roles />, { authService: boardAuthService });

    await screen.findByText("Chair");
    getRoles.mockResolvedValue({ data: [] });
    fireEvent.click(screen.getByText("delete_role"));

    await waitFor(() =>
      expect(deleteRolesById).toHaveBeenCalledWith({ path: { id: 1 } }),
    );
    await screen.findByText("no_roles_found");
  });

  it("opens the permissions modal and loads/saves via the role-permission endpoints", async () => {
    getRoles.mockResolvedValue({ data: [makeRole()] });
    getRolesByIdPermissions.mockResolvedValue({ data: ["ViewFinances"] });
    putRolesByIdPermissions.mockResolvedValue({});
    renderWithProviders(<Roles />, { authService: boardAuthService });

    await screen.findByText("Chair");
    fireEvent.click(screen.getByText("manage_permissions"));

    expect(await screen.findByLabelText("View Finances")).toBeChecked();
    expect(getRolesByIdPermissions).toHaveBeenCalledWith({
      path: { id: 1 },
    });

    fireEvent.click(screen.getByText("save_permissions"));

    await waitFor(() =>
      expect(putRolesByIdPermissions).toHaveBeenCalledWith({
        path: { id: 1 },
        body: ["ViewFinances"],
      }),
    );
  });

  it("opens the aliases modal and shows only aliases for that role", async () => {
    getRoles.mockResolvedValue({ data: [makeRole()] });
    getRolealiases.mockResolvedValue({
      data: [
        makeAlias({ id: 1, roleId: 1, name: "Voorzitter" }),
        makeAlias({ id: 2, roleId: 2, name: "Penningmeester" }),
      ],
    });
    renderWithProviders(<Roles />, { authService: boardAuthService });

    await screen.findByText("Chair");
    fireEvent.click(screen.getByText("manage_aliases"));

    expect(await screen.findByText("Voorzitter")).toBeInTheDocument();
    expect(screen.queryByText("Penningmeester")).toBeNull();
  });

  it("adds a role alias and reloads the list", async () => {
    getRoles.mockResolvedValue({ data: [makeRole()] });
    getRolealiases.mockResolvedValue({ data: [] });
    postRolealiases.mockResolvedValue({ data: makeAlias() });
    renderWithProviders(<Roles />, { authService: boardAuthService });

    await screen.findByText("Chair");
    fireEvent.click(screen.getByText("manage_aliases"));

    const input = await screen.findByPlaceholderText("role_alias_placeholder");
    fireEvent.change(input, { target: { value: "Voorzitter" } });

    getRolealiases.mockResolvedValue({ data: [makeAlias()] });
    fireEvent.click(screen.getByText("add"));

    await waitFor(() =>
      expect(postRolealiases).toHaveBeenCalledWith({
        body: { name: "Voorzitter", roleId: 1 },
      }),
    );
    expect(await screen.findByText("Voorzitter")).toBeInTheDocument();
  });

  it("shows an error toast when adding a role alias fails", async () => {
    getRoles.mockResolvedValue({ data: [makeRole()] });
    getRolealiases.mockResolvedValue({ data: [] });
    postRolealiases.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    renderWithProviders(<Roles />, { authService: boardAuthService });

    await screen.findByText("Chair");
    fireEvent.click(screen.getByText("manage_aliases"));

    fireEvent.change(
      await screen.findByPlaceholderText("role_alias_placeholder"),
      { target: { value: "Voorzitter" } },
    );
    fireEvent.click(screen.getByText("add"));

    await waitFor(() => expect(toastErrorFn).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("removes a role alias and reloads the list", async () => {
    getRoles.mockResolvedValue({ data: [makeRole()] });
    getRolealiases.mockResolvedValue({ data: [makeAlias()] });
    deleteRolealiasesById.mockResolvedValue({});
    renderWithProviders(<Roles />, { authService: boardAuthService });

    await screen.findByText("Chair");
    fireEvent.click(screen.getByText("manage_aliases"));

    await screen.findByText("Voorzitter");
    getRolealiases.mockResolvedValue({ data: [] });
    fireEvent.click(screen.getByLabelText("remove"));

    await waitFor(() =>
      expect(deleteRolealiasesById).toHaveBeenCalledWith({ path: { id: 1 } }),
    );
    await waitFor(() =>
      expect(screen.queryByText("Voorzitter")).not.toBeInTheDocument(),
    );
  });
});
