import { screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import ConfirmMail from "~/routes/confirm-mail";
import { renderWithProviders } from "~/testUtils";

const { postMembersByIdActivationEmail } = vi.hoisted(() => ({
  postMembersByIdActivationEmail: vi.fn(),
}));

vi.mock("~/api", () => ({ postMembersByIdActivationEmail }));

describe("ConfirmMail", () => {
  beforeEach(() => {
    postMembersByIdActivationEmail.mockClear();
  });

  it("shows the confirmation message immediately when no memberId is present", () => {
    renderWithProviders(<ConfirmMail />, { route: "/confirm-mail" });

    expect(screen.getByText(/confirm_mail_description/)).toBeInTheDocument();
    expect(postMembersByIdActivationEmail).not.toHaveBeenCalled();
  });

  it("shows loading, then the confirmation message once the email is sent", async () => {
    postMembersByIdActivationEmail.mockResolvedValue({
      status: 200,
      data: "Sent",
    });

    renderWithProviders(<ConfirmMail />, {
      route: "/confirm-mail?memberId=member-1",
    });

    expect(screen.getByText(/loading/)).toBeInTheDocument();

    await waitFor(() =>
      expect(postMembersByIdActivationEmail).toHaveBeenCalledWith({
        path: { id: "member-1" },
      }),
    );
    await waitFor(() =>
      expect(screen.getByText(/confirm_mail_description/)).toBeInTheDocument(),
    );
  });

  it("shows the confirmation message when the email was already sent before", async () => {
    postMembersByIdActivationEmail.mockResolvedValue({
      status: 200,
      data: "AlreadySent",
    });

    renderWithProviders(<ConfirmMail />, {
      route: "/confirm-mail?memberId=member-2",
    });

    await waitFor(() =>
      expect(screen.getByText(/confirm_mail_description/)).toBeInTheDocument(),
    );
    expect(postMembersByIdActivationEmail).toHaveBeenCalledTimes(1);
  });

  it("shows the admin-facing message and a link back to the member when createdByAdmin is set", async () => {
    postMembersByIdActivationEmail.mockResolvedValue({
      status: 200,
      data: "Sent",
    });

    renderWithProviders(<ConfirmMail />, {
      route: "/confirm-mail?memberId=member-4&createdByAdmin=true",
    });

    await waitFor(() =>
      expect(
        screen.getByText(/verification_mail_sent_to_new_user_description/),
      ).toBeInTheDocument(),
    );
    expect(
      screen.queryByText(/confirm_mail_description/),
    ).not.toBeInTheDocument();

    const link = screen.getByRole("link", { name: /back_to_member/ });
    expect(link).toHaveAttribute("href", "/admin/members/member-4");
  });

  it("retries while the member isn't linked to the auth system yet, then stops", async () => {
    vi.useFakeTimers();
    postMembersByIdActivationEmail.mockResolvedValue({
      status: 200,
      data: "Pending",
    });

    renderWithProviders(<ConfirmMail />, {
      route: "/confirm-mail?memberId=member-3",
    });

    // Attempt 1 fires on mount; 4 more retries (5 total) at the fixed delay, then it gives up.
    await vi.advanceTimersByTimeAsync(0);
    for (let i = 0; i < 4; i++) {
      await vi.advanceTimersByTimeAsync(2000);
    }
    expect(postMembersByIdActivationEmail).toHaveBeenCalledTimes(5);

    // Switch to real timers so testing-library's waitFor can flush the resulting re-render.
    vi.useRealTimers();
    await waitFor(() =>
      expect(screen.getByText(/confirm_mail_description/)).toBeInTheDocument(),
    );

    // No further retries beyond the cap.
    vi.useFakeTimers();
    await vi.advanceTimersByTimeAsync(2000);
    expect(postMembersByIdActivationEmail).toHaveBeenCalledTimes(5);
    vi.useRealTimers();
  });
});
