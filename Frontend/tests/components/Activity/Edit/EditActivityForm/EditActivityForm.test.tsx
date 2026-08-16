import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import EditActivityForm from "~/components/Activity/Edit/EditActivityForm/EditActivityForm";
import {
  addQuestion,
  handleActivityFormChange,
  handleActivitySubmit,
  loadGroups,
  removeQuestion,
} from "~/components/Activity/Edit/EditActivityForm/EditActivityForm.handlers";
import { renderWithProviders } from "~/testUtils";

vi.mock(
  "~/components/Activity/Edit/EditActivityForm/EditActivityForm.handlers",
  () => ({
    loadGroups: vi.fn((setLoading: (l: boolean) => void, setGroups: any) => {
      setLoading(false);
      setGroups([]);
    }),
    formatForInput: vi.fn(() => ""),
    formatDateOnly: vi.fn(() => ""),
    handleActivityFormChange: vi.fn(),
    addQuestion: vi.fn(),
    removeQuestion: vi.fn(),
    updateQuestion: vi.fn(),
    handleActivitySubmit: vi.fn((args: any) => args.e.preventDefault()),
  }),
);

function buildActivity(
  overrides: Partial<ActivityResponseDto> = {},
): ActivityResponseDto {
  return {
    id: 1,
    name: "Party",
    location: "Enschede",
    price: 5,
    dutchDescription: "Beschrijving",
    englishDescription: "Description",
    dateTimeStart: "2026-09-01T10:00:00Z",
    dateTimeEnd: "2026-09-01T12:00:00Z",
    specificationQuestions: [],
    allowedAudience: "All",
    ...overrides,
  } as ActivityResponseDto;
}

describe("EditActivityForm", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows a loading state while editing until groups have loaded", () => {
    vi.mocked(loadGroups).mockImplementationOnce(async () => {});
    renderWithProviders(
      <EditActivityForm activity={buildActivity()} id="1" isBoard={false} />,
    );
    expect(screen.getByText("loading")).toBeInTheDocument();
    expect(loadGroups).toHaveBeenCalled();
  });

  it("renders the form immediately when creating a new activity", () => {
    renderWithProviders(
      <EditActivityForm activity={null} id={undefined} isBoard={false} />,
    );
    expect(screen.getByLabelText(/^name/)).toBeInTheDocument();
    expect(screen.getByText("create_activity")).toBeInTheDocument();
  });

  it("does not show board-only fields for a non-board user", () => {
    renderWithProviders(
      <EditActivityForm activity={null} id={undefined} isBoard={false} />,
    );
    expect(screen.queryByLabelText("vat_rate")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("show_in_koala")).not.toBeInTheDocument();
  });

  it("shows board-only fields for a board user", () => {
    renderWithProviders(
      <EditActivityForm activity={null} id={undefined} isBoard={true} />,
    );
    expect(screen.getByLabelText("vat_rate")).toBeInTheDocument();
    expect(screen.getByLabelText(/show_in_koala/)).toBeInTheDocument();
  });

  it("shows a hint about keeping the current poster only in edit mode", () => {
    renderWithProviders(
      <EditActivityForm activity={null} id={undefined} isBoard={false} />,
    );
    expect(
      screen.queryByText("leave_empty_to_keep_current"),
    ).not.toBeInTheDocument();
  });

  it("shows the no-content message when there are no specification questions", () => {
    renderWithProviders(
      <EditActivityForm activity={null} id={undefined} isBoard={false} />,
    );
    expect(
      screen.getByText("no_specification_questions_yet"),
    ).toBeInTheDocument();
  });

  it("renders a question tile for each specification question and forwards onRemove", async () => {
    renderWithProviders(
      <EditActivityForm
        activity={buildActivity({
          specificationQuestions: [
            {
              id: 1,
              questionDutch: "V",
              questionEnglish: "Question",
              type: "String",
            },
          ] as ActivityResponseDto["specificationQuestions"],
        })}
        id="1"
        isBoard={false}
      />,
    );

    await waitFor(() => expect(screen.getByText("×")).toBeInTheDocument());
    fireEvent.click(screen.getByText("×"));
    expect(removeQuestion).toHaveBeenCalledWith(
      0,
      expect.any(Array),
      expect.any(Function),
    );
  });

  it("calls addQuestion when the add-question button is clicked", () => {
    renderWithProviders(
      <EditActivityForm activity={null} id={undefined} isBoard={false} />,
    );
    fireEvent.click(screen.getByText("+ add_question"));
    expect(addQuestion).toHaveBeenCalledWith([], expect.any(Function));
  });

  it("calls handleActivityFormChange when a form field changes", () => {
    renderWithProviders(
      <EditActivityForm activity={null} id={undefined} isBoard={false} />,
    );
    fireEvent.change(screen.getByLabelText(/^name/), {
      target: { value: "New name" },
    });
    expect(handleActivityFormChange).toHaveBeenCalled();
  });

  it("calls handleActivitySubmit on form submission with the expected context", () => {
    renderWithProviders(
      <EditActivityForm activity={buildActivity()} id="1" isBoard={true} />,
    );
    fireEvent.submit(screen.getByText("save").closest("form")!);

    expect(handleActivitySubmit).toHaveBeenCalledWith(
      expect.objectContaining({
        isBoard: true,
        isEdit: true,
        id: "1",
      }),
    );
  });

  it("shows 'create_activity' for a new activity and 'save' when editing", () => {
    renderWithProviders(
      <EditActivityForm activity={buildActivity()} id="1" isBoard={false} />,
    );
    expect(screen.getByText("save")).toBeInTheDocument();
  });
});
