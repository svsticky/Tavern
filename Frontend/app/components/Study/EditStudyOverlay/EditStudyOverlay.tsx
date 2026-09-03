import { t } from "i18next";
import { useState } from "react";
import type { Study, StudyType } from "~/api";
import Button from "../../UI/Button";
import Checkbox from "../../UI/Checkbox";
import Form from "../../UI/Form/Form";
import Input from "../../UI/Input";
import Select from "../../UI/Select";
import {
  handleStudyDelete,
  handleStudySubmit,
} from "./EditStudyOverlay.handlers";

/**
 * An overlay component used for creating or editing study program information.
 *
 * It manages a local form state for study details like title, degree type,
 * and duration. If a `study` object is provided, the form initializes in "Edit" mode,
 * allowing the user to update or delete the record. Otherwise, it functions in
 * "Create" mode.
 *
 * @component
 * @param {Object} props - Component properties.
 * @param {function} props.onStudyAdded - Callback triggered when a study is successfully created, updated, or deleted.
 * @param {Study} [props.study] - Optional existing study data; if present, the component switches to edit/delete mode.
 */
export default function EditStudyOverlay({
  onStudyAdded: onComplete,
  study = undefined,
}: {
  onStudyAdded: (study?: Study) => void;
  study?: Study;
}) {
  const [formData, setFormData] = useState({
    title: study ? study.title : "",
    type: study?.type ?? "Bachelor",
    nominalDurationYears: study?.nominalDurationYears,
    active: study?.active ?? true,
  });
  const [loading, setLoading] = useState(false);

  return (
    <Form
      onSubmit={(e) =>
        handleStudySubmit({ e, formData, study, setLoading, onComplete })
      }
    >
      <Input
        label={t("name")}
        value={formData.title}
        onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
          setFormData({ ...formData, title: e.target.value })
        }
        required
      />

      <Select
        label={t("study_type")}
        value={formData.type}
        onChange={(e) =>
          setFormData({ ...formData, type: e.target.value as StudyType })
        }
        options={[
          { value: "Bachelor", label: t("bachelor") },
          { value: "Master", label: t("master") },
        ]}
        required
      />

      <Input
        label={t("nominal_duration")}
        type="number"
        min="1"
        required
        value={formData.nominalDurationYears}
        onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
          setFormData({
            ...formData,
            nominalDurationYears: Number(e.target.value),
          })
        }
      />

      {study && (
        <Checkbox
          label={t("active")}
          checked={formData.active}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
            setFormData({ ...formData, active: e.target.checked })
          }
        />
      )}

      <Button
        variant="primary"
        className="flex-1"
        disabled={
          loading ||
          !formData.title ||
          !formData.type ||
          !formData.nominalDurationYears
        }
        type="submit"
      >
        {study ? t("save") : t("create")}
      </Button>

      {study && (
        <Button
          variant="danger"
          className="flex-1"
          onClick={() => handleStudyDelete({ study, setLoading, onComplete })}
        >
          {t("delete")}
        </Button>
      )}
    </Form>
  );
}
