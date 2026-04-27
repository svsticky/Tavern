import type React from "react";
import { t } from "i18next";
import toast from "react-hot-toast";
import { getApiRoles, postApiRolealiases, postApiRoles, type Role, type RoleAlias } from "~/api";

export const fetchRoles = async (
  setLoadingRoles: (loading: boolean) => void,
  setRoles: (roles: RoleAlias[]) => void
) => {
  setLoadingRoles(true);
  try {
    const res = await getApiRoles();

    if(res.error || !res.data) throw new Error("Failed to fetch roles");

    setRoles(res.data);
  } catch (error) {
    console.error("Search error:", error);
    toast.error(t("failed_to_load_roles"));
  } finally {
    setLoadingRoles(false);
  }
};

type HandleCreateRoleSubmitArgs = {
  e: React.FormEvent;
  selectedType: string;
  name: string;
  selectedRoleId: string;
  setLoading: (loading: boolean) => void;
  onRoleCreated: (role: Role) => void;
  onRoleAliasCreated: (roleAlias: RoleAlias) => void;
};

export const handleCreateRoleSubmit = ({ e, selectedType, name, selectedRoleId, setLoading, onRoleCreated, onRoleAliasCreated }: HandleCreateRoleSubmitArgs) => {
  e.preventDefault();

  const postRole = async () => {
    try {
      setLoading(true);
      if (selectedType === "ParentRole") {
        const response = await postApiRoles({
          body: {
            name,
          }
        });
        
        if(response.error || !response.data) throw new Error("Failed to create role");

        onRoleCreated({ id: (response.data as any).id, name });
      } else if (selectedType === "RoleAlias") {
        const response = await postApiRolealiases({
          body: {
            name,
            roleId: Number(selectedRoleId)
          }
        });

        if(response.error || !response.data) throw new Error("Failed to create role alias");

        onRoleAliasCreated({ id: (response.data as any).id, name, roleId: Number(selectedRoleId) });
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
    error: t("creating_role_failed")
  });
};
