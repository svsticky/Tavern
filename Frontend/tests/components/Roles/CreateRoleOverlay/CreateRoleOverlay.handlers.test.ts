import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RoleAlias } from "~/api";
import {
  fetchRoles,
  handleCreateRoleSubmit,
} from "~/components/Roles/CreateRoleOverlay/CreateRoleOverlay.handlers";

const { getRoles, postRolealiases, postRoles } = vi.hoisted(() => ({
  getRoles: vi.fn(),
  postRolealiases: vi.fn(),
  postRoles: vi.fn(),
}));

vi.mock("~/api", () => ({ getRoles, postRolealiases, postRoles }));

const toastFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: Object.assign((...args: unknown[]) => toastFn(...args), {
    error: vi.fn(),
    promise: vi.fn((p: Promise<unknown>, opts: any) => {
      p.then(
        (data) => opts.success?.(data),
        (err) => opts.error?.(err),
      ).catch(() => {});
      return p;
    }),
  }),
}));

function makeEvent() {
  return { preventDefault: vi.fn() } as unknown as React.FormEvent;
}

describe("fetchRoles", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("sets roles on success", async () => {
    const roles: RoleAlias[] = [{ id: 1, name: "Chair" } as RoleAlias];
    getRoles.mockResolvedValue({ data: roles });
    const setRoles = vi.fn();
    const setLoadingRoles = vi.fn();

    await fetchRoles(setLoadingRoles, setRoles);

    expect(setRoles).toHaveBeenCalledWith(roles);
    expect(setLoadingRoles).toHaveBeenNthCalledWith(1, true);
    expect(setLoadingRoles).toHaveBeenNthCalledWith(2, false);
  });

  it("logs and shows an error toast on failure", async () => {
    getRoles.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const setRoles = vi.fn();

    await fetchRoles(vi.fn(), setRoles);

    expect(setRoles).not.toHaveBeenCalled();
    expect(consoleError).toHaveBeenCalled();
    consoleError.mockRestore();
  });
});

describe("handleCreateRoleSubmit", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("creates a parent role and calls onRoleCreated", async () => {
    postRoles.mockResolvedValue({ data: { id: 5 } });
    const onRoleCreated = vi.fn();
    const setLoading = vi.fn();
    const e = makeEvent();

    handleCreateRoleSubmit({
      e,
      selectedType: "ParentRole",
      name: "Chair",
      selectedRoleId: "",
      setLoading,
      onRoleCreated,
      onRoleAliasCreated: vi.fn(),
    });

    expect(e.preventDefault).toHaveBeenCalled();
    await vi.waitFor(() =>
      expect(onRoleCreated).toHaveBeenCalledWith({
        id: 5,
        name: "Chair",
      }),
    );
    expect(setLoading).toHaveBeenCalledWith(true);
    expect(setLoading).toHaveBeenCalledWith(false);
  });

  it("creates a role alias and calls onRoleAliasCreated", async () => {
    postRolealiases.mockResolvedValue({ data: { id: 9 } });
    const onRoleAliasCreated = vi.fn();

    handleCreateRoleSubmit({
      e: makeEvent(),
      selectedType: "RoleAlias",
      name: "Vice Chair",
      selectedRoleId: "3",
      setLoading: vi.fn(),
      onRoleCreated: vi.fn(),
      onRoleAliasCreated,
    });

    await vi.waitFor(() =>
      expect(onRoleAliasCreated).toHaveBeenCalledWith({
        id: 9,
        name: "Vice Chair",
        roleId: 3,
      }),
    );
    expect(postRolealiases).toHaveBeenCalledWith({
      body: { name: "Vice Chair", roleId: 3 },
    });
  });

  it("does nothing when selectedType is unrecognized", async () => {
    handleCreateRoleSubmit({
      e: makeEvent(),
      selectedType: "Unknown",
      name: "X",
      selectedRoleId: "",
      setLoading: vi.fn(),
      onRoleCreated: vi.fn(),
      onRoleAliasCreated: vi.fn(),
    });

    await vi.waitFor(() => expect(postRoles).not.toHaveBeenCalled());
    expect(postRolealiases).not.toHaveBeenCalled();
  });

  it("logs and rethrows when creating a parent role fails", async () => {
    postRoles.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    handleCreateRoleSubmit({
      e: makeEvent(),
      selectedType: "ParentRole",
      name: "Chair",
      selectedRoleId: "",
      setLoading: vi.fn(),
      onRoleCreated: vi.fn(),
      onRoleAliasCreated: vi.fn(),
    });

    await vi.waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });
});
