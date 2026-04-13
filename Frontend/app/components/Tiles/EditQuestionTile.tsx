import { t } from "i18next";
import type { GetSpecificationQuestionResponseDto } from "~/api";
import Tile from "~/components/Tiles/Tile";
import Checkbox from "~/components/UI/Checkbox";
import Input from "~/components/UI/Input";
import Select from "~/components/UI/Select";
import BorderedTile from "./BorderedTile";

interface Props {
  question: Partial<GetSpecificationQuestionResponseDto>;
  onRemove: () => void;
  onUpdate: (field: keyof GetSpecificationQuestionResponseDto, value: any) => void;
}

export default function EditQuestionTile({ question, onRemove, onUpdate }: Props) {
  return (
    <BorderedTile className="group relative">
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
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => onUpdate("questionDutch", e.target.value)} 
          required
        />
        <Input 
          label={t("question_english")} 
          value={question.questionEnglish} 
          onChange={(e: React.ChangeEvent<HTMLInputElement>) => onUpdate("questionEnglish", e.target.value)} 
          required
        />
        
        <Select 
          label={t("type")}
          value={question.type}
          onChange={(e) => onUpdate("type", Number(e.target.value))}
          options={[
            { value: 0, label: "String" },
            { value: 1, label: "Boolean" },
            { value: 2, label: "Number" },
            { value: 3, label: "Date" },
            { value: 4, label: "DateTime" },
            { value: 5, label: "Multiple Choice" }
          ]}
        />

        <div className="flex items-center gap-6 mt-8">
          <Checkbox 
            label={t("mandatory")} 
            defaultChecked={question.isMandatory} 
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => onUpdate("isMandatory", e.target.checked)} 
          />
          <Checkbox 
            label={t("public")} 
            defaultChecked={question.isPublic} 
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => onUpdate("isPublic", e.target.checked)} 
          />
        </div>
      </div>

      {question.type === 'MultipleChoice' && (
        <div className="mt-4">
          <Input 
            label={t("options_semicolon_separated")}
            placeholder="Option 1; Option 2" 
            defaultValue={question.options?.join("; ")}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => 
              onUpdate("options", e.target.value.split(";").map(s => s.trim()).filter(s => s !== ""))
            }
          />
        </div>
      )}
    </BorderedTile>
  );
}