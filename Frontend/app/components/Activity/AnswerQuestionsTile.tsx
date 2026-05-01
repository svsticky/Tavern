import { useKeycloak } from "@react-keycloak/web";
import { t } from "i18next";
import { useEffect, useState } from "react";
import type {
  GetSpecificationQuestionResponseDto,
  SpecificationAnswerResponseDto,
} from "~/api";
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
 * - **State Syncing**: Initializes local state with existing `enrollmentAnswers` and
 *   notifies parent components via `onChange` whenever a value is modified.
 * - **Validation Visuals**: Appends a red asterisk to labels for mandatory questions.
 *
 * @component
 * @param {Object} props - The component props.
 * @param {GetSpecificationQuestionResponseDto[]} props.questions - The list of question definitions to render.
 * @param {SpecificationAnswerResponseDto[]} props.answers - Existing answers for the current enrollment to pre-populate the form.
 * @param {boolean} [props.disabled=false] - If true, prevents user interaction with all input fields.
 * @param {(answers: Record<number, string>) => void} [props.onChange] - Callback triggered on every state change, providing a map of question IDs to answer strings.
 *
 * @example
 * ```tsx
 * <AnswerQuestionsTile
 *   questions={activity.specificationQuestions}
 *   answers={enrollment.specificationAnswers}
 *   onChange={(newAnswers) => setFormData(newAnswers)}
 * />
 * ```
 */
export default function AnswerQuestionsTile({
  questions,
  answers: enrollmentAnswers,
  disabled = false,
  onChange,
}: {
  questions: GetSpecificationQuestionResponseDto[];
  answers: SpecificationAnswerResponseDto[];
  disabled?: boolean;
  onChange?: (answers: Record<number, string>) => void;
}) {
  const { keycloak } = useKeycloak();

  const [answers, setAnswers] = useState<Record<number, string>>({});

  useEffect(() => {
    if (!enrollmentAnswers) return;

    const existing: Record<number, string> = {};

    enrollmentAnswers.forEach((a) => {
      if (a.questionId !== undefined) {
        existing[a.questionId] = a.answer;
      }
    });

    setAnswers(existing);
  }, [enrollmentAnswers]);

  useEffect(() => {
    onChange?.(answers);
  }, [answers, onChange]);

  const setValue = (questionId: number, value: string) => {
    setAnswers((prev) => ({ ...prev, [questionId]: value }));
  };

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
              setValue(id, e.target.value)
            }
            disabled={disabled}
          />
        );

      case "Boolean":
        return (
          <Input
            type="checkbox"
            checked={value === "true"}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              setValue(id, e.target.checked ? "true" : "false")
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
              setValue(id, e.target.value)
            }
            disabled={disabled}
          />
        );

      case "Date":
        return (
          <Input
            type="date"
            className="input"
            value={value}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              setValue(id, e.target.value)
            }
            disabled={disabled}
          />
        );

      case "DateTime":
        return (
          <Input
            type="datetime-local"
            className="input"
            value={value}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              setValue(id, e.target.value)
            }
            disabled={disabled}
          />
        );

      case "MultipleChoice": {
        const options = q.options ?? [];

        return (
          <Select
            className="input"
            value={value}
            onChange={(e) => setValue(id, e.target.value)}
            options={options.map((opt) => ({ label: opt, value: opt }))}
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
