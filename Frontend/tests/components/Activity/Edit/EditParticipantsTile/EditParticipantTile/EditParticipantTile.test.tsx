import { fireEvent, render, screen } from "@testing-library/react";
import type { ReactElement } from "react";
import { MemoryRouter } from "react-router";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { EnrollmentResponseDto } from "~/api";
import EditParticipantTile from "~/components/Activity/Edit/EditParticipantsTile/EditParticipantTile/EditParticipantTile";

function renderTile(ui: ReactElement) {
  return render(<MemoryRouter>{ui}</MemoryRouter>);
}

const {
  patchEnrollmentsByActivityIdByMemberId,
  deleteEnrollmentsByActivityIdByMemberId,
} = vi.hoisted(() => ({
  patchEnrollmentsByActivityIdByMemberId: vi.fn(),
  deleteEnrollmentsByActivityIdByMemberId: vi.fn(),
}));

vi.mock("~/api", () => ({
  patchEnrollmentsByActivityIdByMemberId,
  deleteEnrollmentsByActivityIdByMemberId,
}));

vi.mock("react-hot-toast", () => ({
  default: Object.assign((...args: unknown[]) => args, {
    success: vi.fn(),
    error: vi.fn(),
    promise: vi.fn((p: Promise<unknown>, opts: any) =>
      p.then(
        (data) =>
          typeof opts.success === "function" ? opts.success(data) : data,
        (err) => (typeof opts.error === "function" ? opts.error(err) : err),
      ),
    ),
  }),
}));

function buildEnrollment(
  overrides: Partial<EnrollmentResponseDto> = {},
): EnrollmentResponseDto {
  return {
    member: { id: "member-1", firstName: "Alice", lastName: "Smith" },
    price: 5,
    ...overrides,
  } as EnrollmentResponseDto;
}

describe("EditParticipantTile", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders the member's name and price", () => {
    renderTile(
      <EditParticipantTile
        activityId={1}
        enrollment={buildEnrollment()}
        onUnenroll={vi.fn()}
      />,
    );
    expect(screen.getByText("Alice Smith")).toBeInTheDocument();
    expect(screen.getByDisplayValue("5.00")).toBeInTheDocument();
  });

  it("links the member's name to their admin profile", () => {
    renderTile(
      <EditParticipantTile
        activityId={1}
        enrollment={buildEnrollment()}
        onUnenroll={vi.fn()}
      />,
    );
    expect(screen.getByText("Alice Smith")).toHaveAttribute(
      "href",
      "/admin/members/member-1",
    );
  });

  it("renders the member's name as plain text when the member has no id", () => {
    renderTile(
      <EditParticipantTile
        activityId={1}
        enrollment={buildEnrollment({
          member: { firstName: "Alice", lastName: "Smith" },
        })}
        onUnenroll={vi.fn()}
      />,
    );
    expect(screen.getByText("Alice Smith")).not.toHaveAttribute("href");
  });

  it("shows an empty price input when the enrollment has no price", () => {
    renderTile(
      <EditParticipantTile
        activityId={1}
        enrollment={buildEnrollment({ price: undefined })}
        onUnenroll={vi.fn()}
      />,
    );
    expect(screen.getByPlaceholderText("0.00")).toHaveValue("");
  });

  it("saves the price on blur", async () => {
    patchEnrollmentsByActivityIdByMemberId.mockResolvedValue({});
    renderTile(
      <EditParticipantTile
        activityId={1}
        enrollment={buildEnrollment()}
        onUnenroll={vi.fn()}
      />,
    );
    const input = screen.getByDisplayValue("5.00");
    fireEvent.change(input, { target: { value: "10" } });
    fireEvent.blur(input);

    await vi.waitFor(() =>
      expect(patchEnrollmentsByActivityIdByMemberId).toHaveBeenCalledWith({
        path: { activityId: 1, memberId: "member-1" },
        body: [{ op: "replace", path: "/price", value: 10 }],
      }),
    );
  });

  it("saves the price on Enter and blurs the input", async () => {
    patchEnrollmentsByActivityIdByMemberId.mockResolvedValue({});
    renderTile(
      <EditParticipantTile
        activityId={1}
        enrollment={buildEnrollment()}
        onUnenroll={vi.fn()}
      />,
    );
    const input = screen.getByDisplayValue("5.00");
    fireEvent.change(input, { target: { value: "8" } });
    fireEvent.keyDown(input, { key: "Enter" });

    await vi.waitFor(() =>
      expect(patchEnrollmentsByActivityIdByMemberId).toHaveBeenCalled(),
    );
  });

  it("reverts to the previous price when the input is not a number", async () => {
    renderTile(
      <EditParticipantTile
        activityId={1}
        enrollment={buildEnrollment()}
        onUnenroll={vi.fn()}
      />,
    );
    const input = screen.getByDisplayValue("5.00");
    fireEvent.change(input, { target: { value: "abc" } });
    fireEvent.blur(input);

    await vi.waitFor(() => expect(input).toHaveValue("5.00"));
    expect(patchEnrollmentsByActivityIdByMemberId).not.toHaveBeenCalled();
  });

  it("treats a blank input as a price of 0 on blur", async () => {
    patchEnrollmentsByActivityIdByMemberId.mockResolvedValue({});
    renderTile(
      <EditParticipantTile
        activityId={1}
        enrollment={buildEnrollment()}
        onUnenroll={vi.fn()}
      />,
    );
    const input = screen.getByDisplayValue("5.00");
    fireEvent.change(input, { target: { value: "" } });
    fireEvent.blur(input);

    await vi.waitFor(() =>
      expect(patchEnrollmentsByActivityIdByMemberId).toHaveBeenCalledWith({
        path: { activityId: 1, memberId: "member-1" },
        body: [{ op: "replace", path: "/price", value: 0 }],
      }),
    );
    expect(input).toHaveValue("");
  });

  it("ignores non-Enter key presses", () => {
    renderTile(
      <EditParticipantTile
        activityId={1}
        enrollment={buildEnrollment()}
        onUnenroll={vi.fn()}
      />,
    );
    const input = screen.getByDisplayValue("5.00");
    fireEvent.change(input, { target: { value: "8" } });
    fireEvent.keyDown(input, { key: "a" });

    expect(patchEnrollmentsByActivityIdByMemberId).not.toHaveBeenCalled();
  });

  it("reverts to an empty price when the input is not a number and there is no prior price", async () => {
    renderTile(
      <EditParticipantTile
        activityId={1}
        enrollment={buildEnrollment({ price: undefined })}
        onUnenroll={vi.fn()}
      />,
    );
    const input = screen.getByPlaceholderText("0.00");
    fireEvent.change(input, { target: { value: "abc" } });
    fireEvent.blur(input);

    await vi.waitFor(() => expect(input).toHaveValue(""));
    expect(patchEnrollmentsByActivityIdByMemberId).not.toHaveBeenCalled();
  });

  it("reverts the input to zero when saving a failed price update for a previously priceless enrollment", async () => {
    patchEnrollmentsByActivityIdByMemberId.mockResolvedValue({
      error: { title: "Boom" },
    });
    renderTile(
      <EditParticipantTile
        activityId={1}
        enrollment={buildEnrollment({ price: undefined })}
        onUnenroll={vi.fn()}
      />,
    );
    const input = screen.getByPlaceholderText("0.00");
    fireEvent.change(input, { target: { value: "10" } });
    fireEvent.blur(input);

    await vi.waitFor(() =>
      expect(patchEnrollmentsByActivityIdByMemberId).toHaveBeenCalled(),
    );
    await vi.waitFor(() => expect(input).toHaveValue(""));
  });

  it("reverts the input to the previous price when a price update fails", async () => {
    patchEnrollmentsByActivityIdByMemberId.mockResolvedValue({
      error: { title: "Boom" },
    });
    renderTile(
      <EditParticipantTile
        activityId={1}
        enrollment={buildEnrollment()}
        onUnenroll={vi.fn()}
      />,
    );
    const input = screen.getByDisplayValue("5.00");
    fireEvent.change(input, { target: { value: "10" } });
    fireEvent.blur(input);

    await vi.waitFor(() =>
      expect(patchEnrollmentsByActivityIdByMemberId).toHaveBeenCalled(),
    );
    await vi.waitFor(() => expect(input).toHaveValue("5.00"));
  });

  it("calls the unenroll API and onUnenroll when the unenroll button is clicked", async () => {
    deleteEnrollmentsByActivityIdByMemberId.mockResolvedValue({});
    const onUnenroll = vi.fn();
    renderTile(
      <EditParticipantTile
        activityId={1}
        enrollment={buildEnrollment()}
        onUnenroll={onUnenroll}
      />,
    );
    fireEvent.click(screen.getByText("unenroll"));

    await vi.waitFor(() => expect(onUnenroll).toHaveBeenCalled());
    expect(deleteEnrollmentsByActivityIdByMemberId).toHaveBeenCalledWith({
      path: { activityId: 1, memberId: "member-1" },
    });
  });
});
