import { useEffect, useState } from "react";
import { t } from "i18next";
import Button from "~/components/UI/Button";
import Select from "~/components/UI/Select";
import { type Study, type StudyType } from "~/api";
import TriStateFilter from "../../UI/TriStateFilter";
import { type MembersFilterDto } from "~/types/MembersFilterDto";
import { handleApplyFilters, handleResetFilters, loadStudies } from "./FilterMemberOverlay.handlers";

export default function FilterMemberOverlay({ filters, onFilter }: { filters: MembersFilterDto | null; onFilter: (filters: MembersFilterDto) => void }) {
  const [loading, setLoading] = useState(false);

  const [studies, setStudies] = useState<Study[]>([]);

  const [study, setStudy] = useState<Study | null>(filters?.studyId ? { id: filters.studyId, title: "" } : null);
  const [gratie, setGratie] = useState<boolean | null>(filters?.gratie ?? null);
  const [lidVanVerdienste, setLidVanVerdienste] = useState<boolean | null>(filters?.lidVanVerdienste ?? null);
  const [ereLid, setEreLid] = useState<boolean | null>(filters?.ereLid ?? null);
  const [begunstiger, setBegunstiger] = useState<boolean | null>(filters?.begunstiger ?? null);
  const [suspended, setSuspended] = useState<boolean | null>(filters?.suspended ?? null);
  const [inactive, setInactive] = useState<boolean | null>(filters?.inactive ?? null);
  const [studyType, setStudyType] = useState<StudyType | null>(filters?.studyType ?? null);

  useEffect(() => {
    loadStudies(setLoading, setStudies);
  }, []);

  return (
    <div className="space-y-4">
      <div className="grid grid-cols-1 gap-3">
        <Select 
          label={t("study_type")}
          value={studyType ?? ""}
          onChange={(e) => setStudyType(e.target.value as StudyType || null)}
          options={[
            { value: "", label: t("all_studies") },
            { value: "Bachelor", label: "Bachelor" },
            { value: "Master", label: "Master" },
          ]}
        />

        <Select 
          label={t("study")}
          value={study?.id ?? ""}
          onChange={(e) => setStudy(studies.find(s => s.id === Number(e.target.value)) || null)}
          options={
            [{ value: "", label: t("all_studies") }, ...studies.map(s => ({ value: s.id!.toString(), label: s.title }))]
          }
          disabled={loading || studies.length === 0}
        />

        <TriStateFilter label={t("gratie")} value={gratie} onChange={setGratie} />
        <TriStateFilter label={t("lid_van_verdienste")} value={lidVanVerdienste} onChange={setLidVanVerdienste} />
        <TriStateFilter label={t("ere_lid")} value={ereLid} onChange={setEreLid} />
        <TriStateFilter label={t("begunstiger")} value={begunstiger} onChange={setBegunstiger} />
        <TriStateFilter label={t("suspended")} value={suspended} onChange={setSuspended} />
        <TriStateFilter label={t("inactive")} value={inactive} onChange={setInactive} />
      </div>

      <div className="flex gap-2 pt-4">
        <Button
          variant="secondary"
          onClick={() =>
            handleResetFilters({
              setStudy,
              setGratie,
              setLidVanVerdienste,
              setEreLid,
              setBegunstiger,
              setSuspended,
              setInactive,
              setStudyType
            })
          }
          className="flex-1"
        >
          {t("reset")}
        </Button>
        <Button
          onClick={() =>
            handleApplyFilters({
              onFilter,
              studyId: study?.id || null,
              gratie,
              lidVanVerdienste,
              ereLid,
              begunstiger,
              suspended,
              inactive,
              studyType
            })
          }
          className="flex-1"
        >
          {t("apply_filters")}
        </Button>
      </div>
    </div>
  );
}
