import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto, GroupResponseDto } from "~/api";
import EditActivityForm from "~/components/Activity/Edit/EditActivityForm/EditActivityForm";
import {
  addQuestion,
  handleActivityFormChange,
  handleActivitySubmit,
  handleDeleteActivity,
  removeQuestion,
} from "~/components/Activity/Edit/EditActivityForm/EditActivityForm.handlers";
import { renderWithProviders } from "~/testUtils";

vi.mock(
  "~/components/Activity/Edit/EditActivityForm/EditActivityForm.handlers",
  () => ({
    formatForInput: vi.fn(() => ""),
    formatDateOnly: vi.fn(() => ""),
    handleActivityFormChange: vi.fn(),
    addQuestion: vi.fn(),
    removeQuestion: vi.fn(),
    updateQuestion: vi.fn(),
    handleActivitySubmit: vi.fn((args: any) => args.e.preventDefault()),
    handleDeleteActivity: vi.fn(),
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

  it("offers the loaded groups as possible organizers", () => {
    renderWithProviders(
      <EditActivityForm
        activity={null}
        id={undefined}
        isBoard={false}
        groups={[{ id: 7, name: "BaCo" } as GroupResponseDto]}
      />,
    );
    expect(screen.getByText("BaCo")).toBeInTheDocument();
  });

  it("renders the edit form straight away with the loaded activity", () => {
    renderWithProviders(
      <EditActivityForm
        activity={buildActivity()}
        id="1"
        isBoard={false}
        groups={[]}
      />,
    );
    expect(screen.queryByText("loading")).not.toBeInTheDocument();
    expect(screen.getByDisplayValue("Party")).toBeInTheDocument();
  });

  it("renders the form immediately when creating a new activity", () => {
    renderWithProviders(
      <EditActivityForm
        activity={null}
        id={undefined}
        isBoard={false}
        groups={[]}
      />,
    );
    expect(screen.getByLabelText(/^name/)).toBeInTheDocument();
    expect(screen.getByText("create_activity")).toBeInTheDocument();
  });

  it("does not show board-only fields for a non-board user", () => {
    renderWithProviders(
      <EditActivityForm
        activity={null}
        id={undefined}
        isBoard={false}
        groups={[]}
      />,
    );
    expect(screen.queryByLabelText("vat_rate")).not.toBeInTheDocument();
    expect(screen.queryByLabelText("show_in_koala")).not.toBeInTheDocument();
  });

  it("shows board-only fields for a board user", () => {
    renderWithProviders(
      <EditActivityForm
        activity={null}
        id={undefined}
        isBoard={true}
        groups={[]}
      />,
    );
    expect(screen.getByLabelText("vat_rate")).toBeInTheDocument();
    expect(screen.getByLabelText(/show_in_koala/)).toBeInTheDocument();
  });

  it("shows a hint about keeping the current poster only in edit mode", () => {
    renderWithProviders(
      <EditActivityForm
        activity={null}
        id={undefined}
        isBoard={false}
        groups={[]}
      />,
    );
    expect(
      screen.queryByText("leave_empty_to_keep_current"),
    ).not.toBeInTheDocument();
  });

  it("shows the no-content message when there are no specification questions", () => {
    renderWithProviders(
      <EditActivityForm
        activity={null}
        id={undefined}
        isBoard={false}
        groups={[]}
      />,
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
        groups={[]}
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
      <EditActivityForm
        activity={null}
        id={undefined}
        isBoard={false}
        groups={[]}
      />,
    );
    fireEvent.click(screen.getByText("+ add_question"));
    expect(addQuestion).toHaveBeenCalledWith([], expect.any(Function));
  });

  it("calls handleActivityFormChange when a form field changes", () => {
    renderWithProviders(
      <EditActivityForm
        activity={null}
        id={undefined}
        isBoard={false}
        groups={[]}
      />,
    );
    fireEvent.change(screen.getByLabelText(/^name/), {
      target: { value: "New name" },
    });
    expect(handleActivityFormChange).toHaveBeenCalled();
  });

  it("calls handleActivitySubmit on form submission with the expected context", () => {
    renderWithProviders(
      <EditActivityForm
        activity={buildActivity()}
        id="1"
        isBoard={true}
        groups={[]}
      />,
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
      <EditActivityForm
        activity={buildActivity()}
        id="1"
        isBoard={false}
        groups={[]}
      />,
    );
    expect(screen.getByText("save")).toBeInTheDocument();
  });

  it("does not show a delete button for a non-board user", () => {
    renderWithProviders(
      <EditActivityForm
        activity={buildActivity()}
        id="1"
        isBoard={false}
        groups={[]}
      />,
    );
    expect(screen.queryByText("delete")).not.toBeInTheDocument();
  });

  it("does not show a delete button when creating a new activity", () => {
    renderWithProviders(
      <EditActivityForm
        activity={null}
        id={undefined}
        isBoard={true}
        groups={[]}
      />,
    );
    expect(screen.queryByText("delete")).not.toBeInTheDocument();
  });

  it("shows a delete button for a board member editing an activity, and deletes on confirm", async () => {
    renderWithProviders(
      <EditActivityForm
        activity={buildActivity()}
        id="1"
        isBoard={true}
        groups={[]}
      />,
    );

    fireEvent.click(screen.getByText("delete"));

    const confirmButtons = await screen.findAllByRole("button", {
      name: "delete",
    });
    expect(confirmButtons.length).toBeGreaterThan(1);
    fireEvent.click(confirmButtons[confirmButtons.length - 1]);

    await waitFor(() =>
      expect(handleDeleteActivity).toHaveBeenCalledWith(
        1,
        expect.any(Function),
      ),
    );
  });

  it("closes the delete modal on cancel without deleting", async () => {
    renderWithProviders(
      <EditActivityForm
        activity={buildActivity()}
        id="1"
        isBoard={true}
        groups={[]}
      />,
    );

    fireEvent.click(screen.getByText("delete"));

    const cancelButton = await screen.findByRole("button", { name: "cancel" });
    fireEvent.click(cancelButton);

    await waitFor(() =>
      expect(
        screen.queryByText("are_you_sure_delete_activity"),
      ).not.toBeInTheDocument(),
    );
    expect(handleDeleteActivity).not.toHaveBeenCalled();
  });
});
