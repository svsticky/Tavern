import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { MemberResponseDto } from "~/api";
import * as route from "~/routes/admin/members";
import { renderRoute } from "~/testUtils";

const { getMembers, requireBoardOrCandidateBoard } = vi.hoisted(() => ({
  getMembers: vi.fn(),
  requireBoardOrCandidateBoard: vi.fn(),
}));

vi.mock("~/api", () => ({ getMembers }));
vi.mock("~/util/loaderAuth.util", () => ({ requireBoardOrCandidateBoard }));

vi.mock("~/components/Member/FilterMemberOverlay/FilterMemberOverlay", () => ({
  default: ({ onFilter }: { onFilter: (f: any) => void }) => (
    <button type="button" onClick={() => onFilter({ studyId: 5 })}>
      apply-filters
    </button>
  ),
}));

// jsdom does not implement IntersectionObserver. Capture the callback so tests
// can simulate the end of the list coming into view.
let intersectionCallback: IntersectionObserverCallback | null = null;
class IntersectionObserverStub {
  constructor(callback: IntersectionObserverCallback) {
    intersectionCallback = callback;
  }
  observe() {}
  unobserve() {}
  disconnect() {}
}

function makeMembers(count: number, offset = 0): MemberResponseDto[] {
  return Array.from({ length: count }, (_, i) => ({
    id: `member-${offset + i}`,
    firstName: `First${offset + i}`,
    lastName: `Last${offset + i}`,
    email: `member${offset + i}@example.com`,
    phoneNumber: "0612345678",
  })) as MemberResponseDto[];
}

/** Renders the members page at the given URL, with stubs for the pages it opens. */
function renderMembers(url = "/admin/members") {
  return renderRoute(route, {
    path: "/admin/members",
    route: url,
    otherRoutes: [
      {
        path: "/admin/members/create-member",
        Component: () => <div>create-member-page</div>,
      },
      { path: "/admin/members/:id", Component: () => <div>member-page</div> },
    ],
  });
}

describe("Members", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal("IntersectionObserver", IntersectionObserverStub);
    intersectionCallback = null;
    requireBoardOrCandidateBoard.mockResolvedValue({});
    getMembers.mockResolvedValue({ data: [] });
  });

  it("loads the first page of members", async () => {
    getMembers.mockResolvedValue({ data: makeMembers(20) });
    renderMembers();

    expect(await screen.findByText("First0 Last0")).toBeInTheDocument();
    expect(getMembers).toHaveBeenCalledWith(
      expect.objectContaining({
        query: expect.objectContaining({ Page: 1, PageSize: 20, Search: "" }),
      }),
    );
  });

  it("restores all loaded pages from the URL in one request", async () => {
    renderMembers("/admin/members?pages=3");

    await screen.findByText("no_data");
    expect(getMembers).toHaveBeenCalledWith(
      expect.objectContaining({
        query: expect.objectContaining({ Page: 1, PageSize: 60 }),
      }),
    );
  });

  it("uses the search and filters from the URL", async () => {
    renderMembers("/admin/members?search=jane&studyId=5&gratie=true");

    expect(await screen.findByLabelText("search")).toHaveValue("jane");
    expect(getMembers).toHaveBeenCalledWith(
      expect.objectContaining({
        query: expect.objectContaining({
          Search: "jane",
          StudyId: 5,
          Gratie: true,
        }),
      }),
    );
  });

  it("shows the error page when the members fail to load", async () => {
    getMembers.mockResolvedValue({
      error: new Error("Failed to fetch members"),
    });
    renderMembers();

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Failed to fetch members",
    );
  });

  it("shows 'no_data' when there are no members", async () => {
    renderMembers();

    expect(await screen.findByText("no_data")).toBeInTheDocument();
  });

  it("reloads the list with the search term after typing", async () => {
    renderMembers();
    await screen.findByText("no_data");

    getMembers.mockClear();
    fireEvent.change(screen.getByLabelText("search"), {
      target: { value: "jane" },
    });

    await waitFor(
      () =>
        expect(getMembers).toHaveBeenCalledWith(
          expect.objectContaining({
            query: expect.objectContaining({ Search: "jane", Page: 1 }),
          }),
        ),
      { timeout: 2000 },
    );
  });

  it("loads more members when the end of the list comes into view", async () => {
    getMembers
      .mockResolvedValueOnce({ data: makeMembers(20, 0) })
      .mockResolvedValueOnce({ data: makeMembers(5, 20) });
    renderMembers();

    await screen.findByText("First0 Last0");
    expect(intersectionCallback).toBeTruthy();

    intersectionCallback!(
      [{ isIntersecting: true } as IntersectionObserverEntry],
      {} as IntersectionObserver,
    );

    expect(await screen.findByText("First20 Last20")).toBeInTheDocument();
    expect(getMembers).toHaveBeenLastCalledWith(
      expect.objectContaining({
        query: expect.objectContaining({ Page: 2, PageSize: 20 }),
      }),
    );
    // Loading more pages must not rerun the loader.
    expect(getMembers).toHaveBeenCalledTimes(2);
    expect(screen.getByText("First0 Last0")).toBeInTheDocument();
  });

  it("reloads the list with the chosen filters", async () => {
    renderMembers();

    await screen.findByText("no_data");
    fireEvent.click(screen.getByText("filters"));
    fireEvent.click(await screen.findByText("apply-filters"));

    await waitFor(() =>
      expect(getMembers).toHaveBeenCalledWith(
        expect.objectContaining({
          query: expect.objectContaining({ StudyId: 5, Page: 1 }),
        }),
      ),
    );
  });

  it("navigates to a member's page", async () => {
    getMembers.mockResolvedValue({ data: makeMembers(1) });
    renderMembers();

    await screen.findByText("First0 Last0");
    fireEvent.click(screen.getByText("view_member"));

    expect(await screen.findByText("member-page")).toBeInTheDocument();
  });

  it("navigates to create-member when the plus button is clicked", async () => {
    renderMembers();

    await screen.findByText("no_data");
    const plusButton = document
      .querySelector("svg.lucide-plus")
      ?.closest("button");
    expect(plusButton).toBeTruthy();
    fireEvent.click(plusButton!);

    expect(await screen.findByText("create-member-page")).toBeInTheDocument();
  });
});
