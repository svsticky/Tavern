import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Study, StudyEnrollmentResponseDto } from "~/api";
import {
  handleAddEnrollment,
  handleUpdateEnrollmentStatus,
} from "~/routes/admin/edit-member/edit-member.handlers";
import * as route from "~/routes/update-account-status/update-account-status";
import {
  createMockAuthService,
  makeTokenParsed,
  renderRoute,
} from "~/testUtils";

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

const { loadStudyStartDates, requireTokenParsed } = vi.hoisted(() => ({
  loadStudyStartDates: vi.fn(async (): Promise<string | null> => null),
  requireTokenParsed: vi.fn(),
}));

vi.mock("~/util/loaderAuth.util", () => ({ requireTokenParsed }));

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

const token = makeTokenParsed({ given_name: "Jane", family_name: "Doe" });

function makeStudy(overrides: Partial<Study> = {}): Study {
  return {
    id: 1,
    title: "Computer Science",
    nominalDurationYears: 3,
    active: true,
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
    loadStudyStartDates.mockResolvedValue(null);
    requireTokenParsed.mockResolvedValue(token);
    getMembersByIdMailinglists.mockResolvedValue({ data: [] });
    putMembersByIdMailinglists.mockResolvedValue({});
  });

  it("renders enrolled studies once loaded", async () => {
    getStudyenrollments.mockResolvedValue({ data: [makeEnrollment()] });
    getStudies.mockResolvedValue({ data: [makeStudy()] });
    renderRoute(route);

    expect(
      (await screen.findAllByText("Computer Science")).length,
    ).toBeGreaterThan(0);
  });

  it("shows the empty-state message when there are no enrollments", async () => {
    getStudyenrollments.mockResolvedValue({ data: [] });
    getStudies.mockResolvedValue({ data: [] });
    renderRoute(route);

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
    renderRoute(route);

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
    renderRoute(route);

    await screen.findAllByText("Computer Science");
    expect(screen.getByText("add")).toBeDisabled();
  });

  it("hides the add-study controls entirely for a member with no enrollment history", async () => {
    getStudyenrollments.mockResolvedValue({ data: [] });
    getStudies.mockResolvedValue({ data: [makeStudy()] });
    renderRoute(route);

    await screen.findByText("no_enrollments_found");
    expect(screen.queryByLabelText("add_study_enrollment")).toBeNull();
    expect(screen.queryByText("add")).toBeNull();
  });

  it("opens the delete-account modal and calls handleDeleteAccount", async () => {
    getStudyenrollments.mockResolvedValue({ data: [] });
    getStudies.mockResolvedValue({ data: [] });
    deleteMembersById.mockResolvedValue({});
    const authService = createMockAuthService({
      logout: vi.fn(async () => {}),
    });
    renderRoute(route, { authService });

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
    loadStudyStartDates.mockResolvedValue("09-01, 03-15");
    renderRoute(route);

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

  it("doesn't crash and shows a dropped-out label when enrolled in a study that is no longer returned (e.g. deactivated)", async () => {
    getStudyenrollments.mockResolvedValue({
      data: [makeEnrollment({ studyId: 999 })],
    });
    getStudies.mockResolvedValue({ data: [makeStudy({ id: 1 })] });
    renderRoute(route);

    expect(
      (await screen.findAllByText("status_dropped_out")).length,
    ).toBeGreaterThan(0);
  });

  it("shows the error page when the study enrollments fail to load", async () => {
    getStudyenrollments.mockResolvedValue({
      error: new Error("Failed to load study enrollments"),
    });
    getStudies.mockResolvedValue({ data: [] });
    renderRoute(route);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Failed to load study enrollments",
    );
  });

  it("shows the error page when the studies fail to load", async () => {
    getStudyenrollments.mockResolvedValue({ data: [] });
    getStudies.mockResolvedValue({
      error: new Error("Failed to load studies"),
    });
    renderRoute(route);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Failed to load studies",
    );
  });

  it("fetches studies including inactive ones so deadline calculations still work for deactivated studies", async () => {
    getStudyenrollments.mockResolvedValue({ data: [] });
    getStudies.mockResolvedValue({ data: [] });
    renderRoute(route);

    await waitFor(() =>
      expect(getStudies).toHaveBeenCalledWith({
        query: { IncludeInactive: true },
      }),
    );
  });

  it("shows a dropped-out label once the deadline has passed", async () => {
    getStudyenrollments.mockResolvedValue({
      data: [
        makeEnrollment({
          status: "DroppedOut",
          enrollmentDate: "2000-01-01T00:00:00Z",
        }),
      ],
    });
    getStudies.mockResolvedValue({ data: [makeStudy()] });
    renderRoute(route);

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
    renderRoute(route);

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
    renderRoute(route);

    await screen.findByText("no_enrollments_found");
    fireEvent.click(
      screen.getByRole("button", { name: /Account Verwijderen/ }),
    );
    const cancelButton = await screen.findByRole("button", { name: "cancel" });
    fireEvent.click(cancelButton);

    await waitFor(() =>
      expect(
        screen.queryByRole("button", { name: "cancel" }),
      ).not.toBeInTheDocument(),
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
    renderRoute(route);

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
    renderRoute(route);

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
    renderRoute(route);

    expect(
      await screen.findByText("mailinglists_unavailable"),
    ).toBeInTheDocument();
  });
});
