import { useKeycloak } from "@react-keycloak/web";
import { t } from "i18next";
import { useEffect, useState } from "react";
import { useNavigate } from "react-router";
import { 
  postApiActivities, 
  getApiGroups, 
  type TargetAudience, 
  type GroupResponseDto
} from "~/api";
import Button from "~/components/UI/Button";
import Checkbox from "~/components/UI/Checkbox";
import Input from "~/components/UI/Input";
import TextArea from "~/components/UI/TextArea";
import { getAssociationYear } from "~/util/date.util";
import { isInGroupWithId } from "~/util/group.util";

export default function CreateActivityPage() {
  const navigate = useNavigate();
  const [loading, setLoading] = useState(false);
  const [groups, setGroups] = useState<GroupResponseDto[]>([]);
  const [formValid, setFormValid] = useState(false);

  const { keycloak } = useKeycloak();

  useEffect(() => {
    async function fetchGroups() {
      try {
        const response = await getApiGroups({ 
          query: { 
            IncludeInactive: false,
            MembershipYear: getAssociationYear()
          } 
        });

        if(response.data) {
          setGroups(response.data);
        }
      } catch (error) {
        console.error("Error fetching groups:", error);
      }
    }

    fetchGroups();
  }, []);

  const handleFormChange = (e: React.FormEvent<HTMLFormElement>) => {
    const fd = new FormData(e.currentTarget);
    const name = fd.get("Name") as string;
    const start = fd.get("DateTimeStart") as string;
    const end = fd.get("DateTimeEnd") as string;
    const location = fd.get("Location") as string;
    const group = fd.get("OrganizerId") as string;

    setFormValid(!!(name && start && end && location && group));
  };

  if((keycloak.tokenParsed?.group_memberships ?? []).length === 0) {
    window.location.href = "/logout";
    return null;
  }

  const isBoard = isInGroupWithId(keycloak.tokenParsed, import.meta.env.BOARD_GROUP_ID);

  const handleSubmit = async (e: React.FormEvent<HTMLFormElement>) => {
    e.preventDefault();
    const fd = new FormData(e.currentTarget);
    
    const audienceFlags = fd.getAll("AudienceBit").reduce((acc, val) => acc + Number(val), 0);

    setLoading(true);
    try {
      await postApiActivities({
        body: {
          Poster: (fd.get("Poster") as File).size > 0 ? (fd.get("Poster") as File) : undefined,
        },
        query: {
          // Strings & Numbers
          Name: fd.get("Name") as string,
          Location: fd.get("Location") as string,
          DutchDescription: fd.get("DutchDescription") as string,
          EnglishDescription: fd.get("EnglishDescription") as string,
          Price: Number(fd.get("Price")) || 0,
          ParticipantLimit: fd.get("ParticipantLimit") ? Number(fd.get("ParticipantLimit")) : undefined,
          OrganizerId: fd.get("OrganizerId") ? Number(fd.get("OrganizerId")) : undefined,
          
          // Dates (ISO format)
          DateTimeStart: new Date(fd.get("DateTimeStart") as string).toISOString(),
          DateTimeEnd: new Date(fd.get("DateTimeEnd") as string).toISOString(),
          EnrollmentDeadline: fd.get("EnrollmentDeadline") ? new Date(fd.get("EnrollmentDeadline") as string).toISOString() : undefined,
          UnenrollmentDeadline: fd.get("UnenrollmentDeadline") ? new Date(fd.get("UnenrollmentDeadline") as string).toISOString() : undefined,

          // Booleans
          ShowInKoala: isBoard ? fd.get("ShowInKoala") === "on" : false,
          ShowOnWebsite: isBoard ? fd.get("ShowOnWebsite") === "on" : false,
          IsEnrollable: isBoard ? fd.get("IsEnrollable") === "on" : false,
          AreParticipantsVisible: fd.get("AreParticipantsVisible") === "on",
          IsAdultOnly: fd.get("IsAdultOnly") === "on",

          // Flags & Finance
          AllowedAudience: audienceFlags as TargetAudience,
          VatRate: isBoard ? fd.get("VatRate") ? Number(fd.get("VatRate")) : undefined : undefined,
          GLAccountId: isBoard ? fd.get("GLAccountId") as string || undefined : undefined,
          CostCenterId: isBoard ? fd.get("CostCenterId") as string || undefined : undefined,
          CostUnitId: isBoard ? fd.get("CostUnitId") as string || undefined : undefined,

          SpecificationQuestions: [], 
        },
      });
      navigate("/admin/activities");
    } catch (error) {
      console.error(error);
      alert("Fout bij opslaan.");
    } finally {
      setLoading(false);
    }
  };

  return (
    <>
        <Button
          showArrow
          arrowDirection="left"
          className="bg-transparent p-0 hover:bg-transparent text-(--board-primary) hover:text-(--board-primary-light) shadow-none"
          onClick={() => navigate("/activities")}
        >
          {t("back")}
        </Button>
        <form onSubmit={handleSubmit} onChange={handleFormChange} className="space-y-8">
          <h1 className="text-2xl font-bold">{t("activities")}</h1>
          
          <section className="grid grid-cols-1 md:grid-cols-2 gap-6">
            <div className="md:col-span-2"><h2 className="font-bold border-b pb-2 uppercase text-xs text-gray-500">Basis Informatie</h2></div>
            <Input label={t("name")} name="Name" required />
            <Input label={t("location")} name="Location" required />
            <TextArea label={t("dutch_description")} name="DutchDescription" />
            <TextArea label={t("english_description")} name="EnglishDescription" />
          </section>

          <section className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
            <div className="md:col-span-2 lg:col-span-4"><h2 className="font-bold border-b pb-2 uppercase text-xs text-gray-500">Planning & Inschrijving</h2></div>
            <Input label={t("datetime_start")} name="DateTimeStart" type="datetime-local" required />
            <Input label={t("datetime_end")} name="DateTimeEnd" type="datetime-local" required />
            <Input label={t("enrollment_deadline")} name="EnrollmentDeadline" type="datetime-local" />
            <Input label={t("unenrollment_deadline")} name="UnenrollmentDeadline" type="datetime-local" />
          </section>

          <section className="grid grid-cols-1 md:grid-cols-2 gap-8">
            <div>
              <h2 className="font-bold mb-3 uppercase text-xs text-gray-500">Doelgroep (Selecteer alle relevante)</h2>
              <div className="flex flex-wrap gap-4 p-4 bg-gray-50 rounded-xl">
                <Checkbox label={t("year_1")} name="AudienceBit" value="1" defaultChecked />
                <Checkbox label={t("year_2")} name="AudienceBit" value="2" defaultChecked />
                <Checkbox label={t("year_3_plus")} name="AudienceBit" value="4" defaultChecked />
                <Checkbox label={t("masters")} name="AudienceBit" value="8" defaultChecked />
              </div>
            </div>
            <div>
              <h2 className="font-bold mb-3 uppercase text-xs text-gray-500">{t("organizer")}</h2>
              <select name="OrganizerId" className="w-full border p-2.5 rounded-lg bg-white mt-1">
                <option value="">{t("select_organizer")}</option>
                {groups.map(g => <option key={g.id} value={g.id}>{g.name}</option>)}
              </select>
            </div>
          </section>

          <section className="grid grid-cols-2 md:grid-cols-5 gap-4">
            <div className="col-span-full"><h2 className="font-bold border-b pb-2 uppercase text-xs text-gray-500">Financieel & Capaciteit</h2></div>
            <Input label={t("price")} name="Price" type="number" step="0.01" />
            <Input label={t("participant_limit")} name="ParticipantLimit" type="number" />
            {isBoard && (
              <>
                <Input label={t("vat_rate")} name="VatRate" type="number" placeholder="21" />
                <Input label={t("gl_account_id")} name="GLAccountId" />
                <Input label={t("cost_center_id")} name="CostCenterId" />
                <Input label={t("cost_unit_id")} name="CostUnitId" />
              </>
            )}
          </section>

          <section className="grid grid-cols-1 md:grid-cols-2 gap-8">
            <div className="grid grid-cols-2 gap-4">
              <div className="col-span-2"><h2 className="font-bold uppercase text-xs text-gray-500 mb-2">{t("settings")}</h2></div>
              {isBoard && (
                <>
                  <Checkbox label={t("is_enrollable")} name="IsEnrollable" defaultChecked />
                  <Checkbox label={t("show_in_koala")} name="ShowInKoala" defaultChecked />
                  <Checkbox label={t("show_on_website")} name="ShowOnWebsite" defaultChecked />
                </>
              )}
              <Checkbox label={t("are_participants_visible")} name="AreParticipantsVisible" defaultChecked />
              <Checkbox label={t("is_adult_only")} name="IsAdultOnly" />
            </div>
            <div>
              <h2 className="font-bold uppercase text-xs text-gray-500 mb-2">{t("poster")}</h2>
              <input name="Poster" type="file" className="w-full p-2 border border-dashed rounded-lg" />
            </div>
          </section>

          <Button
            type="submit"
            disabled={loading || !formValid}
            className="w-full"
          >
            {loading ? t("saving") : t("create_activity")}
          </Button>
        </form>
    </>
  );
}