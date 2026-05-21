import { t } from "i18next";
import type React from "react";
import toast from "react-hot-toast";
import {
  getRoles,
  postRolealiases,
  postRoles,
  type Role,
  type RoleAlias,
} from "~/api";

/**
 * Fetches all existing roles/aliases from the API and updates the local state.
 *
 * @param {function} setLoadingRoles - State setter to track the loading status of the fetch operation.
 * @param {function} setRoles - State setter to update the list of roles in the UI.
 * @returns {Promise<void>}
 */
export const fetchRoles = async (
  setLoadingRoles: (loading: boolean) => void,
  setRoles: (roles: RoleAlias[]) => void,
) => {
  setLoadingRoles(true);
  try {
    const res = await getRoles();

    if (res.error || !res.data) throw new Error("Failed to fetch roles");

    setRoles(res.data);
  } catch (error) {
    console.error("Search error:", error);
    toast.error(t("failed_to_load_roles"));
  } finally {
    setLoadingRoles(false);
  }
};

/**
 * Arguments required for the handleCreateRoleSubmit function.
 * @typedef {Object} HandleCreateRoleSubmitArgs
 * @property {React.FormEvent} e - The form submission event.
 * @property {string} selectedType - The type of role to create ("ParentRole" or "RoleAlias").
 * @property {string} name - The display name for the new role or alias.
 * @property {string} selectedRoleId - The ID of the parent role (required only for RoleAlias).
 * @property {function} setLoading - Callback to update the submission loading state.
 * @property {function} onRoleCreated - Callback executed when a new ParentRole is successfully created.
 * @property {function} onRoleAliasCreated - Callback executed when a new RoleAlias is successfully created.
 */
type HandleCreateRoleSubmitArgs = {
  e: React.FormEvent;
  selectedType: string;
  name: string;
  selectedRoleId: string;
  setLoading: (loading: boolean) => void;
  onRoleCreated: (role: Role) => void;
  onRoleAliasCreated: (roleAlias: RoleAlias) => void;
};

/**
 * Handles the submission logic for creating either a new Parent Role or a Role Alias.
 * It determines which API endpoint to call based on the selectedType and provides
 * visual feedback via toast notifications.
 *
 * @param {HandleCreateRoleSubmitArgs} args - The configuration and state handlers for the submission.
 */
export const handleCreateRoleSubmit = ({
  e,
  selectedType,
  name,
  selectedRoleId,
  setLoading,
  onRoleCreated,
  onRoleAliasCreated,
}: HandleCreateRoleSubmitArgs) => {
  e.preventDefault();

  const postRole = async () => {
    try {
      setLoading(true);
      if (selectedType === "ParentRole") {
        const response = await postRoles({
          body: {
            name,
          },
        });

        if (response.error || !response.data)
          throw new Error("Failed to create role");

        onRoleCreated({ id: (response.data as any).id, name });
      } else if (selectedType === "RoleAlias") {
        const response = await postRolealiases({
          body: {
            name,
            roleId: Number(selectedRoleId),
          },
        });

        if (response.error || !response.data)
          throw new Error("Failed to create role alias");

        onRoleAliasCreated({
          id: (response.data as any).id,
          name,
          roleId: Number(selectedRoleId),
        });
      }
    } catch (error) {
      console.error("Error creating role:", error);
      throw error;
    } finally {
      setLoading(false);
    }
  };

  toast.promise(postRole(), {
    loading: t("creating_role"),
    success: t("role_created"),
    error: t("creating_role_failed"),
  });
};
