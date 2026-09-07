import { act, fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import EditActivityForm from "~/components/Activity/Edit/EditActivityForm/EditActivityForm";
import {
  addQuestion,
  handleActivityFormChange,
  handleActivitySubmit,
  handleDeleteActivity,
  loadGroups,
  removeQuestion,
} from "~/components/Activity/Edit/EditActivityForm/EditActivityForm.handlers";
import { renderWithProviders } from "~/testUtils";
import {
  loadActivityDraft,
  saveActivityDraft,
} from "~/util/activityDraft.util";

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
    localStorage.clear();
  });

  it("shows a loading state while editing until groups have loaded", () => {
    vi.mocked(loadGroups).mockImplementationOnce(async () => {});
    renderWithProviders(
      <EditActivityForm
        activity={buildActivity()}
        id="1"
        canEditStructural={false}
        canManageFinances={false}
      />,
    );
    expect(screen.getByText("loading")).toBeInTheDocument();
    expect(loadGroups).toHaveBeenCalled();
  });

  it("renders the form immediately when creating a new activity", () => {
    renderWithProviders(
      <EditActivityForm
        activity={null}
        id={undefined}
        canEditStructural={false}
        canManageFinances={false}
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
        canEditStructural={false}
        canManageFinances={false}
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
        canEditStructural={true}
        canManageFinances={true}
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
        canEditStructural={false}
        canManageFinances={false}
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
        canEditStructural={false}
        canManageFinances={false}
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
        canEditStructural={false}
        canManageFinances={false}
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
        canEditStructural={false}
        canManageFinances={false}
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
        canEditStructural={false}
        canManageFinances={false}
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
        canEditStructural={true}
        canManageFinances={true}
      />,
    );
    fireEvent.submit(screen.getByText("save").closest("form")!);

    expect(handleActivitySubmit).toHaveBeenCalledWith(
      expect.objectContaining({
        canEditStructural: true,
        canManageFinances: true,
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
        canEditStructural={false}
        canManageFinances={false}
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
        canEditStructural={false}
        canManageFinances={false}
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
        canEditStructural={false}
        canManageFinances={false}
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
        canEditStructural={false}
        canManageFinances={false}
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
        canEditStructural={false}
        canManageFinances={false}
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

  it("restores a saved draft when creating a new activity and displays restored banner", () => {
    saveActivityDraft({
      name: "Drafted Gala",
      location: "Castle",
      dutchDescription: "Mooi gala",
      englishDescription: "Nice gala",
      price: "25.00",
      savedAt: "2026-10-01T14:30:00Z",
    });

    renderWithProviders(
      <EditActivityForm
        activity={null}
        id={undefined}
        canEditStructural={false}
        canManageFinances={false}
      />,
    );

    expect(screen.getByRole("alert")).toBeInTheDocument();
    expect(screen.getByText("draft_restored")).toBeInTheDocument();
    expect(screen.getByText("draft_restored_description")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Drafted Gala")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Castle")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Mooi gala")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Nice gala")).toBeInTheDocument();
    expect(screen.getByDisplayValue("25.00")).toBeInTheDocument();
  });

  it("does not restore draft if editing an existing activity", () => {
    saveActivityDraft({
      name: "Drafted Gala",
      location: "Castle",
    });

    renderWithProviders(
      <EditActivityForm
        activity={buildActivity({ name: "Actual Activity" })}
        id="42"
        canEditStructural={false}
        canManageFinances={false}
      />,
    );

    expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    expect(screen.queryByDisplayValue("Drafted Gala")).not.toBeInTheDocument();
    expect(screen.getByDisplayValue("Actual Activity")).toBeInTheDocument();
  });

  it("discards draft when clicking discard and confirming", async () => {
    saveActivityDraft({
      name: "Draft to Discard",
      location: "Somewhere",
    });

    renderWithProviders(
      <EditActivityForm
        activity={null}
        id={undefined}
        canEditStructural={false}
        canManageFinances={false}
      />,
    );

    expect(screen.getByRole("alert")).toBeInTheDocument();
    expect(screen.getByDisplayValue("Draft to Discard")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button", { name: "discard_draft" }));

    const confirmButtons = await screen.findAllByRole("button", {
      name: "discard_draft",
    });
    fireEvent.click(confirmButtons[confirmButtons.length - 1]);

    await waitFor(() => {
      expect(screen.queryByRole("alert")).not.toBeInTheDocument();
    });

    expect(loadActivityDraft()).toBeNull();
    expect(
      screen.queryByDisplayValue("Draft to Discard"),
    ).not.toBeInTheDocument();
  });

  it("keeps draft when cancelling discard confirmation", async () => {
    saveActivityDraft({
      name: "Preserved Draft",
    });

    renderWithProviders(
      <EditActivityForm
        activity={null}
        id={undefined}
        canEditStructural={false}
        canManageFinances={false}
      />,
    );

    expect(screen.getByRole("alert")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button", { name: "discard_draft" }));

    const cancelButton = await screen.findByRole("button", { name: "cancel" });
    fireEvent.click(cancelButton);

    await waitFor(() => {
      expect(screen.getByRole("alert")).toBeInTheDocument();
    });
    expect(loadActivityDraft()).not.toBeNull();
  });

  it("auto-saves changes to localStorage when typing", async () => {
    vi.useFakeTimers();

    renderWithProviders(
      <EditActivityForm
        activity={null}
        id={undefined}
        canEditStructural={false}
        canManageFinances={false}
      />,
    );

    expect(loadActivityDraft()).toBeNull();

    const nameInput = screen.getByLabelText(/^name/);
    fireEvent.change(nameInput, { target: { value: "Autosaved Event" } });

    act(() => {
      vi.advanceTimersByTime(600);
    });

    const saved = loadActivityDraft();
    expect(saved).not.toBeNull();
    expect(saved?.name).toBe("Autosaved Event");

    vi.useRealTimers();
  });

  it("flushes draft save on beforeunload", () => {
    renderWithProviders(
      <EditActivityForm
        activity={null}
        id={undefined}
        canEditStructural={false}
        canManageFinances={false}
      />,
    );

    const nameInput = screen.getByLabelText(/^name/);
    fireEvent.change(nameInput, { target: { value: "Window Close Event" } });

    window.dispatchEvent(new Event("beforeunload"));

    const saved = loadActivityDraft();
    expect(saved).not.toBeNull();
    expect(saved?.name).toBe("Window Close Event");
  });
});
