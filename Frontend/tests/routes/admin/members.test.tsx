import { configure } from "@testing-library/dom";
import { fireEvent, screen, waitFor } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { MemberResponseDto } from "~/api";
import Members from "~/routes/admin/members";
import { renderWithProviders } from "~/testUtils";

// The component debounces its initial fetch by 300ms; under full-suite parallel load the
// default 1000ms async-query timeout can be too tight, so give these queries more headroom.
configure({ asyncUtilTimeout: 20000 });
vi.setConfig({ testTimeout: 25000 });

const { getMembers } = vi.hoisted(() => ({
  getMembers: vi.fn(),
}));

vi.mock("~/api", () => ({ getMembers }));

const toastErrorFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: { error: (...args: unknown[]) => toastErrorFn(...args) },
}));

vi.mock("~/components/Member/FilterMemberOverlay/FilterMemberOverlay", () => ({
  default: ({ onFilter }: { onFilter: (f: any) => void }) => (
    <button type="button" onClick={() => onFilter({ studyId: 5 })}>
      apply-filters
    </button>
  ),
}));

// jsdom does not implement IntersectionObserver. Stub it locally (not touching the shared
// vitest.setup.ts) and capture the callback so tests can simulate the loader coming into view.
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

describe("Members", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.stubGlobal("IntersectionObserver", IntersectionObserverStub);
    intersectionCallback = null;
  });

  it("fetches the first page of members on mount", async () => {
    getMembers.mockResolvedValue({ data: makeMembers(20) });
    renderWithProviders(<Members />);

    expect(await screen.findByText("First0 Last0")).toBeInTheDocument();
    expect(getMembers).toHaveBeenCalledWith(
      expect.objectContaining({
        query: expect.objectContaining({ Page: 1, Search: "" }),
      }),
    );
  });

  it("shows an error toast when fetching members fails", async () => {
    getMembers.mockResolvedValue({ error: "fail" });
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    renderWithProviders(<Members />);

    await waitFor(() => expect(toastErrorFn).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("shows 'no_data' when there are no members and no more pages", async () => {
    getMembers.mockResolvedValue({ data: [] });
    renderWithProviders(<Members />);

    expect(await screen.findByText("no_data")).toBeInTheDocument();
  });

  it("debounces the search query before refetching", async () => {
    getMembers.mockResolvedValue({ data: [] });
    renderWithProviders(<Members />);
    await screen.findByText("no_data");

    getMembers.mockClear();
    fireEvent.change(screen.getByLabelText("search"), {
      target: { value: "jane" },
    });

    await waitFor(
      () =>
        expect(getMembers).toHaveBeenCalledWith(
          expect.objectContaining({
            query: expect.objectContaining({ Search: "jane" }),
          }),
        ),
      { timeout: 2000 },
    );
  });

  it("loads more members when the loader comes into view", async () => {
    getMembers
      .mockResolvedValueOnce({ data: makeMembers(20, 0) })
      .mockResolvedValueOnce({ data: makeMembers(5, 20) });
    renderWithProviders(<Members />);

    await screen.findByText("First0 Last0");
    expect(intersectionCallback).toBeTruthy();

    intersectionCallback!(
      [{ isIntersecting: true } as IntersectionObserverEntry],
      {} as IntersectionObserver,
    );

    expect(await screen.findByText("First20 Last20")).toBeInTheDocument();
  });

  it("opens the filters modal and applies filters", async () => {
    getMembers.mockResolvedValue({ data: [] });
    renderWithProviders(<Members />);

    await screen.findByText("no_data");
    fireEvent.click(screen.getByText("filters"));
    fireEvent.click(await screen.findByText("apply-filters"));

    await waitFor(() =>
      expect(getMembers).toHaveBeenCalledWith(
        expect.objectContaining({
          query: expect.objectContaining({ StudyId: 5 }),
        }),
      ),
    );
  });

  it("navigates to create-member when the plus button is clicked", async () => {
    getMembers.mockResolvedValue({ data: [] });
    renderWithProviders(<Members />);

    await screen.findByText("no_data");
    const plusButton = document
      .querySelector("svg.lucide-plus")
      ?.closest("button");
    expect(plusButton).toBeTruthy();
    fireEvent.click(plusButton!);
  });

  it("renders honorary and merit badges in the table", async () => {
    getMembers.mockResolvedValue({
      data: [
        {
          id: "m-1",
          firstName: "Arthur",
          lastName: "Dent",
          email: "arthur@example.com",
          phoneNumber: "0612345678",
          ereLid: true,
        },
        {
          id: "m-2",
          firstName: "Ford",
          lastName: "Prefect",
          email: "ford@example.com",
          phoneNumber: "0612345679",
          lidVanVerdienste: true,
        },
      ],
    });
    renderWithProviders(<Members />);

    expect(await screen.findByText("Arthur Dent")).toBeInTheDocument();
    expect(screen.getByText("ere_lid")).toBeInTheDocument();
    expect(screen.getByText("Ford Prefect")).toBeInTheDocument();
    expect(screen.getByText("lid_van_verdienste")).toBeInTheDocument();
  });
});
