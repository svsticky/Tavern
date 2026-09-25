import { t } from "i18next";
import type {
  ActivityResponseDto,
  GetSpecificationQuestionResponseDto,
} from "~/api";
import { useTokenParsed } from "~/context/AuthContext";
import { isQuestionAnswerable } from "~/util/answer.util";
import {
  formatDateOnly,
  formatForInput,
  parseInputAsAssociationTime,
} from "~/util/date.util";
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
 *   user's locale preference.
 * - **Controlled Inputs**: Uses parent-owned answer state, so rerenders never
 *   reset in-progress typing.
 * - **Validation Visuals**: Appends a red asterisk to labels for mandatory questions.
 * - **Per-question deadlines**: Each question is individually disabled once its own answer
 *   deadline (see `isQuestionAnswerable`) has passed, regardless of the blanket `disabled` prop.
 *
 * @component
 * @param {Object} props - The component props.
 * @param {GetSpecificationQuestionResponseDto[]} props.questions - The list of question definitions to render.
 * @param {ActivityResponseDto} props.activity - The activity the questions belong to, used to resolve each question's effective answer deadline.
 * @param {Record<number, string>} props.answers - Current answers keyed by question id.
 * @param {boolean} [props.disabled=false] - If true, prevents user interaction with all input fields in addition to any that are locked by their own deadline.
 * @param {(id: number, value: string) => void} props.onChange - Callback triggered for each input change.
 *
 * @example
 * ```tsx
 * <AnswerQuestionsTile
 *   questions={activity.specificationQuestions}
 *   activity={activity}
 *   answers={formData}
 *   onChange={(id, value) => setFormData((prev) => ({ ...prev, [id]: value }))}
 * />
 * ```
 */
export default function AnswerQuestionsTile({
  questions,
  activity,
  answers,
  disabled = false,
  onChange,
}: {
  questions: GetSpecificationQuestionResponseDto[];
  activity: ActivityResponseDto;
  answers: Record<number, string>;
  disabled?: boolean;
  onChange: (id: number, value: string) => void;
}) {
  const tokenParsed = useTokenParsed();

  if (!tokenParsed) return null;

  const renderInput = (q: GetSpecificationQuestionResponseDto) => {
    if (q.id === undefined) return null;

    const id = q.id;

    const value = answers[id] || "";
    const questionDisabled = disabled || !isQuestionAnswerable(q, activity);

    switch (q.type) {
      case "String":
        return (
          <Input
            className="input"
            value={value}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              onChange(id, e.target.value)
            }
            disabled={questionDisabled}
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
            disabled={questionDisabled}
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
            disabled={questionDisabled}
            required={q.isMandatory}
          />
        );

      case "Date":
        return (
          <Input
            type="date"
            className="input"
            value={formatDateOnly(value)}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
              const raw = e.target.value;
              onChange(
                id,
                raw ? parseInputAsAssociationTime(raw).toISOString() : "",
              );
            }}
            disabled={questionDisabled}
            required={q.isMandatory}
          />
        );

      case "DateTime":
        return (
          <Input
            type="datetime-local"
            className="input"
            value={formatForInput(value)}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => {
              const raw = e.target.value;
              onChange(
                id,
                raw ? parseInputAsAssociationTime(raw).toISOString() : "",
              );
            }}
            disabled={questionDisabled}
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
            options={[
              { label: t("select_option"), value: "" },
              ...options.map((opt) => ({ label: opt, value: opt })),
            ]}
            disabled={questionDisabled}
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
              {tokenParsed.locale === "NL"
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
