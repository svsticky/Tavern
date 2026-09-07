import { act, fireEvent, screen, waitFor } from "@testing-library/react";
import i18next from "i18next";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ActivityResponseDto } from "~/api";
import ActivityDetailsTile from "~/components/Activity/ActivityDetailsTile/ActivityDetailsTile";
import {
  handleAddToCalendar,
  handleCopyForWhatsapp,
  handleDownloadIcs,
  handleEnrollment,
  handleUnenrollment,
  handleUpdateEnrollment,
} from "~/components/Activity/ActivityDetailsTile/ActivityDetailsTile.handlers";
import { createMockAuthService, renderWithProviders } from "~/testUtils";
import type { TokenParsed } from "~/types/TokenParsed";

const { getGroupsById } = vi.hoisted(() => ({ getGroupsById: vi.fn() }));

vi.mock("~/api", async (importOriginal) => ({
  ...(await importOriginal<typeof import("~/api")>()),
  getGroupsById,
}));

vi.mock(
  "~/components/Activity/ActivityDetailsTile/ActivityDetailsTile.handlers",
  () => ({
    handleAddToCalendar: vi.fn(),
    handleCopyForWhatsapp: vi.fn(),
    handleDownloadIcs: vi.fn(),
    handleEnrollment: vi.fn(),
    handleUnenrollment: vi.fn(),
    handleUpdateEnrollment: vi.fn(),
  }),
);

vi.mock("~/components/Activity/AnswerQuestionsTile", () => ({
  default: ({
    onChange,
  }: {
    onChange: (id: number, value: string) => void;
  }) => (
    <button type="button" onClick={() => onChange(1, "Answer")}>
      answer-questions-tile
    </button>
  ),
}));

// Relative to whenever the test actually runs, rather than a fixed date, so
// enrollment-window checks (canEnroll/canUnenroll default to activity.dateTimeEnd)
// don't start failing once that fixed date is in the past.
const ONE_DAY_MS = 24 * 60 * 60 * 1000;

function buildActivity(
  overrides: Partial<ActivityResponseDto> = {},
): ActivityResponseDto {
  return {
    id: 1,
    name: "Party",
    price: 0,
    location: "Enschede",
    dateTimeStart: new Date(Date.now() + ONE_DAY_MS).toISOString(),
    dateTimeEnd: new Date(
      Date.now() + ONE_DAY_MS + 2 * 60 * 60 * 1000,
    ).toISOString(),
    dutchDescription: "Beschrijving",
    englishDescription: "Description",
    enrollments: [],
    specificationQuestions: [],
    isEnrollable: true,
    posterFileName: null,
    ...overrides,
  } as ActivityResponseDto;
}

const memberToken: TokenParsed = {
  locale: "en",
  UserId: "00000000-0000-0000-0000-000000000000" as TokenParsed["UserId"],
  access_level: "member",
  given_name: "Test",
  family_name: "User",
  name: "Test User",
};

describe("ActivityDetailsTile", () => {
  beforeEach(() => {
    getGroupsById.mockReset();
  });

  it("shows the no-poster placeholder when there is no poster", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(<ActivityDetailsTile activity={buildActivity()} />, {
      authService,
    });
    await waitFor(() => expect(authService.getTokenParsed).toHaveBeenCalled());
    expect(screen.getByText("no_poster")).toBeInTheDocument();
  });

  it("renders a poster image with the activity name as alt text", async () => {
    renderWithProviders(
      <ActivityDetailsTile
        activity={buildActivity({ posterFileName: "poster.jpg" })}
      />,
    );
    expect(
      await screen.findByRole("img", { name: "Party" }),
    ).toBeInTheDocument();
  });

  it("shows 'free' when the price is zero", async () => {
    renderWithProviders(<ActivityDetailsTile activity={buildActivity()} />);
    expect(await screen.findByText("free")).toBeInTheDocument();
  });

  it("shows a formatted price when set", async () => {
    renderWithProviders(
      <ActivityDetailsTile activity={buildActivity({ price: 7.5 })} />,
    );
    expect(await screen.findByText("€ 7.50")).toBeInTheDocument();
  });

  it("shows the English description for an English-locale user", async () => {
    await i18next.changeLanguage("en");
    renderWithProviders(<ActivityDetailsTile activity={buildActivity()} />);
    expect(await screen.findByText("Description")).toBeInTheDocument();
  });

  it("shows the Dutch description for a Dutch-locale user", async () => {
    await i18next.changeLanguage("nl");
    renderWithProviders(<ActivityDetailsTile activity={buildActivity()} />);
    expect(await screen.findByText("Beschrijving")).toBeInTheDocument();
  });

  it("updates the activity description immediately after changing language", async () => {
    await act(async () => {
      await i18next.changeLanguage("nl");
    });
    renderWithProviders(<ActivityDetailsTile activity={buildActivity()} />);
    expect(await screen.findByText("Beschrijving")).toBeInTheDocument();

    await act(async () => {
      await i18next.changeLanguage("en");
    });
    expect(await screen.findByText("Description")).toBeInTheDocument();
  });

  it("shows a sign-in button when the user can enroll and is not enrolled", async () => {
    renderWithProviders(
      <ActivityDetailsTile activity={buildActivity({ isEnrollable: true })} />,
    );
    expect(await screen.findByText("sign_in")).toBeInTheDocument();
  });

  it("calls handleEnrollment when the sign-in button is clicked", async () => {
    renderWithProviders(
      <ActivityDetailsTile activity={buildActivity({ isEnrollable: true })} />,
    );
    fireEvent.click(await screen.findByText("sign_in"));
    expect(handleEnrollment).toHaveBeenCalled();
  });

  it("shows sign-out and unenroll actions when the user is already enrolled", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(
      <ActivityDetailsTile
        activity={buildActivity({
          isEnrollable: true,
          enrollments: [
            { member: { id: memberToken.UserId }, specificationAnswers: [] },
          ] as unknown as ActivityResponseDto["enrollments"],
        })}
      />,
      { authService },
    );

    fireEvent.click(await screen.findByText("sign_out"));
    expect(handleUnenrollment).toHaveBeenCalled();
  });

  it("calls handleUpdateEnrollment when updating answers for an enrolled member with questions", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(
      <ActivityDetailsTile
        activity={buildActivity({
          isEnrollable: true,
          specificationQuestions: [
            {
              id: 1,
              questionDutch: "V",
              questionEnglish: "Q",
              type: "String",
            },
          ] as ActivityResponseDto["specificationQuestions"],
          enrollments: [
            { member: { id: memberToken.UserId }, specificationAnswers: [] },
          ] as unknown as ActivityResponseDto["enrollments"],
        })}
      />,
      { authService },
    );

    fireEvent.click(await screen.findByText("update_answers"));
    expect(handleUpdateEnrollment).toHaveBeenCalled();
  });

  it("calls handleAddToCalendar when the calendar button is clicked", async () => {
    renderWithProviders(<ActivityDetailsTile activity={buildActivity()} />);
    fireEvent.click(await screen.findByText("add_to_google_calendar"));
    expect(handleAddToCalendar).toHaveBeenCalled();
  });

  it("does not show the WhatsApp copy buttons for a non-board user", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => ({ ...memberToken, is_admin: false })),
    });
    renderWithProviders(<ActivityDetailsTile activity={buildActivity()} />, {
      authService,
    });
    await waitFor(() => expect(authService.getTokenParsed).toHaveBeenCalled());
    expect(screen.queryByText("copy NL")).not.toBeInTheDocument();
  });

  it("shows and wires up the WhatsApp copy buttons for a board user", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => ({ ...memberToken, is_admin: true })),
    });
    renderWithProviders(<ActivityDetailsTile activity={buildActivity()} />, {
      authService,
    });

    const copyNl = await screen.findByText("copy NL");
    fireEvent.click(copyNl);
    expect(handleCopyForWhatsapp).toHaveBeenCalledWith(expect.anything(), "NL");

    fireEvent.click(screen.getByText("copy EN"));
    expect(handleCopyForWhatsapp).toHaveBeenCalledWith(expect.anything(), "EN");
  });

  it("updates the poster status once the image loads or errors", async () => {
    renderWithProviders(
      <ActivityDetailsTile
        activity={buildActivity({ posterFileName: "poster.jpg" })}
      />,
    );
    const img = await screen.findByRole("img", { name: "Party" });

    fireEvent.error(img);
    expect(screen.getByText("no_poster")).toBeInTheDocument();
  });

  it("marks the poster as loaded once the image fires its load event", async () => {
    const { container } = renderWithProviders(
      <ActivityDetailsTile
        activity={buildActivity({ posterFileName: "poster.jpg" })}
      />,
    );
    const img = await screen.findByRole("img", { name: "Party" });

    fireEvent.load(img);
    expect(container.querySelector("img.opacity-100")).toBeTruthy();
  });

  it("initializes answers from the current enrollment's specification answers", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => memberToken),
    });
    renderWithProviders(
      <ActivityDetailsTile
        activity={buildActivity({
          isEnrollable: true,
          enrollments: [
            {
              member: { id: memberToken.UserId },
              specificationAnswers: [{ questionId: 1, answer: "Existing" }],
            },
          ] as unknown as ActivityResponseDto["enrollments"],
        })}
      />,
      { authService },
    );

    expect(
      await screen.findByText("answer-questions-tile"),
    ).toBeInTheDocument();
  });

  it("updates the answers state when AnswerQuestionsTile reports a change", async () => {
    renderWithProviders(
      <ActivityDetailsTile activity={buildActivity({ isEnrollable: true })} />,
    );

    fireEvent.click(await screen.findByText("answer-questions-tile"));
  });

  it("does not show enroll/unenroll actions or participant details when enrollment has not opened", async () => {
    renderWithProviders(
      <ActivityDetailsTile
        activity={buildActivity({
          isEnrollable: false,
          enrollOpenDate: undefined,
        })}
      />,
    );
    expect(
      await screen.findByText("add_to_google_calendar"),
    ).toBeInTheDocument();
    expect(screen.queryByText("sign_in")).not.toBeInTheDocument();
    expect(screen.queryByText("participants")).not.toBeInTheDocument();
    expect(screen.queryByText("enrollment_deadline")).not.toBeInTheDocument();
    expect(screen.queryByText("unenrollment_deadline")).not.toBeInTheDocument();
  });

  it("shows participant details and deadlines when enrollment has closed after closing date", async () => {
    renderWithProviders(
      <ActivityDetailsTile
        activity={buildActivity({
          isEnrollable: true,
          enrollmentDeadline: "2020-01-01T00:00:00Z",
          unenrollmentDeadline: "2020-01-01T00:00:00Z",
          dateTimeStart: "2020-01-02T00:00:00Z",
          dateTimeEnd: "2020-01-02T02:00:00Z",
          enrollments: [
            { id: 1, memberId: "m1", isOnWaitingList: false } as any,
          ],
        })}
      />,
    );
    expect(
      await screen.findByText("add_to_google_calendar"),
    ).toBeInTheDocument();
    expect(screen.queryByText("sign_in")).not.toBeInTheDocument();
    expect(screen.getByText("participants")).toBeInTheDocument();
  });

  it("shows the organizer's name and logo when the activity has an organizer", async () => {
    getGroupsById.mockResolvedValue({ data: { name: "BaCo" } });
    const { container } = renderWithProviders(
      <ActivityDetailsTile activity={buildActivity({ organizerId: 5 })} />,
    );

    expect(getGroupsById).toHaveBeenCalledWith({ path: { id: 5 } });
    expect(await screen.findByText("organizer")).toBeInTheDocument();
    expect(screen.getByText("BaCo")).toBeInTheDocument();
    // Decorative logo (alt="") - not exposed via role "img", so query the DOM directly.
    expect(container.querySelector("img")).toHaveAttribute(
      "src",
      expect.stringContaining("/groups/5/group-picture"),
    );
  });

  it("does not show an organizer info item when the activity has no organizer", async () => {
    renderWithProviders(<ActivityDetailsTile activity={buildActivity()} />);

    await screen.findByText("add_to_google_calendar");
    expect(getGroupsById).not.toHaveBeenCalled();
    expect(screen.queryByText("organizer")).not.toBeInTheDocument();
  });

  it("falls back to the default avatar when the organizer's logo fails to load", async () => {
    getGroupsById.mockResolvedValue({ data: { name: "BaCo" } });
    const { container } = renderWithProviders(
      <ActivityDetailsTile activity={buildActivity({ organizerId: 5 })} />,
    );

    await screen.findByText("BaCo");
    const logo = container.querySelector("img")!;
    fireEvent.error(logo);

    expect(logo).toHaveAttribute("src", "/profile-picture.svg");
  });

  it("triggers handleDownloadIcs when clicking the download .ics button", async () => {
    const activity = buildActivity();
    renderWithProviders(<ActivityDetailsTile activity={activity} />);

    const downloadBtn = await screen.findByRole("button", {
      name: /download_ics/i,
    });
    fireEvent.click(downloadBtn);

    expect(handleDownloadIcs).toHaveBeenCalledWith(activity, false);
  });

  it("displays waiting list position indicator when user is on the waiting list", async () => {
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => memberToken),
    });
    const activity = buildActivity({
      enrollments: [
        {
          isOnWaitingList: true,
          registeredOn: "2026-01-01T00:00:00Z",
          member: {
            id: memberToken.UserId,
            firstName: "Test",
            lastName: "User",
          } as any,
          activity: null!,
        },
      ],
    });

    renderWithProviders(<ActivityDetailsTile activity={activity} />, {
      authService,
    });

    expect(
      await screen.findByText(/waiting_list_position/i),
    ).toBeInTheDocument();
    expect(screen.getByText("#1")).toBeInTheDocument();
  });

  it("does not render export buttons in ActivityDetailsTile (delegated to participants tile)", async () => {
    const boardToken: TokenParsed = {
      ...memberToken,
      is_admin: true,
    };
    const authService = createMockAuthService({
      getTokenParsed: vi.fn(async () => boardToken),
    });
    const activity = buildActivity();

    renderWithProviders(<ActivityDetailsTile activity={activity} />, {
      authService,
    });

    expect(
      screen.queryByRole("button", { name: /export_csv/i }),
    ).not.toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: /export_pdf/i }),
    ).not.toBeInTheDocument();
  });
});
