import { t } from "i18next";
import type { GetSpecificationQuestionResponseDto } from "~/api";
import Checkbox from "~/components/UI/Checkbox";
import Input from "~/components/UI/Input";
import Select from "~/components/UI/Select";
import BorderedTile from "../../Tiles/BorderedTile";

/**
 * A specialized form tile for creating or editing an activity specification question.
 *
 * Features:
 * - **Bilingual Support**: Provides inputs for both Dutch and English versions of the question.
 * - **Dynamic Type Selection**: Supports various data types (String, Boolean, Number, etc.)
 *   via a dropdown.
 * - **Conditional Rendering**: Displays an additional input field for options if the
 *   question type is set to 'MultipleChoice'.
 * - **Semicolon Parsing**: Automatically splits and trims string input into an array
 *   of options for multiple-choice questions.
 * - **Interactive Deletion**: Displays a floating "remove" button when the tile is hovered.
 *
 * @component
 * @param {Object} props - The component props.
 * @param {Partial<GetSpecificationQuestionResponseDto>} props.question - The current state of the question being edited.
 * @param {() => void} props.onRemove - Callback to remove this question from the parent state.
 * @param {(field: keyof GetSpecificationQuestionResponseDto, value: any) => void} props.onUpdate - Callback to update a specific property of the question.
 *
 * @example
 * ```tsx
 * <EditQuestionTile
 *   question={myQuestion}
 *   onRemove={() => handleRemove(index)}
 *   onUpdate={(field, value) => handleUpdate(index, field, value)}
 * />
 * ```
 */
export default function EditQuestionTile({
  question,
  onRemove,
  onUpdate,
}: {
  question: Partial<GetSpecificationQuestionResponseDto>;
  onRemove: () => void;
  onUpdate: (
    field: keyof GetSpecificationQuestionResponseDto,
    value: any,
  ) => void;
}) {
  return (
    <BorderedTile className="group relative overflow-visible">
      <button
        type="button"
        onClick={onRemove}
        className="absolute -top-2 -right-2 bg-red-500 text-white rounded-full w-6 h-6 flex items-center justify-center hover:bg-red-600 shadow-sm z-10 hover:cursor-pointer opacity-0 group-hover:opacity-100 transition-opacity"
      >
        ×
      </button>

      <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
        <Input
          label={t("question_dutch")}
          value={question.questionDutch}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
            onUpdate("questionDutch", e.target.value)
          }
          required
        />
        <Input
          label={t("question_english")}
          value={question.questionEnglish}
          onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
            onUpdate("questionEnglish", e.target.value)
          }
          required
        />

        <Select
          label={t("type")}
          value={question.type}
          onChange={(e) => onUpdate("type", e.target.value)}
          options={[
            { value: "String", label: "String" },
            { value: "Boolean", label: "Boolean" },
            { value: "Number", label: "Number" },
            { value: "Date", label: "Date" },
            { value: "DateTime", label: "DateTime" },
            { value: "MultipleChoice", label: "Multiple Choice" },
          ]}
        />

        <div className="flex items-center gap-6 mt-8">
          <Checkbox
            label={t("mandatory")}
            defaultChecked={question.isMandatory}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              onUpdate("isMandatory", e.target.checked)
            }
          />
          <Checkbox
            label={t("public")}
            defaultChecked={question.isPublic}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              onUpdate("isPublic", e.target.checked)
            }
          />
        </div>
      </div>

      {question.type === "MultipleChoice" && (
        <div className="mt-4">
          <Input
            label={t("options_semicolon_separated")}
            placeholder="Option 1; Option 2"
            defaultValue={question.options?.join("; ")}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) =>
              onUpdate(
                "options",
                e.target.value
                  .split(";")
                  .map((s) => s.trim())
                  .filter((s) => s !== ""),
              )
            }
          />
        </div>
      )}
    </BorderedTile>
  );
}
