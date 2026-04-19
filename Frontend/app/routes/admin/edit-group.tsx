import { t } from "i18next";
import React, { useEffect, useState, useRef } from "react";
import { 
    deleteApiGroupmembershipsById,
  getApiGroupmemberships,
  getApiGroupsById,
  getApiGroupsByIdGroupPicture,
  getApiRolealiases,
  patchApiGroupmembershipsById, 
  patchApiGroupsById, 
  postApiGroupmemberships, 
  postApiGroupsByIdGroupPicture, 
  type GroupMembershipResponseDto, 
  type Member,
  type RoleAlias,
} from "~/api";
import Input from "~/components/UI/Input";
import Button from "~/components/UI/Button";
import { FormSection } from "~/components/UI/Form/FormSection";
import { FormHeader } from "~/components/UI/Form/FormHeader";
import Form from "~/components/UI/Form/Form";
import toast from "react-hot-toast";
import { useParams } from "react-router";
import type { Column } from "~/components/Tiles/DataTableTile";
import DataTableTile from "~/components/Tiles/DataTableTile";
import BorderedTile from "~/components/Tiles/BorderedTile";
import { PageHeader } from "~/components/UI/PageHeader";
import { getAssociationYear } from "~/util/date.util";
import Modal from "~/components/UI/Modal";
import SearchMemberOverlay from "~/components/Activity/Edit/EditParticipantsTile/SearchMemberOverlay";
import Select from "~/components/UI/Select";
import { PlusIcon } from "lucide-react";

export default function EditGroupPage() {
  const params = useParams();
  const id = params.id ? parseInt(params.id) : null;
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [groupPictureSrc, setGroupPictureSrc] = useState<string | null>(null);
  const [enrollments, setEnrollments] = useState<GroupMembershipResponseDto[]>([]);
  const [roleAliases, setRoleAliases] = useState<RoleAlias[]>([]);
  const [modalIsOpen, setModalIsOpen] = useState(false);

  const [formData, setFormData] = useState({
    Name: "",
    Type: "",
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
            value={item.roleAliasName || "-"}
            onChange={(e) => handleUpdateRole(item.id, parseInt(e.target.value))}
            className={`text-xs font-semibold px-2 py-1 rounded-full border-none cursor-pointer focus:ring-2 focus:ring-blue-500`}
            disabled={loading}
        >
            {roleAliases.map((alias) => (
                <option key={alias.id} value={alias.id}>
                    {alias.name}
                </option>
            ))}
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
                onClick={() => setModalIsOpen(true)}
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
                handleDeleteEnrollment(item.id);
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
    let url = null as string | null;
    async function loadGroup() {
      if (!id) return;
      try {
        const groupResponse = await getApiGroupsById({ path: { id } });
        if (groupResponse.data) {
          setFormData({
            Name: groupResponse.data.name,
            Type: groupResponse.data.type,
          });
        }

        const groupPictureResponse = await getApiGroupsByIdGroupPicture({ path: { id: id }, responseType: 'blob' });
        if (groupPictureResponse.data instanceof Blob) {
          url = URL.createObjectURL(groupPictureResponse.data);
          setGroupPictureSrc(url);
        }

        const roleAliasesResponse = await getApiRolealiases();
        if (roleAliasesResponse.data) {
            setRoleAliases(roleAliasesResponse.data);
        }
      } catch (err) {
        console.log("Failed to load group data:", err);
        toast.error(t("loading_failed"));
      } finally {
        setLoading(false);
      }
    }
    loadGroup();
    return () => { if (url) URL.revokeObjectURL(url); };
  }, [id]);

  useEffect(() => {
    async function loadMemberships() {
      if (!id) return;
      try {
        const groupMembershipsResponse = await getApiGroupmemberships(
            {
                query: {
                    GroupId: id,
                    MembershipYear: selectedYear
                }
            }
        );
        if (groupMembershipsResponse.data) {
            setEnrollments(groupMembershipsResponse.data);
        }
      } catch (err) {
        console.log("Failed to load group data:", err);
        toast.error(t("loading_failed"));
      } finally {
        setLoading(false);
      }
    }
    loadMemberships();
  }, [id, selectedYear]);


  const handleSave = async () => {
    if (!id) return;
    const saveProcess = async () => {
        try{
             setSaving(true);

            const patchDoc = Object.keys(formData).map(key => ({
            op: "replace",
            path: `/${key}`,
            value: formData[key as keyof typeof formData]
            }));

            await patchApiGroupsById({
                path: { id: id },
                body: patchDoc as any
            });
        } catch (err) {
            console.error("Failed to save group data:", err);
            throw err;
        } finally {
            setSaving(false);
        }
    };

    toast.promise(saveProcess(), {
      loading: t("saving"),
      success: t("save_success"),
      error: t("save_error")
    }).finally(() => setSaving(false));
  };

  const handleProfilePictureUpload = async (e: React.ChangeEvent<HTMLInputElement>) => {
    const file = e.target.files?.[0];
    if (!file || !id) return;
    
    setSaving(true);
    
    const saveProcess = async () => {
        try {
        await postApiGroupsByIdGroupPicture({
            path: { id: id }, 
            body: { image: file }
        });
        
        window.location.reload();
        } catch (err) {
            console.error("Failed to upload group picture:", err);
            throw err;
        } finally {
            setSaving(false);
        }
    };

    toast.promise(saveProcess(), {
        loading: t("uploading"),
        success: t("upload_success"),
        error: t("upload_error")
    });
  };

  const handleDeleteEnrollment = async (id: number) => { 
    
    const deleteProcess = async () => {
        try {
            setLoading(true);
            const response = await deleteApiGroupmembershipsById({ path: { id } });

            if(response.error) throw new Error("Failed to delete enrollment");

            setEnrollments(prev => prev.filter(e => e.id !== id));
        } catch (err) {
            console.error("Failed to delete enrollment:", err);
            throw err;
        } finally {
            setLoading(false);
        }
    }

    toast.promise(deleteProcess(), {
        loading: t("deleting"),
        success: t("delete_success"),
        error: t("delete_error")
    });
  };

  const handleAddEnrollment = async (member: Member) => {
    if (!id || !member || !member.id) return;
    const executeProcess = async () => {
        try {
            setLoading(true);
            const res = await postApiGroupmemberships({
                body: {
                    memberId: member.id!,
                    groupId: id,
                    membershipYear: selectedYear,
                }
            });
            if (res.data) {
                setEnrollments(prev => [...prev, res.data as GroupMembershipResponseDto]);
                toast.success(t("enrollment_added"));
                setModalIsOpen(false);
            }
        } catch (err) {
            console.error("Failed to add enrollment:", err);
            throw err;
        }
        finally {
            setLoading(false);
        }
    }

    toast.promise(executeProcess(), {
        loading: t("adding"),
        success: t("add_success"),
        error: t("add_error")
    });
  };

    const handleUpdateRole = async (enrollmentId: number, newRoleAliasId: number) => {
        const saveProcess = async () => {
            try{
                setLoading(true);
                const response = await patchApiGroupmembershipsById({
                    path: { id: enrollmentId },
                    body: [
                        { op: "replace", path: "/status", value: newRoleAliasId  }
                    ] as any
                });

                if(response.error) throw new Error("Failed to update role");
                
                setEnrollments(prev => prev.map(e => 
                    e.id === enrollmentId 
                        ? { ...e, status: newRoleAliasId as any } 
                        : e
                ));
            } catch (err) {
                console.error("Failed to update enrollment role:", err);
                throw err;
            } finally {
                setLoading(false);
            }
        };

        toast.promise(saveProcess(), {
            loading: t("updating_role"),
            success: t("role_updated"),
            error: t("role_update_failed")
        });
    };

  if (loading) return t("loading") + "...";

  return (
    <>      
      <PageHeader title="" backTo="/admin/members" />
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
            onChange={handleProfilePictureUpload} 
          />
        </div>

        <Form className="w-full space-y-8">
          <FormSection title={t("group_info")} columns={2}>
            <Input label={t("group_name")} value={formData.Name} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, Name: e.target.value})} />
            <Select label={t("group_type")} value={formData.Type} onChange={(e: React.ChangeEvent<HTMLSelectElement>) => setFormData({...formData, Type: e.target.value})} 
                options={[{ value: "Committee", label: t("committee") }, { value: "WorkingGroup", label: t("working_group") }, { value: "Dispute", label: t("dispute") }]} />
          </FormSection>

          <Button 
            onClick={handleSave} 
            disabled={saving}
          >
            {saving ? t("saving") : t("save")}
          </Button>

            <section>
                <FormHeader title={t("group_enrollments")} />
                <BorderedTile>
                    <DataTableTile
                        data={enrollments}
                        columns={enrollmentColumns}
                        emptyText={t("no_enrollments_found")}
                    />
                </BorderedTile>
            </section>
        </Form>
        <Modal isOpen={modalIsOpen} onClose={() => setModalIsOpen(false)} title={t("add_enrollment")}>
            <SearchMemberOverlay
                selectText={t("enroll")}
                onSelect={handleAddEnrollment}
                loading={loading}
             />
        </Modal>
      </div>
    </>
  );
}