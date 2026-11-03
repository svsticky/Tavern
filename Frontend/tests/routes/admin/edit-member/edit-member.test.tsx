import { fireEvent, screen, waitFor, within } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { StudyEnrollmentResponseDto } from "~/api";
import type { EditMemberLoaderData } from "~/routes/admin/edit-member/edit-member.handlers";
import { renderRoute } from "~/testUtils";

const {
  loadEditMemberLoaderData,
  requireBoardOrCandidateBoard,
  handleSaveMember,
  handleDeleteMember,
  handleDeleteEnrollment,
  handleAddEnrollment,
  handleUpdateEnrollmentStatus,
  handleMarkMembershipAsPaid,
  handleMarkBegunstigerFeeAsPaid,
} = vi.hoisted(() => ({
  loadEditMemberLoaderData: vi.fn(),
  requireBoardOrCandidateBoard: vi.fn(),
  handleSaveMember: vi.fn(),
  handleDeleteMember: vi.fn(),
  handleDeleteEnrollment: vi.fn(),
  handleAddEnrollment: vi.fn(),
  handleUpdateEnrollmentStatus: vi.fn(),
  handleMarkMembershipAsPaid: vi.fn(),
  handleMarkBegunstigerFeeAsPaid: vi.fn(),
}));

vi.mock("~/util/loaderAuth.util", () => ({ requireBoardOrCandidateBoard }));

vi.mock("~/routes/admin/edit-member/edit-member.handlers", () => ({
  loadEditMemberLoaderData,
  handleSaveMember,
  handleDeleteMember,
  handleDeleteEnrollment,
  handleAddEnrollment,
  handleUpdateEnrollmentStatus,
  handleMarkMembershipAsPaid,
  handleMarkBegunstigerFeeAsPaid,
}));

// Stub ChangeProfilePicture to keep this route test focused on edit-member's own logic.
vi.mock(
  "~/components/Account/ChangeProfilePicture/ChangeProfilePicture",
  () => ({
    default: ({ profilePictureSrc }: { profilePictureSrc: string }) => (
      <div>change-profile-picture-{profilePictureSrc}</div>
    ),
  }),
);

import * as route from "~/routes/admin/edit-member/edit-member";

function renderPage(id = "m1") {
  return renderRoute(route, {
    path: "/admin/members/:id",
    route: `/admin/members/${id}`,
  });
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

/** Makes the loader return member m1 (Jane) with the given changes. */
function mockLoaderData(overrides: Partial<EditMemberLoaderData> = {}) {
  loadEditMemberLoaderData.mockResolvedValue({
    memberId: "m1",
    formData: {
      firstName: "Jane",
      lastName: "",
      studentNumber: "",
      phoneNumber: "",
      street: "",
      houseNumber: "",
      postalCode: "",
      city: "",
      parentPhoneNumber: "",
      preferredLanguage: "NL",
      notes: "",
      gratie: false,
      lidVanVerdienste: false,
      ereLid: false,
      begunstiger: false,
      suspended: false,
      dateOfBirth: "",
    },
    email: "jane@example.com",
    enrollments: [enrollment()],
    groupMemberships: [],
    availableStudies: [],
    profilePictureSrc: "blob:picture",
    hasPaidMembership: false,
    isBegunstiger: false,
    ...overrides,
  });
}

describe("EditMemberPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    requireBoardOrCandidateBoard.mockResolvedValue({});
    mockLoaderData();
  });

  it("loads member data for the given member id", async () => {
    renderPage("m42");

    await screen.findByDisplayValue("Jane");
    expect(loadEditMemberLoaderData).toHaveBeenCalledWith(
      expect.objectContaining({ id: "m42" }),
    );
  });

  it("shows the error page when the member fails to load", async () => {
    loadEditMemberLoaderData.mockRejectedValue(
      new Error("Failed to load member data"),
    );
    renderPage();

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Failed to load member data",
    );
  });

  it("renders loaded form data and study enrollments", async () => {
    renderPage();

    expect(await screen.findByDisplayValue("Jane")).toBeInTheDocument();
    expect(screen.getByText("Computer Science")).toBeInTheDocument();
  });

  it("links a group membership to the group's admin page", async () => {
    mockLoaderData({
      groupMemberships: [
        {
          id: 1,
          groupId: 5,
          groupName: "Board",
          membershipYear: 2026,
          roleAliasName: "Chair",
          memberName: "Jane",
        },
      ] as EditMemberLoaderData["groupMemberships"],
    });

    renderPage();

    const groupLink = await screen.findByRole("link", { name: /Board/ });
    expect(groupLink).toHaveAttribute("href", "/admin/groups/5");
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

    await waitFor(() =>
      expect(handleDeleteMember).toHaveBeenCalledWith(
        "m1",
        expect.any(Function),
        expect.any(Function),
      ),
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

  it("opens the mark-membership-as-paid modal and confirms it", async () => {
    renderPage();

    const markPaidButton = await screen.findByRole("button", {
      name: "mark_membership_as_paid",
    });
    fireEvent.click(markPaidButton);

    expect(
      await screen.findByText("are_you_sure_mark_membership_as_paid"),
    ).toBeInTheDocument();

    // Both the trigger button and the modal's confirm button share the label - the modal's is
    // the one rendered later in the DOM once it opens.
    const confirmButtons = await screen.findAllByRole("button", {
      name: "mark_membership_as_paid",
    });
    expect(confirmButtons.length).toBeGreaterThan(1);
    fireEvent.click(confirmButtons[confirmButtons.length - 1]);

    await waitFor(() =>
      expect(handleMarkMembershipAsPaid).toHaveBeenCalledWith(
        "m1",
        expect.any(Function),
        expect.any(Function),
      ),
    );
    await waitFor(() =>
      expect(
        screen.queryByText("are_you_sure_mark_membership_as_paid"),
      ).not.toBeInTheDocument(),
    );
  });

  it("shows the begunstiger-fee action instead of the membership one for a begunstiger", async () => {
    mockLoaderData({ enrollments: [], isBegunstiger: true });

    renderPage();

    const markPaidButton = await screen.findByRole("button", {
      name: "mark_begunstiger_fee_as_paid",
    });
    expect(
      screen.queryByRole("button", { name: "mark_membership_as_paid" }),
    ).not.toBeInTheDocument();

    fireEvent.click(markPaidButton);

    expect(
      await screen.findByText("are_you_sure_mark_begunstiger_fee_as_paid"),
    ).toBeInTheDocument();

    const confirmButtons = await screen.findAllByRole("button", {
      name: "mark_begunstiger_fee_as_paid",
    });
    fireEvent.click(confirmButtons[confirmButtons.length - 1]);

    await waitFor(() =>
      expect(handleMarkBegunstigerFeeAsPaid).toHaveBeenCalledWith(
        "m1",
        expect.any(Function),
        expect.any(Function),
      ),
    );
    expect(handleMarkMembershipAsPaid).not.toHaveBeenCalled();
  });

  it("hides the mark-membership-as-paid action once the membership is paid", async () => {
    mockLoaderData({ enrollments: [], hasPaidMembership: true });

    renderPage();

    await screen.findByRole("button", { name: "save" });
    expect(
      screen.queryByRole("button", { name: "mark_membership_as_paid" }),
    ).not.toBeInTheDocument();
  });

  it("closes the mark-membership-as-paid modal on cancel", async () => {
    renderPage();

    const markPaidButton = await screen.findByRole("button", {
      name: "mark_membership_as_paid",
    });
    fireEvent.click(markPaidButton);

    const cancelButton = await screen.findByRole("button", { name: "cancel" });
    fireEvent.click(cancelButton);

    await waitFor(() =>
      expect(
        screen.queryByText("are_you_sure_mark_membership_as_paid"),
      ).not.toBeInTheDocument(),
    );
    expect(handleMarkMembershipAsPaid).not.toHaveBeenCalled();
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
    mockLoaderData({
      enrollments: [],
      availableStudies: [
        { id: 3, title: "Physics" },
      ] as EditMemberLoaderData["availableStudies"],
    });

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

  it("renders the ChangeProfilePicture child component with the loaded picture", async () => {
    renderPage();
    expect(
      await screen.findByText("change-profile-picture-blob:picture"),
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
    mockLoaderData({
      enrollments: [
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
      ],
    });

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
    mockLoaderData({
      enrollments: [],
      availableStudies: [
        { id: 3, title: "Physics" },
      ] as EditMemberLoaderData["availableStudies"],
    });

    renderPage();

    const studySelect = await screen.findByLabelText("add_study_enrollment");
    fireEvent.change(studySelect, { target: { value: "3" } });
    fireEvent.change(studySelect, { target: { value: "" } });

    expect(screen.getByRole("button", { name: "add" })).toBeDisabled();
  });
});
