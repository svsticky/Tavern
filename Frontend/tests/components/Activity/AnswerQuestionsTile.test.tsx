import { fireEvent, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { GetSpecificationQuestionResponseDto } from "~/api";
import AnswerQuestionsTile from "~/components/Activity/AnswerQuestionsTile";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

function question(
  overrides: Partial<GetSpecificationQuestionResponseDto>,
): GetSpecificationQuestionResponseDto {
  return {
    id: 1,
    questionDutch: "Vraag",
    questionEnglish: "Question",
    type: "String",
    isMandatory: false,
    ...overrides,
  } as GetSpecificationQuestionResponseDto;
}

const enToken: TokenParsed = {
  locale: "en",
  UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
  access_level: "member",
  given_name: "Test",
  family_name: "User",
  name: "Test User",
};

describe("AnswerQuestionsTile", () => {
  it("renders nothing while the token has not loaded", () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(() => new Promise<TokenParsed | null>(() => {})),
    });
    const { container } = renderWithProviders(
      <AnswerQuestionsTile
        questions={[question({})]}
        answers={{}}
        onChange={vi.fn()}
      />,
      { authService },
    );
    expect(container).toBeEmptyDOMElement();
  });

  it("renders nothing when there are no questions", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => enToken),
    });
    const { container } = renderWithProviders(
      <AnswerQuestionsTile questions={[]} answers={{}} onChange={vi.fn()} />,
      { authService },
    );
    await waitFor(() => expect(authService.getTokenParsed).toHaveBeenCalled());
    expect(container).toBeEmptyDOMElement();
  });

  it("renders the English question text for an English-locale user", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => enToken),
    });
    renderWithProviders(
      <AnswerQuestionsTile
        questions={[question({})]}
        answers={{}}
        onChange={vi.fn()}
      />,
      { authService },
    );
    expect(await screen.findByText("Question")).toBeInTheDocument();
  });

  it("renders the Dutch question text for a Dutch-locale user", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => ({ ...enToken, locale: "NL" })),
    });
    renderWithProviders(
      <AnswerQuestionsTile
        questions={[question({})]}
        answers={{}}
        onChange={vi.fn()}
      />,
      { authService },
    );
    expect(await screen.findByText("Vraag")).toBeInTheDocument();
  });

  it("shows a required asterisk for mandatory questions", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => enToken),
    });
    renderWithProviders(
      <AnswerQuestionsTile
        questions={[question({ isMandatory: true })]}
        answers={{}}
        onChange={vi.fn()}
      />,
      { authService },
    );
    expect(await screen.findByText("*")).toBeInTheDocument();
  });

  it("calls onChange with the entered text for a String question", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => enToken),
    });
    const onChange = vi.fn();
    const user = userEvent.setup();
    renderWithProviders(
      <AnswerQuestionsTile
        questions={[question({ type: "String" })]}
        answers={{}}
        onChange={onChange}
      />,
      { authService },
    );

    const input = await screen.findByRole("textbox");
    await user.type(input, "x");

    expect(onChange).toHaveBeenCalledWith(1, "x");
  });

  it("calls onChange with true/false strings for a Boolean question", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => enToken),
    });
    const onChange = vi.fn();
    const user = userEvent.setup();
    renderWithProviders(
      <AnswerQuestionsTile
        questions={[question({ type: "Boolean" })]}
        answers={{}}
        onChange={onChange}
      />,
      { authService },
    );

    const checkbox = await screen.findByRole("checkbox");
    await user.click(checkbox);

    expect(onChange).toHaveBeenCalledWith(1, "true");
  });

  it("renders a Select with the provided options for a MultipleChoice question", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => enToken),
    });
    renderWithProviders(
      <AnswerQuestionsTile
        questions={[question({ type: "MultipleChoice", options: ["A", "B"] })]}
        answers={{}}
        onChange={vi.fn()}
      />,
      { authService },
    );

    const select = await screen.findByRole("combobox");
    expect(select).toBeInTheDocument();
    expect(screen.getByRole("option", { name: "A" })).toBeInTheDocument();
    expect(screen.getByRole("option", { name: "B" })).toBeInTheDocument();
  });

  it("disables all inputs when disabled is true", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => enToken),
    });
    renderWithProviders(
      <AnswerQuestionsTile
        questions={[question({ type: "String" })]}
        answers={{}}
        onChange={vi.fn()}
        disabled
      />,
      { authService },
    );

    expect(await screen.findByRole("textbox")).toBeDisabled();
  });

  it("calls onChange with the entered value for a Number question", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => enToken),
    });
    const onChange = vi.fn();
    renderWithProviders(
      <AnswerQuestionsTile
        questions={[question({ type: "Number" })]}
        answers={{}}
        onChange={onChange}
      />,
      { authService },
    );

    const input = await screen.findByRole("spinbutton");
    fireEvent.change(input, { target: { value: "42" } });

    expect(onChange).toHaveBeenCalledWith(1, "42");
  });

  it("calls onChange with an association-timezone ISO string for a Date question", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => enToken),
    });
    const onChange = vi.fn();
    renderWithProviders(
      <AnswerQuestionsTile
        questions={[question({ type: "Date" })]}
        answers={{}}
        onChange={onChange}
      />,
      { authService },
    );

    await screen.findByText("Question");
    const input = document.querySelector(
      'input[type="date"]',
    ) as HTMLInputElement;
    fireEvent.change(input, { target: { value: "2026-08-01" } });

    // Midnight on 2026-08-01 in Europe/Amsterdam (CEST, UTC+2) is 2026-07-31T22:00Z,
    // regardless of the entering device's own timezone.
    expect(onChange).toHaveBeenCalledWith(1, "2026-07-31T22:00:00.000Z");
  });

  it("clears the answer when a Date question's value is emptied", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => enToken),
    });
    const onChange = vi.fn();
    renderWithProviders(
      <AnswerQuestionsTile
        questions={[question({ type: "Date" })]}
        answers={{ 1: "2026-07-31T22:00:00.000Z" }}
        onChange={onChange}
      />,
      { authService },
    );

    await screen.findByText("Question");
    const input = document.querySelector(
      'input[type="date"]',
    ) as HTMLInputElement;
    fireEvent.change(input, { target: { value: "" } });

    expect(onChange).toHaveBeenCalledWith(1, "");
  });

  it("calls onChange with an association-timezone ISO string for a DateTime question", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => enToken),
    });
    const onChange = vi.fn();
    renderWithProviders(
      <AnswerQuestionsTile
        questions={[question({ type: "DateTime" })]}
        answers={{ 1: "2026-08-01T10:00:00Z" }}
        onChange={onChange}
      />,
      { authService },
    );

    await screen.findByText("Question");
    const input = document.querySelector(
      'input[type="datetime-local"]',
    ) as HTMLInputElement;
    fireEvent.change(input, { target: { value: "2026-08-01T15:30" } });

    // 15:30 on 2026-08-01 in Europe/Amsterdam (CEST, UTC+2) is 13:30Z, regardless
    // of the entering device's own timezone.
    expect(onChange).toHaveBeenCalledWith(1, "2026-08-01T13:30:00.000Z");
  });

  it("calls onChange with the selected value for a MultipleChoice question", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => enToken),
    });
    const onChange = vi.fn();
    renderWithProviders(
      <AnswerQuestionsTile
        questions={[question({ type: "MultipleChoice", options: ["A", "B"] })]}
        answers={{}}
        onChange={onChange}
      />,
      { authService },
    );

    const select = await screen.findByRole("combobox");
    fireEvent.change(select, { target: { value: "B" } });

    expect(onChange).toHaveBeenCalledWith(1, "B");
  });

  it("skips rendering input for a question with no id", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => enToken),
    });
    renderWithProviders(
      <AnswerQuestionsTile
        questions={[question({ id: undefined })]}
        answers={{}}
        onChange={vi.fn()}
      />,
      { authService },
    );

    await screen.findByText("Question");
    expect(screen.queryByRole("textbox")).not.toBeInTheDocument();
  });

  it("renders nothing for a question with an unrecognized type", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => enToken),
    });
    renderWithProviders(
      <AnswerQuestionsTile
        questions={[question({ type: "Unknown" as any })]}
        answers={{}}
        onChange={vi.fn()}
      />,
      { authService },
    );

    await screen.findByText("Question");
    expect(screen.queryByRole("textbox")).not.toBeInTheDocument();
  });
});
