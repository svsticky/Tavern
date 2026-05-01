import type React from "react";
import { t } from "i18next";
import toast from "react-hot-toast";
import {
  deleteApiSettingsById,
  getApiGroups,
  getApiRoles,
  getApiSettings,
  patchApiSettingsById,
  postApiSettings,
  type GroupResponseDto,
  type Role,
  type Setting
} from "~/api";

/**
 * Arguments for the loadSettingsPageData handler.
 */
type LoadSettingsArgs = {
  setSettings: (settings: Record<string, string>) => void;
  setAvailableGroups: (groups: GroupResponseDto[]) => void;
  setAvailableRoles: (roles: Role[]) => void;
  setLoading: (loading: boolean) => void;
};

/**
 * Fetches and synchronizes all data required for the settings dashboard.
 * Orchestrates parallel requests for settings, groups, and roles.
 * 
 * @async
 * @param {LoadSettingsArgs} args - Configuration object and state setters.
 */
export const loadSettingsPageData = async ({
  setSettings,
  setAvailableGroups,
  setAvailableRoles,
  setLoading
}: LoadSettingsArgs) => {
  try {
    const [settingsRes, groupsRes, rolesRes] = await Promise.all([
      getApiSettings(),
      getApiGroups({ query: { IncludeInactive: true } }),
      getApiRoles()
    ]);

    if(settingsRes.error || !settingsRes.data) throw new Error("Failed to load settings");
    if(groupsRes.error || !groupsRes.data) throw new Error("Failed to load groups");
    if(rolesRes.error || !rolesRes.data) throw new Error("Failed to load roles");

    const settingsObj = settingsRes.data.reduce((acc: Record<string, string>, s: Setting) => {
      if (s.name) acc[s.name] = s.value || "";
      return acc;
    }, {});
    setSettings(settingsObj);
    setAvailableGroups(groupsRes.data);
    setAvailableRoles(rolesRes.data);
  } catch (error) {
    console.error("Error loading settings page data:", error);
    toast.error(t("failed_to_load_settings"));
  } finally {
    setLoading(false);
  }
};

/**
 * Updates a specific setting in the local state dictionary.
 * 
 * @param {string} name - The unique key/name of the setting.
 * @param {string} value - The new value to assign.
 * @param {Function} setSettings - React Dispatcher for the settings record.
 */
export const handleSettingsChange = (
  name: string,
  value: string,
  setSettings: React.Dispatch<React.SetStateAction<Record<string, string>>>
) => {
  setSettings((prev) => ({ ...prev, [name]: value }));
};

/**
 * Arguments for the handleAddRoleMapping handler.
 */
type AddRoleMappingArgs = {
  selectedRoleId: string;
  settings: Record<string, string>;
  setSettings: React.Dispatch<React.SetStateAction<Record<string, string>>>;
  setNewSettings: React.Dispatch<React.SetStateAction<Set<string>>>;
  setDeletedSettings: React.Dispatch<React.SetStateAction<Set<string>>>;
  setSelectedRoleId: (value: string) => void;
};

/**
 * Stages a new role-to-email mapping in the local state. 
 * Prevents duplicates and updates tracking sets for batch persistence.
 * 
 * @param {AddRoleMappingArgs} args - Context and tracking setters.
 */
export const handleAddRoleMapping = ({
  selectedRoleId,
  settings,
  setSettings,
  setNewSettings,
  setDeletedSettings,
  setSelectedRoleId
}: AddRoleMappingArgs) => {
  if (!selectedRoleId) return;
  const settingName = `ROLEMAILMAP_${selectedRoleId}`;

  if (settings[settingName] !== undefined) {
    toast.error(t("role_already_added"));
    return;
  }

  setSettings((prev) => ({ ...prev, [settingName]: "" }));
  setNewSettings((prev) => new Set(prev).add(settingName));
  setDeletedSettings((prev) => {
    const next = new Set(prev);
    next.delete(settingName);
    return next;
  });
  setSelectedRoleId("");
};

/**
 * Arguments for the handleRemoveRoleMapping handler.
 */
type RemoveRoleMappingArgs = {
  name: string;
  newSettings: Set<string>;
  setSettings: React.Dispatch<React.SetStateAction<Record<string, string>>>;
  setNewSettings: React.Dispatch<React.SetStateAction<Set<string>>>;
  setDeletedSettings: React.Dispatch<React.SetStateAction<Set<string>>>;
};

/**
 * Removes a role mapping from the UI and adds it to the deletion tracking set.
 * 
 * @param {RemoveRoleMappingArgs} args - Context and tracking setters.
 */
export const handleRemoveRoleMapping = ({
  name,
  newSettings,
  setSettings,
  setNewSettings,
  setDeletedSettings
}: RemoveRoleMappingArgs) => {
  setSettings((prev) => {
    const next = { ...prev };
    delete next[name];
    return next;
  });

  if (newSettings.has(name)) {
    setNewSettings((prev) => {
      const next = new Set(prev);
      next.delete(name);
      return next;
    });
  } else {
    setDeletedSettings((prev) => new Set(prev).add(name));
  }
};

/**
 * Arguments for the handleSaveSettings handler.
 */
type SaveSettingsArgs = {
  deletedSettings: Set<string>;
  settings: Record<string, string>;
  newSettings: Set<string>;
  setSaving: (saving: boolean) => void;
  clearTracking: () => void;
};

/**
 * Persists all staged changes to the server in a batch process.
 * Handles deletions, creations (POST), and updates (PATCH).
 * 
 * @async
 * @param {SaveSettingsArgs} args - Tracking data and persistence logic.
 */
export const handleSaveSettings = async ({
  deletedSettings,
  settings,
  newSettings,
  setSaving,
  clearTracking
}: SaveSettingsArgs) => {
  setSaving(true);

  const saveProcess = async () => {
    try {
      const promises = [];

      for (const id of deletedSettings) {
        promises.push(deleteApiSettingsById({ path: { id } }));
      }

      for (const [name, value] of Object.entries(settings)) {
        if (newSettings.has(name)) {
          promises.push(postApiSettings({ query: { id: name, value } }));
        } else {
          promises.push(
            patchApiSettingsById({
              path: { id: name },
              body: [{ op: "replace", path: "/Value", value }] as any,
            })
          );
        }
      }

      const responses = await Promise.all(promises);

      const hasError = responses.some((res) => res.error);
      if (hasError) throw new Error("Failed to save settings");

      clearTracking();
    } catch (error) {
      console.error("Error saving settings:", error);
      throw error;
    } finally {
      setSaving(false);
    }
  };
  
  toast.promise(saveProcess(), {
    loading: t("saving"),
    success: t("save_success"),
    error: t("save_error")
  });
};

/**
 * Utility to format groups for a Select component.
 * 
 * @param {GroupResponseDto[]} availableGroups - Raw group data.
 * @returns {Array<{value: string, label: string}>} Formatted options.
 */
export const getGroupOptions = (availableGroups: GroupResponseDto[]) => [
  { value: "", label: t("select_a_group") },
  ...availableGroups.map((g) => ({ value: g.id.toString(), label: g.name }))
];

/**
 * Utility to format roles for a Select component, filtering out roles already mapped.
 * 
 * @param {Role[]} availableRoles - Raw role data.
 * @param {Record<string, string>} settings - Current settings to check for existing maps.
 * @returns {Array<{value: string, label: string}>} Formatted options.
 */
export const getRoleOptions = (availableRoles: Role[], settings: Record<string, string>) => [
  { value: "", label: t("select_a_role_to_add") },
  ...availableRoles
    .filter((r) => !settings[`ROLEMAILMAP_${r.id}`])
    .map((r) => ({ value: r.id?.toString() || "", label: r.name }))
];

/**
 * Filters the settings dictionary to return only role-to-email mapping entries.
 * 
 * @param {Record<string, string>} settings - The full settings record.
 * @returns {Array<[string, string]>} Array of role mapping entries.
 */
export const getCurrentRoleMappings = (settings: Record<string, string>) =>
  Object.entries(settings).filter(([key]) => key.startsWith("ROLEMAILMAP_"));
