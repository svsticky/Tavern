import { fireEvent, render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import type { GetSpecificationQuestionResponseDto } from "~/api";
import EditQuestionTile from "~/components/Activity/Edit/EditQuestionTile";

function buildQuestion(
  overrides: Partial<GetSpecificationQuestionResponseDto> = {},
): Partial<GetSpecificationQuestionResponseDto> {
  return {
    questionDutch: "Vraag",
    questionEnglish: "Question",
    type: "String",
    isMandatory: false,
    isPublic: false,
    ...overrides,
  };
}

describe("EditQuestionTile", () => {
  it("renders the Dutch and English question inputs", () => {
    render(
      <EditQuestionTile
        question={buildQuestion()}
        onRemove={vi.fn()}
        onUpdate={vi.fn()}
      />,
    );
    expect(screen.getByDisplayValue("Vraag")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Question")).toBeInTheDocument();
  });

  it("calls onRemove when the remove button is clicked", () => {
    const onRemove = vi.fn();
    render(
      <EditQuestionTile
        question={buildQuestion()}
        onRemove={onRemove}
        onUpdate={vi.fn()}
      />,
    );
    fireEvent.click(screen.getByText("×"));
    expect(onRemove).toHaveBeenCalled();
  });

  it("calls onUpdate with the new Dutch question text", () => {
    const onUpdate = vi.fn();
    render(
      <EditQuestionTile
        question={buildQuestion()}
        onRemove={vi.fn()}
        onUpdate={onUpdate}
      />,
    );
    fireEvent.change(screen.getByDisplayValue("Vraag"), {
      target: { value: "Nieuwe vraag" },
    });
    expect(onUpdate).toHaveBeenCalledWith("questionDutch", "Nieuwe vraag");
  });

  it("calls onUpdate with the new English question text", () => {
    const onUpdate = vi.fn();
    render(
      <EditQuestionTile
        question={buildQuestion()}
        onRemove={vi.fn()}
        onUpdate={onUpdate}
      />,
    );
    fireEvent.change(screen.getByDisplayValue("Question"), {
      target: { value: "New question" },
    });
    expect(onUpdate).toHaveBeenCalledWith("questionEnglish", "New question");
  });

  it("calls onUpdate when the type select changes", () => {
    const onUpdate = vi.fn();
    render(
      <EditQuestionTile
        question={buildQuestion()}
        onRemove={vi.fn()}
        onUpdate={onUpdate}
      />,
    );
    // The select's option text is the translated label ("text"/"true_or_false"/...),
    // not the underlying enum value, so it can't be queried by display value - use its
    // own <label> (t("type") -> "type" in the test i18n setup) instead.
    fireEvent.change(screen.getByLabelText("type"), {
      target: { value: "Boolean" },
    });
    expect(onUpdate).toHaveBeenCalledWith("type", "Boolean");
  });

  it("calls onUpdate with the mandatory checkbox state", () => {
    const onUpdate = vi.fn();
    render(
      <EditQuestionTile
        question={buildQuestion()}
        onRemove={vi.fn()}
        onUpdate={onUpdate}
      />,
    );
    fireEvent.click(screen.getByLabelText("mandatory"));
    expect(onUpdate).toHaveBeenCalledWith("isMandatory", true);
  });

  it("calls onUpdate with the public checkbox state", () => {
    const onUpdate = vi.fn();
    render(
      <EditQuestionTile
        question={buildQuestion()}
        onRemove={vi.fn()}
        onUpdate={onUpdate}
      />,
    );
    fireEvent.click(screen.getByLabelText("public"));
    expect(onUpdate).toHaveBeenCalledWith("isPublic", true);
  });

  it("calls onUpdate with an association-timezone ISO string when the answer deadline changes", () => {
    const onUpdate = vi.fn();
    render(
      <EditQuestionTile
        question={buildQuestion()}
        onRemove={vi.fn()}
        onUpdate={onUpdate}
      />,
    );
    fireEvent.change(screen.getByLabelText("answer_deadline"), {
      target: { value: "2026-08-01T15:30" },
    });
    // 15:30 on 2026-08-01 in Europe/Amsterdam (CEST, UTC+2) is 13:30Z, regardless
    // of the entering device's own timezone.
    expect(onUpdate).toHaveBeenCalledWith(
      "answerDeadline",
      "2026-08-01T13:30:00.000Z",
    );
  });

  it("calls onUpdate with null when the answer deadline is cleared", () => {
    const onUpdate = vi.fn();
    render(
      <EditQuestionTile
        question={buildQuestion({ answerDeadline: "2026-08-01T13:30:00Z" })}
        onRemove={vi.fn()}
        onUpdate={onUpdate}
      />,
    );
    fireEvent.change(screen.getByLabelText("answer_deadline"), {
      target: { value: "" },
    });
    expect(onUpdate).toHaveBeenCalledWith("answerDeadline", null);
  });

  it("does not render the options input for non-MultipleChoice types", () => {
    render(
      <EditQuestionTile
        question={buildQuestion({ type: "String" })}
        onRemove={vi.fn()}
        onUpdate={vi.fn()}
      />,
    );
    expect(
      screen.queryByLabelText("options_semicolon_separated"),
    ).not.toBeInTheDocument();
  });

  it("renders the options input for MultipleChoice questions with joined values", () => {
    render(
      <EditQuestionTile
        question={buildQuestion({
          type: "MultipleChoice",
          options: ["A", "B"],
        })}
        onRemove={vi.fn()}
        onUpdate={vi.fn()}
      />,
    );
    expect(screen.getByDisplayValue("A; B")).toBeInTheDocument();
  });

  it("parses the semicolon-separated options input into an array", () => {
    const onUpdate = vi.fn();
    render(
      <EditQuestionTile
        question={buildQuestion({ type: "MultipleChoice", options: [] })}
        onRemove={vi.fn()}
        onUpdate={onUpdate}
      />,
    );
    fireEvent.change(screen.getByLabelText("options_semicolon_separated"), {
      target: { value: "One; Two ; ;Three" },
    });
    expect(onUpdate).toHaveBeenCalledWith("options", ["One", "Two", "Three"]);
  });
});
