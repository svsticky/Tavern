import { t } from "i18next";
import { CircleAlert } from "lucide-react";
import { useEffect, useState } from "react";
import { useNavigate, useParams } from "react-router";
import type {
  GroupMembershipResponseDto,
  Study,
  StudyEnrollmentResponseDto,
  StudyStatus,
} from "~/api";
import ChangeProfilePicture from "~/components/Account/ChangeProfilePicture/ChangeProfilePicture";
import GroupMembershipOverview from "~/components/Group/GroupMembershipOverview";
import BorderedTile from "~/components/Tiles/BorderedTile";
import type { Column } from "~/components/Tiles/DataTableTile";
import DataTableTile from "~/components/Tiles/DataTableTile";
import Tile from "~/components/Tiles/Tile";
import Button from "~/components/UI/Button";
import Checkbox from "~/components/UI/Checkbox";
import { useConfirm } from "~/components/UI/ConfirmModal/useConfirm";
import Form from "~/components/UI/Form/Form";
import { FormHeader } from "~/components/UI/Form/FormHeader";
import { FormSection } from "~/components/UI/Form/FormSection";
import Input from "~/components/UI/Input";
import { PageHeader } from "~/components/UI/PageHeader";
import Select from "~/components/UI/Select";
import { formatDate } from "~/util/date.util";
import {
  handleAddEnrollment,
  handleDeleteEnrollment,
  handleDeleteMember,
  handleMarkBegunstigerFeeAsPaid,
  handleMarkMembershipAsPaid,
  handleSaveMember,
  handleUpdateEnrollmentStatus,
  loadMemberData,
} from "./edit-member.handlers";

/**
 * An administrative page for viewing and editing a member's complete profile.
 *
 * This comprehensive interface is designed for board members and administrators to:
 * - **Manage Personal & Contact Data**: Update identity, student number, and detailed address information.
 * - **Control Membership Status**: Toggle special statuses like "Honorary Member" (ere lid),
 *   "Benefactor" (begunstiger), or manage disciplinary "Suspensions".
 * - **Audit Educational History**: Manage multiple study enrollments, track graduation status,
 *   and add new educational records.
 * - **Internal Bookkeeping**: View and edit internal admin-only notes about the member.
 * - **Media Management**: Access the `ChangeProfilePicture` component to update the member's avatar.
 *
 * The layout uses a responsive two-column design on larger screens, placing the profile picture
 * as a sidebar and the multi-section form as the main content.
 *
 * @page
 * @component
 */
export default function EditMemberPage() {
  const navigate = useNavigate();
  const { id: memberId } = useParams<{ id: string }>();
  const [loading, setLoading] = useState(true);
  const [saving, setSaving] = useState(false);
  const [confirmModal, confirm] = useConfirm();
  const [markingPaid, setMarkingPaid] = useState(false);
  const [hasPaidMembership, setHasPaidMembership] = useState(true);
  const [isBegunstiger, setIsBegunstiger] = useState(false);
  const [_profilePictureSrc, setProfilePictureSrc] = useState<string | null>(
    null,
  );
  const [enrollments, setEnrollments] = useState<StudyEnrollmentResponseDto[]>(
    [],
  );
  const [groupMemberships, setGroupMemberships] = useState<
    GroupMembershipResponseDto[]
  >([]);
  const [availableStudies, setAvailableStudies] = useState<Study[]>([]);
  const [selectedStudyId, setSelectedStudyId] = useState<number | "">("");
  const [email, setEmail] = useState("");

  const [formData, setFormData] = useState({
    firstName: "",
    lastName: "",
    studentNumber: "",
    phoneNumber: "",
    street: "",
    houseNumber: "",
    postalCode: "",
    city: "",
    parentPhoneNumber: "",
    preferredLanguage: "NL",
    notes: "",
    gratie: false,
    lidVanVerdienste: false,
    ereLid: false,
    begunstiger: false,
    suspended: false,
    dateOfBirth: "",
  });

  const enrollmentColumns: Column<StudyEnrollmentResponseDto>[] = [
    {
      header: t("study"),
      render: (item) => item.studyTitle,
    },
    {
      header: t("start_date"),
      render: (item) => formatDate(new Date(item.enrollmentDate), "dateOnly"),
    },
    {
      header: t("status"),
      render: (item) => (
        <select
          value={item.status}
          onChange={(e) =>
            handleUpdateEnrollmentStatus(
              item.id,
              e.target.value as StudyStatus,
              setLoading,
              setEnrollments,
            )
          }
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
            handleDeleteEnrollment(item.id, setLoading, setEnrollments);
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
    const cleanupPromise = loadMemberData({
      memberId,
      setFormData,
      setEmail,
      setEnrollments,
      setGroupMemberships,
      setAvailableStudies,
      setProfilePictureSrc,
      setHasPaidMembership,
      setIsBegunstiger,
      setLoading,
    });
    return () => {
      cleanupPromise.then((cleanup) => cleanup?.());
    };
  }, [memberId]);

  if (loading) return t("loading");

  return (
    <>
      <PageHeader title="" backTo="/admin/members" />
      <div className="flex flex-col lg:flex-row gap-12">
        <ChangeProfilePicture
          userId={memberId!}
          isHonoraryOrMerit={Boolean(
            formData.ereLid || formData.lidVanVerdienste,
          )}
        />

        <Form className="w-full space-y-8">
          <FormSection title={t("personal_info")} columns={2}>
            <Input
              label={t("first_name")}
              value={formData.firstName}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                setFormData({ ...formData, firstName: e.target.value })
              }
            />
            <Input
              label={t("last_name")}
              value={formData.lastName}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                setFormData({ ...formData, lastName: e.target.value })
              }
            />
            <Input
              label={t("student_number")}
              value={formData.studentNumber}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                setFormData({
                  ...formData,
                  studentNumber: e.target.value,
                })
              }
              required
            />
            <Input
              label={t("date_of_birth")}
              type="date"
              value={formData.dateOfBirth}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                setFormData({ ...formData, dateOfBirth: e.target.value })
              }
            />
            <Input
              className="border-transparent bg-transparent p-0 cursor-default text-gray-900 disabled:text-gray-900"
              label={t("email")}
              type="email"
              value={email}
              disabled
            />
          </FormSection>

          {/* Contact & Adres */}
          <FormSection title={t("contact_and_address")} columns={2}>
            <Input
              label={t("phone_number")}
              value={formData.phoneNumber}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                setFormData({ ...formData, phoneNumber: e.target.value })
              }
            />
            <Input
              label={t("parent_phone_number")}
              value={formData.parentPhoneNumber}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                setFormData({ ...formData, parentPhoneNumber: e.target.value })
              }
            />
            <div className="md:col-span-2 grid grid-cols-3 gap-4">
              <div className="col-span-2">
                <Input
                  label={t("street")}
                  value={formData.street}
                  onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                    setFormData({ ...formData, street: e.target.value })
                  }
                />
              </div>
              <Input
                label={t("house_number")}
                value={formData.houseNumber}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                  setFormData({ ...formData, houseNumber: e.target.value })
                }
              />
            </div>
            <Input
              label={t("postal_code")}
              required
              value={formData.postalCode}
              onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                setFormData({ ...formData, postalCode: e.target.value })
              }
            />
            <div className="md:col-span-2">
              <Input
                label={t("city")}
                required
                value={formData.city}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                  setFormData({ ...formData, city: e.target.value })
                }
              />
            </div>
          </FormSection>

          {/* Administratieve Status (Nieuw) */}
          <section>
            <FormHeader title={t("status_and_membership")} />
            <Tile className="grid grid-cols-1 md:grid-cols-3 gap-4 p-5 bg-blue-50/50">
              <Checkbox
                label={t("gratie")}
                checked={formData.gratie}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                  setFormData({ ...formData, gratie: e.target.checked })
                }
              />
              <Checkbox
                label={t("lid_van_verdienste")}
                checked={formData.lidVanVerdienste}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                  setFormData({
                    ...formData,
                    lidVanVerdienste: e.target.checked,
                  })
                }
              />
              <Checkbox
                label={t("ere_lid")}
                checked={formData.ereLid}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                  setFormData({ ...formData, ereLid: e.target.checked })
                }
              />
              <Checkbox
                label={t("begunstiger")}
                checked={formData.begunstiger}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                  setFormData({ ...formData, begunstiger: e.target.checked })
                }
              />
              <Checkbox
                label={t("suspended")}
                checked={formData.suspended}
                className="text-red-600 font-bold"
                onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
                  setFormData({ ...formData, suspended: e.target.checked })
                }
              />
            </Tile>
            {!hasPaidMembership && (
              <div className="mt-4 flex flex-col sm:flex-row sm:items-center justify-between gap-3 rounded-xl border border-amber-200 bg-amber-50 px-4 py-3">
                <div className="flex items-center gap-2 text-amber-800">
                  <CircleAlert className="w-5 h-5 shrink-0" />
                  <span className="text-sm font-medium">
                    {isBegunstiger
                      ? t("begunstiger_fee_unpaid")
                      : t("membership_fee_unpaid")}
                  </span>
                </div>
                <Button
                  type="button"
                  variant="secondary"
                  className="border-amber-300 text-amber-800 hover:bg-amber-100 whitespace-nowrap"
                  onClick={async () => {
                    const label = isBegunstiger
                      ? t("mark_begunstiger_fee_as_paid")
                      : t("mark_membership_as_paid");
                    const message = isBegunstiger
                      ? t("are_you_sure_mark_begunstiger_fee_as_paid")
                      : t("are_you_sure_mark_membership_as_paid");

                    if (
                      !(await confirm(message, {
                        title: label,
                        confirmLabel: label,
                        variant: "primary",
                      }))
                    ) {
                      return;
                    }

                    const markAsPaid = isBegunstiger
                      ? handleMarkBegunstigerFeeAsPaid
                      : handleMarkMembershipAsPaid;
                    markAsPaid(memberId, setMarkingPaid, () =>
                      setHasPaidMembership(true),
                    );
                  }}
                  disabled={markingPaid}
                >
                  {isBegunstiger
                    ? t("mark_begunstiger_fee_as_paid")
                    : t("mark_membership_as_paid")}
                </Button>
              </div>
            )}
          </section>

          {/* Notities (Admin Only) */}
          <section>
            <FormHeader title={t("internal_notes")} />
            <textarea
              className="w-full p-3 border rounded-md min-h-[100px]"
              value={formData.notes}
              placeholder={t("internal_notes_placeholder")}
              onChange={(e) =>
                setFormData({ ...formData, notes: e.target.value })
              }
            />
          </section>

          <div className="flex flex-col sm:flex-row gap-4">
            <Button
              onClick={() => handleSaveMember(memberId, formData, setSaving)}
              disabled={saving}
              className="flex-1"
            >
              {saving ? t("saving") : t("save")}
            </Button>

            <Button
              type="button"
              variant="secondary"
              onClick={async () => {
                if (
                  !(await confirm(t("are_you_sure_delete_member"), {
                    title: t("delete"),
                    confirmLabel: t("delete"),
                  }))
                ) {
                  return;
                }
                handleDeleteMember(memberId, setLoading, () =>
                  navigate("/admin/members"),
                );
              }}
              className="bg-red-600 hover:bg-red-700 text-white border-transparent"
            >
              {t("delete")}
            </Button>
          </div>

          <section>
            <FormHeader title={t("group_memberships")} />
            <GroupMembershipOverview
              groupMemberships={groupMemberships}
              emptyText={t("no_group_memberships")}
              isClickable
            />
          </section>

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
                        setSelectedStudyId(parseInt(e.target.value, 10));
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
                      })),
                    ]}
                  />
                </div>

                <Button
                  variant="primary"
                  onClick={() =>
                    handleAddEnrollment(
                      memberId,
                      selectedStudyId,
                      setLoading,
                      setEnrollments,
                    )
                  }
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
      {confirmModal}
    </>
  );
}
