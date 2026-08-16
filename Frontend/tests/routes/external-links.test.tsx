import { render, screen, waitFor } from "@testing-library/react";
import i18next from "i18next";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ExternalLinkResponseDto } from "~/api";
import ExternalLinksPage from "~/routes/external-links";

const { getExternallinks } = vi.hoisted(() => ({
  getExternallinks: vi.fn(),
}));

vi.mock("~/api", () => ({ getExternallinks }));

function makeLink(
  overrides: Partial<ExternalLinkResponseDto> = {},
): ExternalLinkResponseDto {
  return {
    id: 1,
    titleDutch: "Koala NL",
    titleEnglish: "Koala",
    descriptionDutch: "Ledenadministratie",
    descriptionEnglish: "Membership system",
    url: "https://koala.example.com",
    sortOrder: 1,
    iconPath: null,
    ...overrides,
  } as ExternalLinkResponseDto;
}

describe("ExternalLinksPage", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  afterEach(async () => {
    await i18next.changeLanguage("en");
  });

  it("falls back to an empty list when the response has no data", async () => {
    getExternallinks.mockResolvedValue({ data: undefined });
    render(<ExternalLinksPage />);

    expect(await screen.findByText("no_external_links")).toBeInTheDocument();
  });

  it("renders Dutch titles and descriptions for a Dutch-locale user", async () => {
    await i18next.changeLanguage("nl");
    getExternallinks.mockResolvedValue({ data: [makeLink()] });
    render(<ExternalLinksPage />);

    expect(await screen.findByText("Koala NL")).toBeInTheDocument();
    expect(screen.getByText("Ledenadministratie")).toBeInTheDocument();
  });

  it("shows a loading state, then the no-links message when empty", async () => {
    getExternallinks.mockResolvedValue({ data: [] });
    render(<ExternalLinksPage />);

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
    render(<ExternalLinksPage />);

    const headings = await screen.findAllByRole("heading", { level: 3 });
    expect(headings.map((h) => h.textContent)).toEqual(["First", "Second"]);
  });

  it("renders an icon image when iconPath is set", async () => {
    getExternallinks.mockResolvedValue({
      data: [makeLink({ iconPath: "icon.png" })],
    });
    render(<ExternalLinksPage />);

    await screen.findByText("Koala");
    expect(document.querySelector("img")).toBeTruthy();
  });

  it("logs an error when fetching fails", async () => {
    getExternallinks.mockRejectedValue(new Error("boom"));
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    render(<ExternalLinksPage />);

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });
});
