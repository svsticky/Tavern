import { t } from "i18next";
import { PlusIcon } from "lucide-react";
import { useCallback, useEffect, useState } from "react";
import toast from "react-hot-toast";
import {
  deleteRolesById,
  getRoles,
  getRolesByIdPermissions,
  postRoles,
  putRolesByIdPermissions,
  type Role,
} from "~/api";
import PermissionChecklist from "~/components/Permissions/PermissionChecklist";
import BorderedTile from "~/components/Tiles/BorderedTile";
import type { Column } from "~/components/Tiles/DataTableTile";
import DataTable from "~/components/Tiles/DataTableTile";
import Button from "~/components/UI/Button";
import Input from "~/components/UI/Input";
import Modal from "~/components/UI/Modal/Modal";
import { PageHeader } from "~/components/UI/PageHeader";
import { useAuth } from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import { appendErrorMessage } from "~/util/error.util";
import { hasPermission, isBoardOrCandidateBoard } from "~/util/group.util";

/**
 * An administrative page for creating, deleting, and managing the permissions of Roles.
 *
 * Gated by the ManageRoles permission (board members always have it). Managing a role's
 * permissions specifically requires ManageRolePermissions.
 *
 * @page
 * @component
 */
export default function RolesPage() {
  const authService = useAuth();
  const [tokenParsed, setTokenParsed] = useState<TokenParsed | null>(null);

  useEffect(() => {
    let cancelled = false;
    authService.getTokenParsed().then((token) => {
      if (!cancelled) setTokenParsed(token);
    });
    return () => {
      cancelled = true;
    };
  }, [authService]);

  const canManageRoles =
    isBoardOrCandidateBoard(tokenParsed) ||
    hasPermission(tokenParsed, "ManageRoles");
  const canManageRolePermissions =
    isBoardOrCandidateBoard(tokenParsed) ||
    hasPermission(tokenParsed, "ManageRolePermissions");

  const [loading, setLoading] = useState(true);
  const [roles, setRoles] = useState<Role[]>([]);
  const [createModalOpen, setCreateModalOpen] = useState(false);
  const [newRoleName, setNewRoleName] = useState("");
  const [creating, setCreating] = useState(false);
  const [permissionsRole, setPermissionsRole] = useState<Role | null>(null);

  const loadRolePermissions = useCallback(async () => {
    const response = await getRolesByIdPermissions({
      path: { id: permissionsRole!.id! },
    });
    if (response.error) throw response.error;
    return response.data ?? [];
  }, [permissionsRole]);

  const saveRolePermissions = useCallback(
    async (permissions: string[]) => {
      const response = await putRolesByIdPermissions({
        path: { id: permissionsRole!.id! },
        body: permissions,
      });
      if (response.error) throw response.error;
    },
    [permissionsRole],
  );

  const loadRoles = useCallback(async () => {
    try {
      setLoading(true);
      const response = await getRoles();
      if (response.error) throw response.error;
      setRoles(response.data ?? []);
    } catch (error) {
      console.error("Error fetching roles:", error);
      toast.error(appendErrorMessage(t("loading_failed"), error));
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    loadRoles();
  }, [loadRoles]);

  const createRole = async () => {
    setCreating(true);
    try {
      const response = await postRoles({ body: { name: newRoleName } });
      if (response.error) throw response.error;
      setNewRoleName("");
      setCreateModalOpen(false);
      await loadRoles();
    } catch (error) {
      console.error("Error creating role:", error);
      toast.error(appendErrorMessage(t("save_failed"), error));
    } finally {
      setCreating(false);
    }
  };

  const deleteRole = async (role: Role) => {
    if (role.id === undefined) return;
    try {
      const response = await deleteRolesById({ path: { id: role.id } });
      if (response.error) throw response.error;
      await loadRoles();
    } catch (error) {
      console.error("Error deleting role:", error);
      toast.error(appendErrorMessage(t("save_failed"), error));
    }
  };

  const columns: Column<Role>[] = [
    {
      header: t("role_name"),
      render: (role) => <span className="text-slate-700">{role.name}</span>,
    },
    {
      header: "",
      className: "w-full sm:w-px whitespace-nowrap text-right",
      render: (role) => (
        <div className="flex justify-end gap-2">
          {canManageRolePermissions && (
            <Button
              variant="secondary"
              className="w-full sm:w-auto"
              onClick={(e) => {
                e.stopPropagation();
                setPermissionsRole(role);
              }}
            >
              {t("manage_permissions")}
            </Button>
          )}
          {canManageRoles && (
            <Button
              variant="danger"
              className="w-full sm:w-auto"
              onClick={(e) => {
                e.stopPropagation();
                deleteRole(role);
              }}
            >
              {t("delete_role")}
            </Button>
          )}
        </div>
      ),
    },
  ];

  return (
    <div className="flex flex-col gap-4 p-4">
      <PageHeader
        title={t("roles")}
        backTo="/"
        action={
          canManageRoles && (
            <Button
              variant="secondary"
              onClick={() => setCreateModalOpen(true)}
              className="items-center px-3 py-1"
            >
              <PlusIcon className="w-5 h-5" />
            </Button>
          )
        }
      />

      {loading ? (
        t("loading")
      ) : (
        <BorderedTile className="bg-white p-0">
          <DataTable
            data={roles}
            columns={columns}
            emptyText={t("no_roles_found")}
          />
        </BorderedTile>
      )}

      <Modal
        title={t("create_role")}
        isOpen={createModalOpen}
        onClose={() => setCreateModalOpen(false)}
      >
        <div className="flex flex-col gap-4">
          <Input
            label={t("role_name")}
            value={newRoleName}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              setNewRoleName(e.target.value)
            }
          />
          <Button
            onClick={createRole}
            disabled={creating || newRoleName.trim() === ""}
          >
            {creating ? t("saving") : t("create_role")}
          </Button>
        </div>
      </Modal>

      <Modal
        title={`${t("manage_permissions")}: ${permissionsRole?.name ?? ""}`}
        isOpen={permissionsRole !== null}
        onClose={() => setPermissionsRole(null)}
      >
        {permissionsRole?.id !== undefined && (
          <PermissionChecklist
            note={t("role_permissions_note")}
            onLoad={loadRolePermissions}
            onSave={saveRolePermissions}
          />
        )}
      </Modal>
    </div>
  );
}
