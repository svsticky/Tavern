import { t } from "i18next";
import React, { useEffect, useState, useRef } from "react";
import { 
  type GroupMembershipResponseDto, 
  type MemberResponseDto,
  type RoleAlias,
} from "~/api";
import Input from "~/components/UI/Input";
import Button from "~/components/UI/Button";
import { FormSection } from "~/components/UI/Form/FormSection";
import { FormHeader } from "~/components/UI/Form/FormHeader";
import Form from "~/components/UI/Form/Form";
import { useParams } from "react-router";
import type { Column } from "~/components/Tiles/DataTableTile";
import DataTableTile from "~/components/Tiles/DataTableTile";
import BorderedTile from "~/components/Tiles/BorderedTile";
import { PageHeader } from "~/components/UI/PageHeader/PageHeader";
import { getAssociationYear } from "~/util/date.util";
import Modal from "~/components/UI/Modal/Modal";
import SearchMemberOverlay from "~/components/Member/SearchMemberOverlay";
import Select from "~/components/UI/Select";
import { PlusIcon } from "lucide-react";
import CreateRoleOverlay from "~/components/Roles/CreateRoleOverlay/CreateRoleOverlay";
import {
  handleAddGroupEnrollment,
  handleDeleteGroupEnrollment,
  handleGroupProfilePictureUpload,
  handleRoleAliasAdded,
  handleSaveGroup,
  handleUpdateGroupRole,
  loadGroupData,
  loadGroupMemberships
} from "./edit-group.handlers";

export default function EditGroupPage() {
  const params = useParams();
  const id = params.id ? parseInt(params.id) : null;
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [groupPictureSrc, setGroupPictureSrc] = useState<string | null>(null);
  const [enrollments, setEnrollments] = useState<GroupMembershipResponseDto[]>([]);
  const [roleAliases, setRoleAliases] = useState<RoleAlias[]>([]);
  const [addEnrollmentModalIsOpen, setAddEnrollmentModalIsOpen] = useState(false);
  const [addRoleModalIsOpen, setAddRoleModalIsOpen] = useState(false);
  const [loadingMemberships, setLoadingMemberships] = useState(false);
  const [loadingChangeRole, setLoadingChangeRole] = useState(false);

  const [formData, setFormData] = useState({
    Name: "",
    Type: "",
    Active: false,
  });

  const [selectedYear, setSelectedYear] = useState(getAssociationYear());

  const yearsSince2007 = Array.from({ length: getAssociationYear() - 2007 + 1 }, (_, i) => getAssociationYear() - i);

  const enrollmentColumns: Column<GroupMembershipResponseDto>[] = [
    {
        header: t("name"),
        render: (item) => item.memberName,
    },
    {
        header: t("role"),
        render: (item) => (
        <select
            value={typeof item.roleAliasId === 'number' ? item.roleAliasId : "none"}
            onChange={(e) => handleUpdateGroupRole(item.id, e.target.value === "none" ? null : parseInt(e.target.value), setLoadingChangeRole, setEnrollments)}
            className={`text-xs font-semibold px-2 py-1 rounded-full border-none cursor-pointer focus:ring-2 focus:ring-blue-500`}
            disabled={loading || loadingChangeRole || loadingMemberships}
        >
            <>
                <option value="null"></option>
                {roleAliases.map((alias) => (
                    <option key={alias.id} value={alias.id}>
                        {alias.name}
                    </option>
                ))}
            </>
        </select>
        ),
    },
    {
        header: 
        <div className="flex items-end justify-end gap-2">
            <div className="w-fit min-w-[120px]">
                <Select 
                label={null} 
                options={yearsSince2007.map((year) => ({ 
                    value: year, 
                    label: `${year-1}-${year}` 
                }))} 
                onChange={(e: React.ChangeEvent<HTMLSelectElement>) => setSelectedYear(parseInt(e.target.value))}
                />
            </div>
            <Button
                variant="secondary"
                className="h-[38px] px-3 flex items-center justify-center"
                onClick={() => setAddEnrollmentModalIsOpen(true)}
                type="button"
            >
                <PlusIcon className="w-4 h-4" />
            </Button>
        </div>,
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
      setLoading
    });
    return () => {
      cleanupPromise.then((cleanup) => cleanup && cleanup());
    };
  }, [id]);

  useEffect(() => {
    loadGroupMemberships(id, selectedYear, setLoadingMemberships, setEnrollments);
  }, [id, selectedYear]);

  if (loading) return t("loading") + "...";

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
                className={groupPictureSrc && groupPictureSrc !== "/profile-picture.svg" ? "w-full h-full object-cover" : "w-2/3 h-2/3 opacity-80"}
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
            <Input label={t("group_name")} value={formData.Name} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, Name: e.target.value})} />
            <Select label={t("group_type")} value={formData.Type} onChange={(e: React.ChangeEvent<HTMLSelectElement>) => setFormData({...formData, Type: e.target.value})} 
                options={[{ value: "Committee", label: t("committee") }, { value: "WorkingGroup", label: t("working_group") }, { value: "Dispute", label: t("dispute") }]} />
            <Input label={t("active")} type="checkbox" checked={formData.Active} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, Active: e.target.checked})} />
          </FormSection>

          <Button 
            onClick={() => handleSaveGroup(id, formData, setSaving)} 
            disabled={saving}
          >
            {saving ? t("saving") : t("save")}
          </Button>

            <section>
                <FormHeader title={t("group_enrollments")}>
                    <Button variant="secondary" onClick={() => setAddRoleModalIsOpen(true)} type="button">
                        {t("add_role")}
                    </Button>
                </FormHeader>
                <BorderedTile>
                    <DataTableTile
                        data={loadingMemberships ? [] : enrollments}
                        columns={enrollmentColumns}
                        emptyText={t("no_enrollments_found")}
                    />
                </BorderedTile>
            </section>
        </Form>

        <Modal isOpen={addEnrollmentModalIsOpen} onClose={() => setAddEnrollmentModalIsOpen(false)} title={t("add_enrollment")}>
            <SearchMemberOverlay
                selectText={t("enroll")}
                onSelect={(member: MemberResponseDto) =>
                  handleAddGroupEnrollment(id, member, selectedYear, setLoading, setEnrollments, setAddEnrollmentModalIsOpen)
                }
                loading={loading}
             />
        </Modal>

        <Modal isOpen={addRoleModalIsOpen} onClose={() => setAddRoleModalIsOpen(false)} title={t("add_role")}>
            <CreateRoleOverlay
              onRoleAliasCreated={(roleAlias: RoleAlias) => handleRoleAliasAdded(roleAlias, setRoleAliases, setAddRoleModalIsOpen)}
              onRoleCreated={() => setAddRoleModalIsOpen(false)}
            />
        </Modal>
      </div>
    </>
  );
}
