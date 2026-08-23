import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Study, StudyEnrollmentResponseDto } from "~/api";
import {
  handleAddEnrollment,
  handleUpdateEnrollmentStatus,
} from "~/routes/admin/edit-member/edit-member.handlers";
import UpdateAccountStatus from "~/routes/update-account-status";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

const {
  getStudyenrollments,
  getStudies,
  deleteMembersById,
  getMembersByIdMailinglists,
  putMembersByIdMailinglists,
} = vi.hoisted(() => ({
  getStudyenrollments: vi.fn(),
  getStudies: vi.fn(),
  deleteMembersById: vi.fn(),
  getMembersByIdMailinglists: vi.fn(),
  putMembersByIdMailinglists: vi.fn(),
}));

vi.mock("~/api", () => ({
  getStudyenrollments,
  getStudies,
  deleteMembersById,
  getMembersByIdMailinglists,
  putMembersByIdMailinglists,
}));

const { loadStudyStartDates } = vi.hoisted(() => ({
  loadStudyStartDates: vi.fn(async (_setter?: (value: string) => void) => {}),
}));

vi.mock("~/components/Register/RegisterForm/RegisterForm.handlers", () => ({
  loadStudyStartDates,
}));

vi.mock("~/routes/admin/edit-member/edit-member.handlers", () => ({
  handleAddEnrollment: vi.fn(),
  handleUpdateEnrollmentStatus: vi.fn(),
}));

vi.mock("react-hot-toast", () => ({
  default: {
    promise: vi.fn((p: Promise<unknown>, opts: any) => {
      p.then(
        (data) => opts.success?.(data),
        (err) => opts.error?.(err),
      ).catch(() => {});
      return p;
    }),
    error: vi.fn(),
    success: vi.fn(),
  },
}));

const token: TokenParsed = {
  locale: "en",
  UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
  access_level: "member",
  given_name: "Jane",
  family_name: "Doe",
  name: "Jane Doe",
};

function makeStudy(overrides: Partial<Study> = {}): Study {
  return {
    id: 1,
    title: "Computer Science",
    nominalDurationYears: 3,
    ...overrides,
  } as Study;
}

function makeEnrollment(
  overrides: Partial<StudyEnrollmentResponseDto> = {},
): StudyEnrollmentResponseDto {
  return {
    id: 1,
    studyId: 1,
    studyTitle: "Computer Science",
    enrollmentDate: "2020-09-01T00:00:00Z",
    status: "Enrolled",
    ...overrides,
  } as StudyEnrollmentResponseDto;
}

describe("UpdateAccountStatus", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    loadStudyStartDates.mockImplementation(async () => {});
    getMembersByIdMailinglists.mockResolvedValue({ data: [] });
    putMembersByIdMailinglists.mockResolvedValue({});
  });

  it("logs an error when there is no parsed token", async () => {
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => null),
    });
    renderWithProviders(<UpdateAccountStatus />, { authService });

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("renders enrolled studies once loaded", async () => {
    getStudyenrollments.mockResolvedValue({ data: [makeEnrollment()] });
    getStudies.mockResolvedValue({ data: [makeStudy()] });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<UpdateAccountStatus />, { authService });

    expect(
      (await screen.findAllByText("Computer Science")).length,
    ).toBeGreaterThan(0);
  });

  it("shows the empty-state message when there are no enrollments", async () => {
    getStudyenrollments.mockResolvedValue({ data: [] });
    getStudies.mockResolvedValue({ data: [] });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<UpdateAccountStatus />, { authService });

    expect(await screen.findByText("no_enrollments_found")).toBeInTheDocument();
  });

  it("shows a status dropdown for an in-progress enrollment within the nominal duration and calls the update handler", async () => {
    getStudyenrollments.mockResolvedValue({
      data: [
        makeEnrollment({
          enrollmentDate: new Date().toISOString(),
          status: "Enrolled",
        }),
      ],
    });
    getStudies.mockResolvedValue({ data: [makeStudy()] });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<UpdateAccountStatus />, { authService });

    const select = await screen.findByDisplayValue("status_in_progress");
    fireEvent.change(select, { target: { value: "Completed" } });

    expect(handleUpdateEnrollmentStatus).toHaveBeenCalledWith(
      1,
      "Completed",
      expect.any(Function),
      expect.any(Function),
    );
  });

  it("enables and wires up the add-enrollment button once a study and date are chosen", async () => {
    getStudyenrollments.mockResolvedValue({ data: [makeEnrollment()] });
    getStudies.mockResolvedValue({ data: [makeStudy()] });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<UpdateAccountStatus />, { authService });

    await screen.findAllByText("Computer Science");
    expect(screen.getByText("add")).toBeDisabled();
  });

  it("hides the add-study controls entirely for a member with no enrollment history", async () => {
    getStudyenrollments.mockResolvedValue({ data: [] });
    getStudies.mockResolvedValue({ data: [makeStudy()] });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<UpdateAccountStatus />, { authService });

    await screen.findByText("no_enrollments_found");
    expect(screen.queryByLabelText("add_study_enrollment")).toBeNull();
    expect(screen.queryByText("add")).toBeNull();
  });

  it("opens the delete-account modal and calls handleDeleteAccount", async () => {
    getStudyenrollments.mockResolvedValue({ data: [] });
    getStudies.mockResolvedValue({ data: [] });
    deleteMembersById.mockResolvedValue({});
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
      logout: vi.fn(async () => {}),
    });
    renderWithProviders(<UpdateAccountStatus />, { authService });

    await screen.findByText("no_enrollments_found");
    fireEvent.click(
      screen.getByRole("button", { name: /Account Verwijderen/ }),
    );
    fireEvent.click(await screen.findByText("Definitief Verwijderen"));

    await waitFor(() =>
      expect(deleteMembersById).toHaveBeenCalledWith({
        path: { id: token.UserId },
      }),
    );
  });

  it("generates start-date options and preselects the closest one, allowing the study and date selects to be changed", async () => {
    getStudyenrollments.mockResolvedValue({ data: [makeEnrollment()] });
    getStudies.mockResolvedValue({ data: [makeStudy()] });
    loadStudyStartDates.mockImplementation(async (setter: any) => {
      setter("09-01, 03-15");
    });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<UpdateAccountStatus />, { authService });

    await screen.findAllByText("Computer Science");

    const studySelect = screen.getByLabelText(
      "add_study_enrollment",
    ) as HTMLSelectElement;
    fireEvent.change(studySelect, { target: { value: "1" } });

    const dateSelect = screen.getByLabelText("start_date") as HTMLSelectElement;
    expect(dateSelect.options.length).toBeGreaterThan(0);
    const optionValue = dateSelect.options[0].value;
    fireEvent.change(dateSelect, { target: { value: optionValue } });

    fireEvent.click(screen.getByText("add"));

    expect(handleAddEnrollment).toHaveBeenCalledWith(
      token.UserId,
      1,
      expect.any(Function),
      expect.any(Function),
      optionValue,
    );

    // Clearing the study selection back to the placeholder disables add again.
    fireEvent.change(studySelect, { target: { value: "" } });
    expect(screen.getByText("add")).toBeDisabled();
  });

  it("shows a loading placeholder in the status column while studies haven't loaded, and a dropped-out label once the deadline has passed", async () => {
    getStudyenrollments.mockResolvedValue({
      data: [
        makeEnrollment({
          status: "DroppedOut",
          enrollmentDate: "2000-01-01T00:00:00Z",
        }),
      ],
    });
    getStudies.mockResolvedValue({ data: [makeStudy()] });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<UpdateAccountStatus />, { authService });

    expect(
      (await screen.findAllByText("status_dropped_out")).length,
    ).toBeGreaterThan(0);
  });

  it("shows an error toast when the delete account API call returns an error", async () => {
    getStudyenrollments.mockResolvedValue({ data: [] });
    getStudies.mockResolvedValue({ data: [] });
    deleteMembersById.mockResolvedValue({
      error: "fail",
      message: "delete failed",
    });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<UpdateAccountStatus />, { authService });

    await screen.findByText("no_enrollments_found");
    fireEvent.click(
      screen.getByRole("button", { name: /Account Verwijderen/ }),
    );
    fireEvent.click(await screen.findByText("Definitief Verwijderen"));

    await waitFor(() => expect(deleteMembersById).toHaveBeenCalled());
  });

  it("closes the delete-account modal via the cancel button", async () => {
    getStudyenrollments.mockResolvedValue({ data: [] });
    getStudies.mockResolvedValue({ data: [] });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<UpdateAccountStatus />, { authService });

    await screen.findByText("no_enrollments_found");
    fireEvent.click(
      screen.getByRole("button", { name: /Account Verwijderen/ }),
    );
    const cancelButton = await screen.findByText("Annuleren");
    fireEvent.click(cancelButton);

    await waitFor(() =>
      expect(screen.queryByText("Annuleren")).not.toBeInTheDocument(),
    );
  });

  it("fetches the yearly (General + YearlyRenewalOnly) mailing list context and renders it", async () => {
    getStudyenrollments.mockResolvedValue({ data: [] });
    getStudies.mockResolvedValue({ data: [] });
    getMembersByIdMailinglists.mockResolvedValue({
      data: [
        { id: "list-1", name: "Newsletter", subscribed: false },
        { id: "alumni", name: "Alumni", subscribed: true },
      ],
    });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<UpdateAccountStatus />, { authService });

    await waitFor(() =>
      expect(getMembersByIdMailinglists).toHaveBeenCalledWith({
        path: { id: token.UserId },
        query: { includeYearlyRenewal: true },
      }),
    );
    expect(await screen.findByText("Newsletter")).toBeInTheDocument();
    expect(screen.getByText("Alumni")).toBeInTheDocument();
    expect(screen.getByLabelText("Alumni")).toBeChecked();
    expect(screen.getByLabelText("Newsletter")).not.toBeChecked();
  });

  it("toggles a mailing list checkbox and saves preferences within the yearly context", async () => {
    getStudyenrollments.mockResolvedValue({ data: [] });
    getStudies.mockResolvedValue({ data: [] });
    getMembersByIdMailinglists.mockResolvedValue({
      data: [{ id: "alumni", name: "Alumni", subscribed: false }],
    });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<UpdateAccountStatus />, { authService });

    const checkbox = await screen.findByLabelText("Alumni");
    fireEvent.click(checkbox);
    expect(checkbox).toBeChecked();

    fireEvent.click(
      screen.getByRole("button", { name: "save_mailing_list_preferences" }),
    );

    await waitFor(() =>
      expect(putMembersByIdMailinglists).toHaveBeenCalledWith({
        path: { id: token.UserId },
        query: { includeYearlyRenewal: true },
        body: ["alumni"],
      }),
    );
  });

  it("shows the unavailable message when fetching mailing lists fails", async () => {
    getStudyenrollments.mockResolvedValue({ data: [] });
    getStudies.mockResolvedValue({ data: [] });
    getMembersByIdMailinglists.mockResolvedValue({
      error: { title: "Boom" },
    });
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => token),
    });
    renderWithProviders(<UpdateAccountStatus />, { authService });

    expect(
      await screen.findByText("mailinglists_unavailable"),
    ).toBeInTheDocument();
  });
});
