import { fireEvent, render, screen, waitFor } from "@testing-library/react";
import i18next from "i18next";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ExternalLinkResponseDto } from "~/api";
import ManageExternalLinksDatatable from "~/components/Admin/ManageExternalLinksDatatable/ManageExternalLinksDatatable";

const { getExternallinks, putExternallinksById } = vi.hoisted(() => ({
  getExternallinks: vi.fn(),
  putExternallinksById: vi.fn(),
}));

vi.mock("~/api", () => ({ getExternallinks, putExternallinksById }));

vi.mock("react-hot-toast", () => ({
  default: {
    promise: vi.fn((p: Promise<unknown>) => p.catch(() => {})),
  },
}));

vi.mock(
  "~/components/Admin/EditExternalLinkOverlay/EditExternalLinkOverlay",
  () => ({
    default: ({ onComplete }: { onComplete: () => void }) => (
      <button type="button" onClick={onComplete}>
        complete-edit
      </button>
    ),
  }),
);

function makeLink(
  overrides: Partial<ExternalLinkResponseDto> = {},
): ExternalLinkResponseDto {
  return {
    id: 1,
    titleDutch: "Koala",
    titleEnglish: "Koala",
    descriptionDutch: "Ledenadministratie",
    descriptionEnglish: "Membership system",
    url: "https://koala.example.com",
    sortOrder: 1,
    iconPath: null,
    ...overrides,
  } as ExternalLinkResponseDto;
}

describe("ManageExternalLinksDatatable", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(async () => {
    await i18next.changeLanguage("en");
  });

  it("falls back to an empty list when the response has no data", async () => {
    getExternallinks.mockResolvedValue({ data: undefined });
    render(<ManageExternalLinksDatatable />);

    expect(await screen.findByText("no_external_links")).toBeInTheDocument();
  });

  it("renders an icon image when iconPath is set, and Dutch text for a Dutch-locale user", async () => {
    await i18next.changeLanguage("nl");
    getExternallinks.mockResolvedValue({
      data: [makeLink({ iconPath: "icon.png" })],
    });
    render(<ManageExternalLinksDatatable />);

    await screen.findByText("Koala");
    expect(screen.getByText("Ledenadministratie")).toBeInTheDocument();
    expect(document.querySelector("img")).toBeTruthy();
  });

  it("falls back to the default link icon component past the default icon list", async () => {
    getExternallinks.mockResolvedValue({
      data: Array.from(
        { length: 12 },
        (_, idx) =>
          makeLink({
            id: idx + 1,
            titleEnglish: `Link ${idx}`,
            sortOrder: idx,
          }) as ExternalLinkResponseDto,
      ),
    });
    render(<ManageExternalLinksDatatable />);

    expect(await screen.findByText("Link 11")).toBeInTheDocument();
  });

  it("does not reorder when dragging over the same row", async () => {
    getExternallinks.mockResolvedValue({
      data: [
        makeLink({ id: 1, titleEnglish: "First", sortOrder: 1 }),
        makeLink({ id: 2, titleEnglish: "Second", sortOrder: 2 }),
      ],
    });
    render(<ManageExternalLinksDatatable />);

    await screen.findByText("First");
    const rows = document.querySelectorAll("[draggable='true']");

    fireEvent.dragStart(rows[0], { dataTransfer: {} });
    fireEvent.dragEnter(rows[0]);

    const titles = screen.getAllByRole("heading", { level: 4 });
    expect(titles.map((t) => t.textContent)).toEqual(["First", "Second"]);
  });

  it("shows a loading state, then the no-links message when empty", async () => {
    getExternallinks.mockResolvedValue({ data: [] });
    render(<ManageExternalLinksDatatable />);

    expect(screen.getByText("loading")).toBeInTheDocument();
    expect(await screen.findByText("no_external_links")).toBeInTheDocument();
  });

  it("renders links sorted by sortOrder", async () => {
    getExternallinks.mockResolvedValue({
      data: [
        makeLink({ id: 2, titleEnglish: "Second", sortOrder: 2 }),
        makeLink({ id: 1, titleEnglish: "First", sortOrder: 1 }),
      ],
    });
    render(<ManageExternalLinksDatatable />);

    const titles = await screen.findAllByRole("heading", { level: 4 });
    expect(titles.map((t) => t.textContent)).toEqual(["First", "Second"]);
  });

  it("logs an error when fetching fails", async () => {
    getExternallinks.mockRejectedValue(new Error("boom"));
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    render(<ManageExternalLinksDatatable />);

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("opens the add-link modal and refetches on completion", async () => {
    getExternallinks.mockResolvedValue({ data: [] });
    render(<ManageExternalLinksDatatable />);

    await screen.findByText("no_external_links");
    fireEvent.click(screen.getByText("add_external_link"));
    fireEvent.click(await screen.findByText("complete-edit"));

    await waitFor(() => expect(getExternallinks).toHaveBeenCalledTimes(2));
  });

  it("opens the edit modal for an existing link", async () => {
    getExternallinks.mockResolvedValue({ data: [makeLink()] });
    render(<ManageExternalLinksDatatable />);

    await screen.findByText("Koala");
    fireEvent.click(screen.getByText("edit"));
    expect(await screen.findByText("complete-edit")).toBeInTheDocument();
  });

  it("reorders links on drag enter and persists the new order on drag end", async () => {
    getExternallinks.mockResolvedValue({
      data: [
        makeLink({ id: 1, titleEnglish: "First", sortOrder: 1 }),
        makeLink({ id: 2, titleEnglish: "Second", sortOrder: 2 }),
      ],
    });
    putExternallinksById.mockResolvedValue({});
    render(<ManageExternalLinksDatatable />);

    await screen.findByText("First");
    const rows = document.querySelectorAll("[draggable='true']");
    expect(rows.length).toBe(2);

    fireEvent.dragStart(rows[0], { dataTransfer: {} });
    fireEvent.dragEnter(rows[1]);
    fireEvent.dragEnd(rows[0]);

    await waitFor(() => expect(putExternallinksById).toHaveBeenCalled());
  });
});
