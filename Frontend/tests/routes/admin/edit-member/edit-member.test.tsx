import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { Route, Routes } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { StudyEnrollmentResponseDto } from "~/api";
import { renderWithProviders } from "~/testUtils";

const {
  loadMemberData,
  handleSaveMember,
  handleDeleteMember,
  handleDeleteEnrollment,
  handleAddEnrollment,
  handleUpdateEnrollmentStatus,
} = vi.hoisted(() => ({
  loadMemberData: vi.fn(),
  handleSaveMember: vi.fn(),
  handleDeleteMember: vi.fn(),
  handleDeleteEnrollment: vi.fn(),
  handleAddEnrollment: vi.fn(),
  handleUpdateEnrollmentStatus: vi.fn(),
}));

vi.mock("~/routes/admin/edit-member/edit-member.handlers", () => ({
  loadMemberData,
  handleSaveMember,
  handleDeleteMember,
  handleDeleteEnrollment,
  handleAddEnrollment,
  handleUpdateEnrollmentStatus,
}));

// ChangeProfilePicture makes its own ~/api call (getMembersByIdProfilePicture) - it's not part
// of this batch, so stub it out to keep this route test focused on edit-member's own logic.
vi.mock(
  "~/components/Account/ChangeProfilePicture/ChangeProfilePicture",
  () => ({
    default: () => <div>change-profile-picture</div>,
  }),
);

import EditMemberPage from "~/routes/admin/edit-member/edit-member";

function renderPage(id = "m1") {
  return renderWithProviders(
    <Routes>
      <Route path="/admin/members/:id" element={<EditMemberPage />} />
    </Routes>,
    { route: `/admin/members/${id}` },
  );
}

function enrollment(
  overrides: Partial<StudyEnrollmentResponseDto> = {},
): StudyEnrollmentResponseDto {
  return {
    id: 1,
    studyTitle: "Computer Science",
    enrollmentDate: "2023-09-01T00:00:00Z",
    status: "Enrolled",
    ...overrides,
  } as StudyEnrollmentResponseDto;
}

describe("EditMemberPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    loadMemberData.mockImplementation(
      async ({ setFormData, setEnrollments, setLoading }: any) => {
        setFormData((prev: any) => ({ ...prev, firstName: "Jane" }));
        setEnrollments([enrollment()]);
        setLoading(false);
      },
    );
  });

  it("shows a loading indicator while loading, then renders the form", async () => {
    let resolveLoad: (() => void) | undefined;
    loadMemberData.mockImplementation(
      ({ setLoading }: any) =>
        new Promise<void>((resolve) => {
          resolveLoad = () => {
            setLoading(false);
            resolve();
          };
        }),
    );

    renderPage();

    expect(screen.getByText("loading")).toBeInTheDocument();
    resolveLoad?.();

    await waitFor(() =>
      expect(screen.queryByText("loading")).not.toBeInTheDocument(),
    );
  });

  it("loads member data for the given member id", async () => {
    renderPage("m42");

    await waitFor(() => expect(loadMemberData).toHaveBeenCalled());
    expect(loadMemberData.mock.calls[0][0]).toMatchObject({
      memberId: "m42",
    });
  });

  it("renders loaded form data and study enrollments", async () => {
    renderPage();

    expect(await screen.findByDisplayValue("Jane")).toBeInTheDocument();
    expect(screen.getByText("Computer Science")).toBeInTheDocument();
  });

  it("saves the member when the save button is clicked", async () => {
    renderPage();

    const saveButton = await screen.findByRole("button", { name: "save" });
    fireEvent.click(saveButton);

    expect(handleSaveMember).toHaveBeenCalledWith(
      "m1",
      expect.objectContaining({ firstName: "Jane" }),
      expect.any(Function),
    );
  });

  it("opens the delete confirmation modal and deletes the member", async () => {
    renderPage();

    const deleteButton = await screen.findByRole("button", { name: "delete" });
    fireEvent.click(deleteButton);

    // Both the trigger button and the modal's confirm button are labeled "delete" - the modal's
    // is the one rendered later in the DOM once it opens.
    const deleteButtons = await screen.findAllByRole("button", {
      name: "delete",
    });
    expect(deleteButtons.length).toBeGreaterThan(1);
    fireEvent.click(deleteButtons[deleteButtons.length - 1]);

    expect(handleDeleteMember).toHaveBeenCalledWith(
      "m1",
      expect.any(Function),
      expect.any(Function),
    );
  });

  it("closes the delete modal on cancel", async () => {
    renderPage();

    const deleteButton = await screen.findByRole("button", { name: "delete" });
    fireEvent.click(deleteButton);

    const cancelButton = await screen.findByRole("button", { name: "cancel" });
    fireEvent.click(cancelButton);

    await waitFor(() =>
      expect(
        screen.queryByText("are_you_sure_delete_member"),
      ).not.toBeInTheDocument(),
    );
  });

  it("deletes a study enrollment when remove is clicked", async () => {
    renderPage();

    const removeButton = await screen.findByRole("button", { name: "remove" });
    fireEvent.click(removeButton);

    expect(handleDeleteEnrollment).toHaveBeenCalledWith(
      1,
      expect.any(Function),
      expect.any(Function),
    );
  });

  it("updates enrollment status when the status select changes", async () => {
    renderPage();

    await screen.findByText("Computer Science");
    // The "add study enrollment" select is also a combobox on this page - find the status one
    // by its distinctive option.
    const statusSelect = screen
      .getAllByRole("combobox")
      .find((el) => within(el).queryByText("status_completed"));
    expect(statusSelect).toBeTruthy();
    fireEvent.change(statusSelect as HTMLSelectElement, {
      target: { value: "Completed" },
    });

    expect(handleUpdateEnrollmentStatus).toHaveBeenCalledWith(
      1,
      "Completed",
      expect.any(Function),
      expect.any(Function),
    );
  });

  it("adds a new study enrollment once a study is selected", async () => {
    loadMemberData.mockImplementation(
      async ({ setEnrollments, setAvailableStudies, setLoading }: any) => {
        setEnrollments([]);
        setAvailableStudies([{ id: 3, title: "Physics" }]);
        setLoading(false);
      },
    );

    renderPage();

    const addButton = await screen.findByRole("button", { name: "add" });
    expect(addButton).toBeDisabled();

    const studySelect = screen.getByLabelText("add_study_enrollment");
    fireEvent.change(studySelect, { target: { value: "3" } });

    expect(addButton).not.toBeDisabled();
    fireEvent.click(addButton);

    expect(handleAddEnrollment).toHaveBeenCalledWith(
      "m1",
      3,
      expect.any(Function),
      expect.any(Function),
    );
  });

  it("renders the ChangeProfilePicture child component", async () => {
    renderPage();
    expect(
      await screen.findByText("change-profile-picture"),
    ).toBeInTheDocument();
  });

  it("updates local form state as every personal-info field is edited", async () => {
    renderPage();
    await screen.findByDisplayValue("Jane");

    const textFields: [RegExp, string][] = [
      [/^first_name/, "Janet"],
      [/^last_name/, "Doe"],
      [/^student_number/, "12345"],
      [/^date_of_birth/, "2000-01-01"],
      [/^phone_number/, "0612345678"],
      [/^parent_phone_number/, "0687654321"],
      [/^street/, "Main street"],
      [/^house_number/, "10"],
      [/^postal_code/, "1234AB"],
      [/^city/, "Utrecht"],
    ];
    textFields.forEach(([label, value]) => {
      const input = screen.getByLabelText(label);
      fireEvent.change(input, { target: { value } });
      expect(input).toHaveValue(value);
    });

    const checkboxFields = [
      "gratie",
      "lid_van_verdienste",
      "ere_lid",
      "begunstiger",
      "suspended",
    ];
    checkboxFields.forEach((label) => {
      const checkbox = screen.getByLabelText(label) as HTMLInputElement;
      const before = checkbox.checked;
      fireEvent.click(checkbox);
      expect(checkbox.checked).toBe(!before);
    });

    const notes = screen.getByPlaceholderText("internal_notes_placeholder");
    fireEvent.change(notes, { target: { value: "Some internal note" } });
    expect(notes).toHaveValue("Some internal note");
  });

  it("styles completed and dropped-out enrollments differently", async () => {
    loadMemberData.mockImplementation(
      async ({ setFormData, setEnrollments, setLoading }: any) => {
        setFormData((prev: any) => ({ ...prev, firstName: "Jane" }));
        setEnrollments([
          enrollment({
            id: 1,
            studyTitle: "Completed Study",
            status: "Completed",
          }),
          enrollment({
            id: 2,
            studyTitle: "Dropped Study",
            status: "DroppedOut",
          }),
        ]);
        setLoading(false);
      },
    );

    renderPage();

    await screen.findByText("Completed Study");
    const selects = screen
      .getAllByRole("combobox")
      .filter((el) => within(el).queryByText("status_completed"));

    expect(selects[0]).toHaveClass("bg-green-100");
    expect(selects[1]).toHaveClass("bg-red-100");
  });

  it("shows a saving label on the save button while saving", async () => {
    handleSaveMember.mockImplementation(
      (_id: string, _data: any, setSaving: (v: boolean) => void) => {
        setSaving(true);
      },
    );
    renderPage();

    const saveButton = await screen.findByRole("button", { name: "save" });
    fireEvent.click(saveButton);

    expect(await screen.findByText("saving")).toBeInTheDocument();
  });

  it("clears the selected study when the placeholder option is chosen again", async () => {
    loadMemberData.mockImplementation(
      async ({ setEnrollments, setAvailableStudies, setLoading }: any) => {
        setEnrollments([]);
        setAvailableStudies([{ id: 3, title: "Physics" }]);
        setLoading(false);
      },
    );

    renderPage();

    const studySelect = await screen.findByLabelText("add_study_enrollment");
    fireEvent.change(studySelect, { target: { value: "3" } });
    fireEvent.change(studySelect, { target: { value: "" } });

    expect(screen.getByRole("button", { name: "add" })).toBeDisabled();
  });
});
