import { useKeycloak } from "@react-keycloak/web";
import { t } from "i18next";
import type { GetSpecificationQuestionResponseDto } from "~/api";
import Tile from "../Tiles/Tile";
import Input from "../UI/Input";
import Select from "../UI/Select";

/**
 * A dynamic form component that renders a list of activity-specific questions.
 *
 * Features:
 * - **Polymorphic Inputs**: Automatically switches between `Input` (text, number, date, checkbox)
 *   and `Select` components based on the `question.type`.
 * - **Localization**: Displays question labels in Dutch or English based on the
 *   Keycloak user's locale preference.
 * - **Controlled Inputs**: Uses parent-owned answer state, so rerenders never
 *   reset in-progress typing.
 * - **Validation Visuals**: Appends a red asterisk to labels for mandatory questions.
 *
 * @component
 * @param {Object} props - The component props.
 * @param {GetSpecificationQuestionResponseDto[]} props.questions - The list of question definitions to render.
 * @param {Record<number, string>} props.answers - Current answers keyed by question id.
 * @param {boolean} [props.disabled=false] - If true, prevents user interaction with all input fields.
 * @param {(id: number, value: string) => void} props.onChange - Callback triggered for each input change.
 *
 * @example
 * ```tsx
 * <AnswerQuestionsTile
 *   questions={activity.specificationQuestions}
 *   answers={formData}
 *   onChange={(id, value) => setFormData((prev) => ({ ...prev, [id]: value }))}
 * />
 * ```
 */
export default function AnswerQuestionsTile({
  questions,
  answers,
  disabled = false,
  onChange,
}: {
  questions: GetSpecificationQuestionResponseDto[];
  answers: Record<number, string>;
  disabled?: boolean;
  onChange: (id: number, value: string) => void;
}) {
  const { keycloak } = useKeycloak();

  const renderInput = (q: GetSpecificationQuestionResponseDto) => {
    if (q.id === undefined) return null;

    const id = q.id;

    const value = answers[id] || "";

    switch (q.type) {
      case "String":
        return (
          <Input
            className="input"
            value={value}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              onChange(id, e.target.value)
            }
            disabled={disabled}
            required={q.isMandatory}
          />
        );

      case "Boolean":
        return (
          <Input
            type="checkbox"
            checked={value === "true"}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              onChange(id, e.target.checked ? "true" : "false")
            }
            disabled={disabled}
          />
        );

      case "Number":
        return (
          <Input
            type="number"
            className="input"
            value={value}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              onChange(id, e.target.value)
            }
            disabled={disabled}
            required={q.isMandatory}
          />
        );

      case "Date":
        return (
          <Input
            type="date"
            className="input"
            value={value}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              onChange(id, e.target.value)
            }
            disabled={disabled}
            required={q.isMandatory}
          />
        );

      case "DateTime":
        return (
          <Input
            type="datetime-local"
            className="input"
            value={value}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              onChange(id, e.target.value)
            }
            disabled={disabled}
            required={q.isMandatory}
          />
        );

      case "MultipleChoice": {
        const options = q.options ?? [];

        return (
          <Select
            className="input"
            value={value}
            onChange={(e) => onChange(id, e.target.value)}
            options={options.map((opt) => ({ label: opt, value: opt }))}
            required={q.isMandatory}
          />
        );
      }

      default:
        return null;
    }
  };

  if (questions.length === 0) return null;

  return (
    <Tile>
      <h3 className="font-bold mb-4">{t("questions")}</h3>

      <div className="flex flex-col gap-4">
        {questions.map((q) => (
          <div key={q.id}>
            <label className="font-semibold block mb-1">
              {keycloak.tokenParsed?.locale === "NL"
                ? q.questionDutch
                : q.questionEnglish}

              {q.isMandatory && <span className="text-red-500 ml-1">*</span>}
            </label>

            {renderInput(q)}
          </div>
        ))}
      </div>
    </Tile>
  );
}
