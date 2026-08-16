import { beforeEach, describe, expect, it, vi } from "vitest";
import type { GroupResponseDto, Role } from "~/api";

const {
  deleteSettingsById,
  getGroups,
  getRoles,
  getSettings,
  patchSettingsById,
  postSettings,
} = vi.hoisted(() => ({
  deleteSettingsById: vi.fn(),
  getGroups: vi.fn(),
  getRoles: vi.fn(),
  getSettings: vi.fn(),
  patchSettingsById: vi.fn(),
  postSettings: vi.fn(),
}));

vi.mock("~/api", () => ({
  deleteSettingsById,
  getGroups,
  getRoles,
  getSettings,
  patchSettingsById,
  postSettings,
}));

vi.mock("react-hot-toast", () => ({
  default: {
    success: vi.fn(),
    error: vi.fn(),
    promise: vi.fn((p) => p.catch(() => {})),
  },
}));

import toast from "react-hot-toast";
import {
  getCurrentRoleMappings,
  getGroupOptions,
  getRoleOptions,
  handleAddRoleMapping,
  handleRemoveRoleMapping,
  handleSaveSettings,
  handleSettingsChange,
  loadSettingsPageData,
} from "~/routes/admin/settings/settings.handlers";
import { BOARD_THEME_SETTINGS_UPDATED_EVENT } from "~/util/theme-settings";

describe("loadSettingsPageData", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("loads and merges settings, groups, and roles", async () => {
    getSettings.mockResolvedValue({
      data: [
        { name: "BoardGroupId", value: "1" },
        { name: "BoardPrimary", value: "#ABCDEF80" },
        { name: "NoValue", value: null },
      ],
    });
    getGroups.mockResolvedValue({ data: [{ id: 1, name: "Board" }] });
    getRoles.mockResolvedValue({ data: [{ id: 1, name: "Chair" }] });

    const setSettings = vi.fn();
    const setAvailableGroups = vi.fn();
    const setAvailableRoles = vi.fn();
    const setLoading = vi.fn();

    await loadSettingsPageData({
      setSettings,
      setAvailableGroups,
      setAvailableRoles,
      setLoading,
    });

    expect(getGroups).toHaveBeenCalledWith({
      query: { IncludeInactive: true },
    });
    expect(setSettings).toHaveBeenCalledWith({
      BoardGroupId: "1",
      // 8-digit hex color (with alpha) gets normalized down to 6-digit RGB.
      BoardPrimary: "#ABCDEF",
      NoValue: "",
    });
    expect(setAvailableGroups).toHaveBeenCalledWith([{ id: 1, name: "Board" }]);
    expect(setAvailableRoles).toHaveBeenCalledWith([{ id: 1, name: "Chair" }]);
    expect(setLoading).toHaveBeenCalledWith(false);
  });

  it("leaves non-board-color settings untouched even if they look like colors", async () => {
    getSettings.mockResolvedValue({
      data: [{ name: "SomeOtherColor", value: "#ABCDEF80" }],
    });
    getGroups.mockResolvedValue({ data: [] });
    getRoles.mockResolvedValue({ data: [] });

    const setSettings = vi.fn();

    await loadSettingsPageData({
      setSettings,
      setAvailableGroups: vi.fn(),
      setAvailableRoles: vi.fn(),
      setLoading: vi.fn(),
    });

    expect(setSettings).toHaveBeenCalledWith({
      SomeOtherColor: "#ABCDEF80",
    });
  });

  it("shows an error toast when any request fails", async () => {
    getSettings.mockResolvedValue({ error: true, data: null });
    getGroups.mockResolvedValue({ data: [] });
    getRoles.mockResolvedValue({ data: [] });
    const setLoading = vi.fn();

    await loadSettingsPageData({
      setSettings: vi.fn(),
      setAvailableGroups: vi.fn(),
      setAvailableRoles: vi.fn(),
      setLoading,
    });

    expect(toast.error).toHaveBeenCalledWith(
      "failed_to_load_settings: Failed to load settings",
    );
    expect(setLoading).toHaveBeenCalledWith(false);
  });

  it("shows an error toast when groups fail to load", async () => {
    getSettings.mockResolvedValue({ data: [] });
    getGroups.mockResolvedValue({ error: true, data: null });
    getRoles.mockResolvedValue({ data: [] });

    await loadSettingsPageData({
      setSettings: vi.fn(),
      setAvailableGroups: vi.fn(),
      setAvailableRoles: vi.fn(),
      setLoading: vi.fn(),
    });

    expect(toast.error).toHaveBeenCalledWith(
      "failed_to_load_settings: Failed to load groups",
    );
  });

  it("shows an error toast when roles fail to load", async () => {
    getSettings.mockResolvedValue({ data: [] });
    getGroups.mockResolvedValue({ data: [] });
    getRoles.mockResolvedValue({ error: true, data: null });

    await loadSettingsPageData({
      setSettings: vi.fn(),
      setAvailableGroups: vi.fn(),
      setAvailableRoles: vi.fn(),
      setLoading: vi.fn(),
    });

    expect(toast.error).toHaveBeenCalledWith(
      "failed_to_load_settings: Failed to load roles",
    );
  });
});

describe("handleSettingsChange", () => {
  it("updates a single setting in the record", () => {
    const setSettings = vi.fn();
    handleSettingsChange("BoardGroupId", "5", setSettings);

    const updater = setSettings.mock.calls[0][0];
    expect(updater({ Other: "x" })).toEqual({ Other: "x", BoardGroupId: "5" });
  });
});

describe("handleAddRoleMapping", () => {
  it("does nothing when no role is selected", () => {
    const setSettings = vi.fn();
    handleAddRoleMapping({
      selectedRoleId: "",
      settings: {},
      setSettings,
      setNewSettings: vi.fn(),
      setDeletedSettings: vi.fn(),
      setSelectedRoleId: vi.fn(),
    });
    expect(setSettings).not.toHaveBeenCalled();
  });

  it("shows an error when the role is already mapped", () => {
    handleAddRoleMapping({
      selectedRoleId: "3",
      settings: { ROLEMAILMAP_3: "a@b.com" },
      setSettings: vi.fn(),
      setNewSettings: vi.fn(),
      setDeletedSettings: vi.fn(),
      setSelectedRoleId: vi.fn(),
    });
    expect(toast.error).toHaveBeenCalledWith("role_already_added");
  });

  it("stages a new role mapping and clears the selection", () => {
    const setSettings = vi.fn();
    const setNewSettings = vi.fn();
    const setDeletedSettings = vi.fn();
    const setSelectedRoleId = vi.fn();

    handleAddRoleMapping({
      selectedRoleId: "3",
      settings: {},
      setSettings,
      setNewSettings,
      setDeletedSettings,
      setSelectedRoleId,
    });

    expect(setSettings.mock.calls[0][0]({})).toEqual({
      ROLEMAILMAP_3: "",
    });
    expect(setNewSettings.mock.calls[0][0](new Set())).toEqual(
      new Set(["ROLEMAILMAP_3"]),
    );
    expect(
      setDeletedSettings.mock.calls[0][0](new Set(["ROLEMAILMAP_3"])),
    ).toEqual(new Set());
    expect(setSelectedRoleId).toHaveBeenCalledWith("");
  });
});

describe("handleRemoveRoleMapping", () => {
  it("removes the setting and tracks it as new-then-removed (no delete needed)", () => {
    const setSettings = vi.fn();
    const setNewSettings = vi.fn();
    const setDeletedSettings = vi.fn();

    handleRemoveRoleMapping({
      name: "ROLEMAILMAP_3",
      newSettings: new Set(["ROLEMAILMAP_3"]),
      setSettings,
      setNewSettings,
      setDeletedSettings,
    });

    expect(setSettings.mock.calls[0][0]({ ROLEMAILMAP_3: "a" })).toEqual({});
    expect(setNewSettings.mock.calls[0][0](new Set(["ROLEMAILMAP_3"]))).toEqual(
      new Set(),
    );
    expect(setDeletedSettings).not.toHaveBeenCalled();
  });

  it("tracks a previously-persisted mapping for deletion", () => {
    const setDeletedSettings = vi.fn();

    handleRemoveRoleMapping({
      name: "ROLEMAILMAP_3",
      newSettings: new Set(),
      setSettings: vi.fn(),
      setNewSettings: vi.fn(),
      setDeletedSettings,
    });

    expect(setDeletedSettings.mock.calls[0][0](new Set())).toEqual(
      new Set(["ROLEMAILMAP_3"]),
    );
  });
});

describe("handleSaveSettings", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("deletes, creates, and patches settings as appropriate, then clears tracking", async () => {
    deleteSettingsById.mockResolvedValue({});
    postSettings.mockResolvedValue({});
    patchSettingsById.mockResolvedValue({});
    const dispatchSpy = vi.spyOn(window, "dispatchEvent");

    const setSaving = vi.fn();
    const clearTracking = vi.fn();

    await handleSaveSettings({
      deletedSettings: new Set(["OldSetting"]),
      settings: { NewSetting: "a", ExistingSetting: "b" },
      newSettings: new Set(["NewSetting"]),
      setSaving,
      clearTracking,
    });

    await vi.waitFor(() => expect(setSaving).toHaveBeenLastCalledWith(false));

    expect(deleteSettingsById).toHaveBeenCalledWith({
      path: { id: "OldSetting" },
    });
    expect(postSettings).toHaveBeenCalledWith({
      query: { id: "NewSetting", value: "a" },
    });
    expect(patchSettingsById).toHaveBeenCalledWith({
      path: { id: "ExistingSetting" },
      body: [{ op: "replace", path: "/Value", value: "b" }],
    });
    expect(clearTracking).toHaveBeenCalled();
    expect(dispatchSpy).toHaveBeenCalledWith(
      new Event(BOARD_THEME_SETTINGS_UPDATED_EVENT),
    );
  });

  it("throws and does not clear tracking when any request fails", async () => {
    patchSettingsById.mockResolvedValue({ error: true });
    const clearTracking = vi.fn();
    const setSaving = vi.fn();

    await handleSaveSettings({
      deletedSettings: new Set(),
      settings: { A: "1" },
      newSettings: new Set(),
      setSaving,
      clearTracking,
    });

    await vi.waitFor(() => expect(setSaving).toHaveBeenLastCalledWith(false));
    expect(clearTracking).not.toHaveBeenCalled();
  });
});

describe("getGroupOptions", () => {
  it("prepends a placeholder option to the group list", () => {
    const groups: GroupResponseDto[] = [
      { id: 1, name: "Board" } as GroupResponseDto,
    ];
    expect(getGroupOptions(groups)).toEqual([
      { value: "", label: "select_a_group" },
      { value: "1", label: "Board" },
    ]);
  });
});

describe("getRoleOptions", () => {
  it("filters out roles already mapped and prepends a placeholder", () => {
    const roles: Role[] = [
      { id: 1, name: "Chair" } as Role,
      { id: 2, name: "Secretary" } as Role,
    ];
    const settings = { ROLEMAILMAP_1: "a@b.com" };

    expect(getRoleOptions(roles, settings)).toEqual([
      { value: "", label: "select_a_role_to_add" },
      { value: "2", label: "Secretary" },
    ]);
  });
});

describe("getCurrentRoleMappings", () => {
  it("returns only ROLEMAILMAP_ entries", () => {
    const settings = {
      ROLEMAILMAP_1: "a@b.com",
      BoardGroupId: "1",
      ROLEMAILMAP_2: "c@d.com",
    };
    expect(getCurrentRoleMappings(settings)).toEqual([
      ["ROLEMAILMAP_1", "a@b.com"],
      ["ROLEMAILMAP_2", "c@d.com"],
    ]);
  });
});
