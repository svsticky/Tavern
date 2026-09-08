import { fireEvent, render, screen } from "@testing-library/react";
import type { ReactElement } from "react";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { EnrollmentResponseDto } from "~/api";
import EditWaitinglistParticipantTile from "~/components/Activity/Edit/EditParticipantsTile/EditWaitinglistParticipantTile/EditWaitinglistParticipantTile";

function renderTile(ui: ReactElement) {
  return render(<MemoryRouter>{ui}</MemoryRouter>);
}

const {
  deleteEnrollmentsByActivityIdByMemberId,
  patchEnrollmentsByActivityIdByMemberId,
} = vi.hoisted(() => ({
  deleteEnrollmentsByActivityIdByMemberId: vi.fn(),
  patchEnrollmentsByActivityIdByMemberId: vi.fn(),
}));

vi.mock("~/api", () => ({
  deleteEnrollmentsByActivityIdByMemberId,
  patchEnrollmentsByActivityIdByMemberId,
}));

vi.mock("react-hot-toast", () => ({
  default: Object.assign((...args: unknown[]) => args, {
    success: vi.fn(),
    error: vi.fn(),
    promise: vi.fn((p: Promise<unknown>, opts: any) => {
      p.then(
        (data) => opts.success?.(data),
        (err) => opts.error?.(err),
      ).catch(() => {});
      return p;
    }),
  }),
}));

function buildEnrollment(
  overrides: Partial<EnrollmentResponseDto> = {},
): EnrollmentResponseDto {
  return {
    member: { id: "member-1", firstName: "Alice", lastName: "Smith" },
    ...overrides,
  } as EnrollmentResponseDto;
}

describe("EditWaitinglistParticipantTile", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders the member's name", () => {
    renderTile(
      <EditWaitinglistParticipantTile
        activityId={1}
        enrollment={buildEnrollment()}
        onUnenroll={vi.fn()}
        onMoveToParticipants={vi.fn()}
      />,
    );
    expect(screen.getByText("Alice Smith")).toBeInTheDocument();
  });

  it("links the member's name to their admin profile", () => {
    renderTile(
      <EditWaitinglistParticipantTile
        activityId={1}
        enrollment={buildEnrollment()}
        onUnenroll={vi.fn()}
        onMoveToParticipants={vi.fn()}
      />,
    );
    expect(screen.getByText("Alice Smith")).toHaveAttribute(
      "href",
      "/admin/members/member-1",
    );
  });

  it("renders the member's name as plain text when the member has no id", () => {
    renderTile(
      <EditWaitinglistParticipantTile
        activityId={1}
        enrollment={buildEnrollment({
          member: { firstName: "Alice", lastName: "Smith" },
        })}
        onUnenroll={vi.fn()}
        onMoveToParticipants={vi.fn()}
      />,
    );
    expect(screen.getByText("Alice Smith")).not.toHaveAttribute("href");
  });

  it("calls the move-to-participants API and onMoveToParticipants when clicked", async () => {
    patchEnrollmentsByActivityIdByMemberId.mockResolvedValue({});
    const onMoveToParticipants = vi.fn();
    renderTile(
      <EditWaitinglistParticipantTile
        activityId={1}
        enrollment={buildEnrollment()}
        onUnenroll={vi.fn()}
        onMoveToParticipants={onMoveToParticipants}
      />,
    );
    fireEvent.click(screen.getByText("move_to_participants"));

    await vi.waitFor(() => expect(onMoveToParticipants).toHaveBeenCalled());
  });

  it("calls the unenroll API and onUnenroll when clicked", async () => {
    deleteEnrollmentsByActivityIdByMemberId.mockResolvedValue({});
    const onUnenroll = vi.fn();
    renderTile(
      <EditWaitinglistParticipantTile
        activityId={1}
        enrollment={buildEnrollment()}
        onUnenroll={onUnenroll}
        onMoveToParticipants={vi.fn()}
      />,
    );
    fireEvent.click(screen.getByText("unenroll"));

    await vi.waitFor(() => expect(onUnenroll).toHaveBeenCalled());
  });
});
