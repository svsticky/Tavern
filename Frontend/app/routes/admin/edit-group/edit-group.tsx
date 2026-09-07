import { t } from "i18next";
import { PlusIcon } from "lucide-react";
import type React from "react";
import { useCallback, useEffect, useRef, useState } from "react";
import { Link, useNavigate, useParams } from "react-router";
import {
  type GroupMembershipResponseDto,
  getGroupsByIdPermissions,
  type MemberResponseDto,
  putGroupsByIdPermissions,
  type RoleAlias,
} from "~/api";
import SearchMemberOverlay from "~/components/Member/SearchMemberOverlay";
import PermissionChecklist from "~/components/Permissions/PermissionChecklist";
import CreateRoleOverlay from "~/components/Roles/CreateRoleOverlay/CreateRoleOverlay";
import BorderedTile from "~/components/Tiles/BorderedTile";
import type { Column } from "~/components/Tiles/DataTableTile";
import DataTableTile from "~/components/Tiles/DataTableTile";
import Button from "~/components/UI/Button";
import Form from "~/components/UI/Form/Form";
import { FormHeader } from "~/components/UI/Form/FormHeader";
import { FormSection } from "~/components/UI/Form/FormSection";
import Input from "~/components/UI/Input";
import Modal from "~/components/UI/Modal/Modal";
import { PageHeader } from "~/components/UI/PageHeader";
import Select from "~/components/UI/Select";
import { useApp } from "~/context/AppContext";
import { useAuth } from "~/context/AuthContext";
import type { TokenParsed } from "~/types/TokenParsed";
import { getCommitteeYear } from "~/util/date.util";
import { hasPermission, isBoardOrCandidateBoard } from "~/util/group.util";
import {
  type EditGroupFormData,
  handleAddGroupEnrollment,
  handleDeleteGroupEnrollment,
  handleGroupProfilePictureUpload,
  handleRoleAliasAdded,
  handleSaveGroup,
  handleUpdateGroupRole,
  loadGroupData,
  loadGroupMemberships,
} from "./edit-group.handlers";

/**
 * An administrative page for managing group details, media, and memberships.
 *
 * This component provides a comprehensive management interface that allows board members to:
 * - **Modify Metadata**: Update group name, type (Committee/Working Group/Dispute), and active status.
 * - **Manage Visuals**: Upload and update the group's profile/logo picture.
 * - **Administer Memberships**:
 *    - View historical and current enrollments using an association year filter.
 *    - Assign and update specific roles (Role Aliases) for group members.
 *    - Search for and add new members to the group via a modal overlay.
 *    - Create new Role Aliases on the fly to categorize group positions.
 *
 * The page uses a split layout: a sidebar for the group image and a main form area for settings
 * and the membership data table.
 *
 * @page
 * @component
 */
export default function EditGroupPage() {
  const params = useParams();
  const id = params.id ? parseInt(params.id, 10) : null;
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [groupPictureSrc, setGroupPictureSrc] = useState<string | null>(null);
  const [enrollments, setEnrollments] = useState<GroupMembershipResponseDto[]>(
    [],
  );
  const [roleAliases, setRoleAliases] = useState<RoleAlias[]>([]);
  const [addEnrollmentModalIsOpen, setAddEnrollmentModalIsOpen] =
    useState(false);
  const [addRoleModalIsOpen, setAddRoleModalIsOpen] = useState(false);
  const [loadingMemberships, setLoadingMemberships] = useState(false);

  const loadGroupPermissions = useCallback(async () => {
    const response = await getGroupsByIdPermissions({ path: { id: id! } });
    if (response.error) throw response.error;
    return response.data ?? [];
  }, [id]);

  const saveGroupPermissions = useCallback(
    async (permissions: string[]) => {
      const response = await putGroupsByIdPermissions({
        path: { id: id! },
        body: permissions,
      });
      if (response.error) throw response.error;
    },
    [id],
  );
  const [loadingChangeRole, setLoadingChangeRole] = useState(false);

  const [formData, setFormData] = useState<EditGroupFormData>({
    Name: "",
    Type: "",
    DefaultGLAccount: "",
    DefaultCostCenter: "",
    Active: false,
  });

  const navigate = useNavigate();
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

  const canManageGroupPermissions =
    isBoardOrCandidateBoard(tokenParsed) ||
    hasPermission(tokenParsed, "ManageGroupPermissions");
  const canManageRoles =
    isBoardOrCandidateBoard(tokenParsed) ||
    hasPermission(tokenParsed, "ManageRoles");

  const { committeeCreationDate, boardGroupId, candidateBoardGroupId } =
    useApp();

  const maxYear = getCommitteeYear(committeeCreationDate);

  const [selectedYear, setSelectedYear] = useState(
    getCommitteeYear(committeeCreationDate),
  );

  const isBoardGroup = boardGroupId === id;
  const isBoardOrCandidateBoardGroup =
    id !== null && (id === boardGroupId || id === candidateBoardGroupId);

  const yearsSince2007 = Array.from(
    { length: maxYear - 2007 + 1 },
    (_, i) => maxYear - i,
  );

  const enrollmentColumns: Column<GroupMembershipResponseDto>[] = [
    {
      header: t("name"),
      render: (item) =>
        item.memberId ? (
          <Link
            to={`/admin/members/${item.memberId}`}
            className="text-slate-900 font-medium hover:text-(--board-primary) hover:underline transition-colors"
          >
            {item.memberName}
          </Link>
        ) : (
          item.memberName
        ),
    },
    {
      header: t("role"),
      render: (item) => (
        <select
          value={
            typeof item.roleAliasId === "number" ? item.roleAliasId : "none"
          }
          onChange={(e) =>
            handleUpdateGroupRole(
              item.id,
              e.target.value === "none" ? null : parseInt(e.target.value, 10),
              setLoadingChangeRole,
              setEnrollments,
            )
          }
          className={`text-xs font-semibold px-2 py-1 rounded-full border-none cursor-pointer focus:ring-2 focus:ring-blue-500`}
          disabled={loading || loadingChangeRole || loadingMemberships}
        >
          <option value="null"></option>
          {roleAliases.map((alias) => (
            <option key={alias.id} value={alias.id}>
              {alias.name}
            </option>
          ))}
        </select>
      ),
    },
    {
      header: (
        <div className="flex items-end justify-end gap-2">
          <div className="w-fit min-w-[120px]">
            <Select
              label={null}
              value={selectedYear}
              options={yearsSince2007.map((year) => ({
                value: year,
                label: `${year - 1}-${year}`,
              }))}
              onChange={(e: React.ChangeEvent<HTMLSelectElement>) =>
                setSelectedYear(parseInt(e.target.value, 10))
              }
            />
          </div>
          <Button
            variant="secondary"
            className="h-[38px] px-3 flex items-center justify-center"
            onClick={
              isBoardGroup &&
              enrollments.length === 0 &&
              selectedYear === maxYear
                ? () => navigate("/admin/settings")
                : () => setAddEnrollmentModalIsOpen(true)
            }
            type="button"
          >
            <PlusIcon className="w-4 h-4" />
          </Button>
        </div>
      ),
      className: "text-right",
      render: (item) => (
        <Button
          variant="danger"
          onClick={(e) => {
            e.stopPropagation();
            handleDeleteGroupEnrollment(item.id, setLoading, setEnrollments);
          }}
          type="button"
          disabled={loading}
        >
          {t("remove")}
        </Button>
      ),
    },
  ];

  useEffect(() => {
    const cleanupPromise = loadGroupData({
      id,
      setFormData,
      setGroupPictureSrc,
      setRoleAliases,
      setLoading,
    });
    return () => {
      cleanupPromise.then((cleanup) => cleanup?.());
    };
  }, [id]);

  useEffect(() => {
    loadGroupMemberships(
      id,
      selectedYear,
      setLoadingMemberships,
      setEnrollments,
    );
  }, [id, selectedYear]);

  if (loading) return t("loading");

  return (
    <>
      <PageHeader title="" backTo="/admin/groups" />
      <div className="flex flex-col lg:flex-row gap-12">
        <div className="flex flex-col items-center lg:w-48">
          <div
            className="relative w-40 h-40 group cursor-pointer"
            onClick={() => fileInputRef.current?.click()}
          >
            <div className="w-full h-full rounded-full overflow-hidden flex items-center justify-center bg-(--board-primary) shadow-md border-4 border-white transition-transform group-hover:scale-105">
              <img
                src={groupPictureSrc || "/profile-picture.svg"}
                className={
                  groupPictureSrc && groupPictureSrc !== "/profile-picture.svg"
                    ? "w-full h-full object-contain"
                    : "w-2/3 h-2/3 opacity-80"
                }
                alt="Profile"
              />
            </div>
            <div className="absolute inset-0 flex items-center justify-center bg-black/40 text-white rounded-full opacity-0 group-hover:opacity-100 transition-opacity text-xs font-bold uppercase">
              {t("change")}
            </div>
          </div>
          <input
            type="file"
            ref={fileInputRef}
            hidden
            accept="image/*"
            onChange={(e) => handleGroupProfilePictureUpload(e, id, setSaving)}
          />
        </div>

        <Form className="w-full space-y-8">
          <FormSection title={t("group_info")} columns={2}>
            <Input
              label={t("group_name")}
              value={formData.Name}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                setFormData({ ...formData, Name: e.target.value })
              }
              required
            />
            <Select
              label={t("group_type")}
              value={formData.Type}
              key="Type"
              onChange={(e: React.ChangeEvent<HTMLSelectElement>) =>
                setFormData({ ...formData, Type: e.target.value })
              }
              options={[
                { value: "Committee", label: t("committee") },
                { value: "WorkingGroup", label: t("working_group") },
                { value: "Dispute", label: t("dispute") },
              ]}
            />
            <Input
              label={t("gl_account_id")}
              value={formData.DefaultGLAccount}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                setFormData({ ...formData, DefaultGLAccount: e.target.value })
              }
            />
            <Input
              label={t("cost_unit_id")}
              value={formData.DefaultCostCenter}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                setFormData({ ...formData, DefaultCostCenter: e.target.value })
              }
            />
            <Input
              label={t("active")}
              type="checkbox"
              checked={formData.Active}
              key="Active"
              onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                setFormData({ ...formData, Active: e.target.checked })
              }
            />
          </FormSection>

          <Button
            onClick={() => handleSaveGroup(id, formData, setSaving)}
            disabled={saving}
          >
            {saving ? t("saving") : t("save")}
          </Button>

          {canManageGroupPermissions && id !== null && (
            <section>
              <FormHeader title={t("permissions")} />
              <BorderedTile>
                <PermissionChecklist
                  note={t("group_permissions_note")}
                  onLoad={loadGroupPermissions}
                  onSave={saveGroupPermissions}
                  allKnownPermissionsGranted={isBoardOrCandidateBoardGroup}
                />
              </BorderedTile>
            </section>
          )}

          <section>
            <FormHeader title={t("group_enrollments")}>
              {canManageRoles && (
                <Button
                  variant="secondary"
                  onClick={() => setAddRoleModalIsOpen(true)}
                  type="button"
                >
                  {t("add_role")}
                </Button>
              )}
            </FormHeader>
            <BorderedTile>
              <DataTableTile
                data={loadingMemberships ? [] : enrollments}
                columns={enrollmentColumns}
                emptyText={t("no_enrollments_found")}
                mobileActionsPosition="top"
              />
            </BorderedTile>
          </section>
        </Form>

        <Modal
          isOpen={addEnrollmentModalIsOpen}
          onClose={() => setAddEnrollmentModalIsOpen(false)}
          title={t("add_enrollment")}
        >
          <SearchMemberOverlay
            selectText={t("enroll")}
            onSelect={(member: MemberResponseDto) =>
              handleAddGroupEnrollment(
                id,
                member,
                selectedYear,
                setLoading,
                setEnrollments,
                setAddEnrollmentModalIsOpen,
              )
            }
            loading={loading}
          />
        </Modal>

        <Modal
          isOpen={addRoleModalIsOpen}
          onClose={() => setAddRoleModalIsOpen(false)}
          title={t("add_role")}
        >
          <CreateRoleOverlay
            onRoleAliasCreated={(roleAlias: RoleAlias) =>
              handleRoleAliasAdded(
                roleAlias,
                setRoleAliases,
                setAddRoleModalIsOpen,
              )
            }
            onRoleCreated={() => setAddRoleModalIsOpen(false)}
          />
        </Modal>
      </div>
    </>
  );
}
