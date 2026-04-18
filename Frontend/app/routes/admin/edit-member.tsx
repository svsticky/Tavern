import { t } from "i18next";
import { useEffect, useState, useRef } from "react";
import { 
    deleteApiStudyenrollmentsById,
  getApiMembersById, 
  getApiMembersByIdProfilePicture, 
  getApiStudies, 
  getApiStudyenrollments, 
  patchApiMembersById, 
  patchApiStudyenrollmentsById, 
  postApiProfilepictureByIdProfilePicture,
  postApiStudyenrollments,
  type Study,
  type StudyEnrollmentResponseDto, 
} from "~/api";
import Input from "~/components/UI/Input";
import Button from "~/components/UI/Button";
import Checkbox from "~/components/UI/Checkbox";
import { FormSection } from "~/components/UI/Form/FormSection";
import { FormHeader } from "~/components/UI/Form/FormHeader";
import Tile from "~/components/Tiles/Tile";
import Form from "~/components/UI/Form/Form";
import toast from "react-hot-toast";
import { useParams } from "react-router";
import { type StudyStatus } from "~/api";
import Select from "~/components/UI/Select";
import type { Column } from "~/components/Tiles/DataTableTile";
import DataTableTile from "~/components/Tiles/DataTableTile";
import BorderedTile from "~/components/Tiles/BorderedTile";
import { PageHeader } from "~/components/UI/PageHeader";

export default function AdminMemberEditPage() {
  const { id: memberId } = useParams<{ id: string }>();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const fileInputRef = useRef<HTMLInputElement>(null);
  const [profilePictureSrc, setProfilePictureSrc] = useState<string | null>(null);
  const [enrollments, setEnrollments] = useState<StudyEnrollmentResponseDto[]>([]);
  const [availableStudies, setAvailableStudies] = useState<Study[]>([]);
  const [selectedStudyId, setSelectedStudyId] = useState<number | "">("");

  const [formData, setFormData] = useState({
    firstName: "",
    lastName: "",
    email: "",
    studentNumber: 0,
    phoneNumber: "",
    street: "",
    houseNumber: "",
    postalCode: "",
    city: "",
    parentPhoneNumber: "",
    preferredLanguage: "NL",
    mailSubscriptions: 0,
    notes: "",
    gratie: false,
    lidVanVerdienste: false,
    ereLid: false,
    begunstiger: false,
    suspended: false,
    dateOfBirth: ""
  });

  const enrollmentColumns: Column<StudyEnrollmentResponseDto>[] = [
    {
        header: t("study"),
        render: (item) => item.studyTitle,
    },
    {
        header: t("start_date"),
        render: (item) => new Date(item.enrollmentDate).toLocaleDateString(),
    },
    {
        header: t("status"),
        render: (item) => (
        <select
            value={item.status}
            onChange={(e) => handleUpdateStatus(item.id, e.target.value as StudyStatus)}
            className={`text-xs font-semibold px-2 py-1 rounded-full border-none cursor-pointer focus:ring-2 focus:ring-blue-500 ${
            item.status === "Completed"
                ? "bg-green-100 text-green-700"
                : item.status === "DroppedOut"
                ? "bg-red-100 text-red-700"
                : "bg-blue-100 text-blue-700"
            }`}
            disabled={loading}
        >
            <option value={"Enrolled"}>{t("status_in_progress")}</option>
            <option value={"Completed"}>{t("status_completed")}</option>
            <option value={"DroppedOut"}>{t("status_dropped_out")}</option>
        </select>
        ),
    },
    {
        header: "",
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
    async function loadMember() {
      if (!memberId) return;
      try {
        const memberResponse = await getApiMembersById({ path: { id: memberId } });
        if (memberResponse.data) {
          setFormData({
            firstName: memberResponse.data.firstName || "",
            lastName: memberResponse.data.lastName || "",
            email: memberResponse.data.email || "",
            studentNumber: Number(memberResponse.data.studentNumber) || 0,
            phoneNumber: memberResponse.data.phoneNumber || "",
            street: memberResponse.data.street || "",
            houseNumber: memberResponse.data.houseNumber || "",
            postalCode: memberResponse.data.postalCode || "",
            city: memberResponse.data.city || "",
            parentPhoneNumber: memberResponse.data.parentPhoneNumber || "",
            preferredLanguage: memberResponse.data.preferredLanguage ?? "NL",
            mailSubscriptions: Number(memberResponse.data.mailSubscriptions) || 0,
            notes: memberResponse.data.notes || "",
            gratie: !!memberResponse.data.gratie,
            lidVanVerdienste: !!memberResponse.data.lidVanVerdienste,
            ereLid: !!memberResponse.data.ereLid,
            begunstiger: !!memberResponse.data.begunstiger,
            suspended: !!memberResponse.data.suspended,
            dateOfBirth: memberResponse.data.dateOfBirth ? new Date(memberResponse.data.dateOfBirth).toISOString().split('T')[0] : ""
          });
        }

        const studyEnrollmentsResponse = await getApiStudyenrollments({ query: { MemberId: memberId } });
        if(studyEnrollmentsResponse.data) {
          setEnrollments(studyEnrollmentsResponse.data);
        }

        const studiesResponse = await getApiStudies();
        if(studiesResponse.data) {
          setAvailableStudies(studiesResponse.data);
        }

        const profilePictureResponse = await getApiMembersByIdProfilePicture({ path: { id: memberId }, responseType: 'blob' });
        if (profilePictureResponse.data instanceof Blob) {
          url = URL.createObjectURL(profilePictureResponse.data);
          setProfilePictureSrc(url);
        }
      } catch (err) {
        console.log("Failed to load member data:", err);
        toast.error(t("loading_failed"));
      } finally {
        setLoading(false);
      }
    }
    loadMember();
    return () => { if (url) URL.revokeObjectURL(url); };
  }, [memberId]);

  const handleSave = async () => {
    if (!memberId) return;
    const saveProcess = async () => {
        try{
             setSaving(true);

            const patchDoc = Object.keys(formData).map(key => ({
            op: "replace",
            path: `/${key}`,
            value: formData[key as keyof typeof formData]
            }));

            await patchApiMembersById({
                path: { id: memberId },
                body: patchDoc as any
            });
        } catch (err) {
            console.error("Failed to save member data:", err);
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
    if (!file || !memberId) return;
    
    setSaving(true);
    
    const saveProcess = async () => {
        try {
        await postApiProfilepictureByIdProfilePicture({
            path: { id: memberId }, 
            body: { image: file }
        });
        
        window.location.reload();
        } catch (err) {
            console.error("Failed to upload profile picture:", err);
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
            const response = await deleteApiStudyenrollmentsById({ path: { id } });

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

  const handleAddEnrollment = async () => {
    if (!memberId || !selectedStudyId) return;
    const executeProcess = async () => {
        try {
            setLoading(true);
            const res = await postApiStudyenrollments({
                body: {
                    memberId: memberId,
                    studyId: selectedStudyId,
                    enrollmentDate: new Date().toISOString(),
                }
            });
            if (res.data) {
                setEnrollments(prev => [...prev, res.data]);
                toast.success("Studie toegevoegd");
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

    const handleUpdateStatus = async (enrollmentId: number, newStatus: StudyStatus) => {
        const saveProcess = async () => {
            try{
                setLoading(true);
                const response = await patchApiStudyenrollmentsById({
                    path: { id: enrollmentId },
                    body: [
                        { op: "replace", path: "/status", value: newStatus }
                    ] as any
                });

                if(response.error) throw new Error("Failed to update status");
                
                setEnrollments(prev => prev.map(e => 
                    e.id === enrollmentId 
                        ? { ...e, status: newStatus as any } 
                        : e
                ));
            } catch (err) {
                console.error("Failed to update enrollment status:", err);
                throw err;
            } finally {
                setLoading(false);
            }
        };

        toast.promise(saveProcess(), {
            loading: t("updating_status"),
            success: t("status_updated"),
            error: t("status_update_failed")
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
                src={profilePictureSrc || "/profile-picture.svg"} 
                className={profilePictureSrc && profilePictureSrc !== "/profile-picture.svg" ? "w-full h-full object-cover" : "w-2/3 h-2/3 opacity-80"}
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
          <FormSection title={t("personal_info")} columns={2}>
            <Input label={t("first_name")} value={formData.firstName} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, firstName: e.target.value})} />
            <Input label={t("last_name")} value={formData.lastName} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, lastName: e.target.value})} />
            <Input label={t("email")} type="email" value={formData.email} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, email: e.target.value})} />
            <Input label={t("student_number")} type="number" value={formData.studentNumber.toString()} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, studentNumber: parseInt(e.target.value)})} />
            <Input label={t("date_of_birth")} type="date" value={formData.dateOfBirth} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, dateOfBirth: e.target.value})} />
          </FormSection>

          {/* Contact & Adres */}
          <FormSection title={t("contact_and_address")} columns={2}>
            <Input label={t("phone_number")} value={formData.phoneNumber} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, phoneNumber: e.target.value})} />
            <Input label={t("parent_phone_number")} value={formData.parentPhoneNumber} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, parentPhoneNumber: e.target.value})} />
            <div className="md:col-span-2 grid grid-cols-3 gap-4">
               <div className="col-span-2"><Input label={t("street")} value={formData.street} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, street: e.target.value})} /></div>
               <Input label={t("house_number")} value={formData.houseNumber} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, houseNumber: e.target.value})} />
            </div>
          </FormSection>

          {/* Administratieve Status (Nieuw) */}
          <section>
            <FormHeader title={t("status_and_membership")} />
            <Tile className="grid grid-cols-1 md:grid-cols-3 gap-4 p-5 bg-blue-50/50">
              <Checkbox label={t("gratie")} checked={formData.gratie} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, gratie: e.target.checked})} />
              <Checkbox label={t("lid_van_verdienste")} checked={formData.lidVanVerdienste} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, lidVanVerdienste: e.target.checked})} />
              <Checkbox label={t("ere_lid")} checked={formData.ereLid} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, ereLid: e.target.checked})} />
              <Checkbox label={t("begunstiger")} checked={formData.begunstiger} onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, begunstiger: e.target.checked})} />
              <Checkbox label={t("suspended")} checked={formData.suspended} className="text-red-600 font-bold" onChange={(e: React.ChangeEvent<HTMLInputElement>) => setFormData({...formData, suspended: e.target.checked})} />
            </Tile>
          </section>

          {/* Notities (Admin Only) */}
          <section>
            <FormHeader title={t("internal_notes")} />
            <textarea 
              className="w-full p-3 border rounded-md min-h-[100px]"
              value={formData.notes}
              placeholder={t("internal_notes_placeholder")}
              onChange={e => setFormData({...formData, notes: e.target.value})}
            />
          </section>

          <Button 
            onClick={handleSave} 
            disabled={saving}
          >
            {saving ? t("saving") : t("save")}
          </Button>

            <section>
                <FormHeader title={t("study_enrollments")} />
                <BorderedTile>
                    <DataTableTile
                        data={enrollments}
                        columns={enrollmentColumns}
                        emptyText={t("no_enrollments_found")}
                    />
                    <div className="flex flex-col sm:flex-row items-end gap-4 w-full">
                        <div className="flex-1 w-full">
                            <Select
                                label={t("add_study_enrollment")}
                                onChange={(e) => {
                                    if (e.target.value) {
                                        setSelectedStudyId(parseInt(e.target.value));
                                    } else {
                                        setSelectedStudyId("");
                                    }
                                }}
                                defaultValue=""
                                options={[
                                    { value: "", label: `${t("select_a_study")}...` },
                                    ...availableStudies.map((study) => ({
                                        value: study.id!.toString(),
                                        label: study.title,
                                    }))
                                ]}
                            />
                        </div>

                        <Button
                            variant="primary"
                            onClick={handleAddEnrollment}
                            disabled={!selectedStudyId || loading}
                            className="h-[46px] whitespace-nowrap"
                            type="button"
                        >
                            {t("add")}
                        </Button>
                    </div>
                </BorderedTile>
            </section>
        </Form>
      </div>
    </>
  );
}