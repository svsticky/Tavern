import { useEffect, useState } from "react";
import type { GetSpecificationQuestionResponseDto, SpecificationAnswerResponseDto, SpecificationQuestion } from "~/api";
import Tile from "./Tile";
import { useKeycloak } from "@react-keycloak/web";
import Input from "../UI/Input";
import { t } from "i18next";

type Props = {
  questions: GetSpecificationQuestionResponseDto[];
  answers: SpecificationAnswerResponseDto[];
  disabled?: boolean;
  onChange?: (answers: Record<number, string>) => void;
};

export default function AnswerQuestionsTile({
  questions,
  answers: enrollmentAnswers,
  disabled = false,
  onChange
}: Props) {
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
}, []);

  useEffect(() => {
    onChange?.(answers);
  }, [answers]);

  const setValue = (questionId: number, value: string) => {
    setAnswers(prev => ({ ...prev, [questionId]: value }));
  };

  const renderInput = (q: GetSpecificationQuestionResponseDto) => {
    if(q.id === undefined) return null;

    const id = q.id;

    const value = answers[id] || "";

    switch (q.type) {
      case 'String':
        return (
          <Input
            className="input"
            value={value}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => setValue(id, e.target.value)}
            disabled={disabled}
          />
        );

      case 'Boolean':
        return (
          <Input
            type="checkbox"
            checked={value === "true"}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => setValue(id, e.target.checked ? "true" : "false")}
            disabled={disabled}
          />
        );
        
      case 'Number':
        return (
          <Input
            type="number"
            className="input"
            value={value}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => setValue(id, e.target.value)}
            disabled={disabled}
          />
        );

      case 'Date':
        return (
          <Input
            type="date"
            className="input"
            value={value}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => setValue(id, e.target.value)}
            disabled={disabled}
          />
        );

      case 'DateTime':
        return (
          <Input
            type="datetime-local"
            className="input"
            value={value}
            onChange={(e: React.ChangeEvent<HTMLInputElement>) => setValue(id, e.target.value)}
            disabled={disabled}
          />
        );

      case 'MultipleChoice':
        const options = q.options ?? [];

        return (
          <select
            className="input"
            value={value}
            onChange={e => setValue(id, e.target.value)}
          >
            <option value="">--</option>
            {options.map(opt => (
              <option key={opt} value={opt}>{opt}</option>
            ))}
          </select>
        );

      default:
        return null;
    }
  };

  if (questions.length === 0) return null;

  return (
    <Tile>
      <h3 className="font-bold mb-4">{t("questions")}</h3>

      <div className="flex flex-col gap-4">
        {questions.map(q => (
          <div key={q.id}>
            <label className="font-semibold block mb-1">
              {keycloak.tokenParsed?.locale === "NL"
                ? q.questionDutch
                : q.questionEnglish}

              {q.isMandatory && (
                <span className="text-red-500 ml-1">*</span>
              )}
            </label>

            {renderInput(q)}
          </div>
        ))}
      </div>
    </Tile>
  );
}