import { render, screen } from "@testing-library/react";
import i18next from "i18next";
import { MemoryRouter } from "react-router";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ExternalLinkResponseDto } from "~/api";
import ExternalLinksPage, { clientLoader } from "~/routes/external-links";

const { getExternallinks } = vi.hoisted(() => ({ getExternallinks: vi.fn() }));
vi.mock("~/api", () => ({ getExternallinks }));

const { requireTokenParsed } = vi.hoisted(() => ({
  requireTokenParsed: vi.fn(),
}));
vi.mock("~/util/loaderAuth.util", () => ({ requireTokenParsed }));

const { useLoaderData } = vi.hoisted(() => ({ useLoaderData: vi.fn() }));
vi.mock("react-router", async (importOriginal) => ({
  ...(await importOriginal<typeof import("react-router")>()),
  useLoaderData,
}));

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

function renderPage(links: ExternalLinkResponseDto[]) {
  useLoaderData.mockReturnValue({ links });
  return render(
    <MemoryRouter>
      <ExternalLinksPage />
    </MemoryRouter>,
  );
}

describe("external links clientLoader", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    requireTokenParsed.mockResolvedValue({ UserId: "user-1" });
  });

  it("sorts the links by sortOrder", async () => {
    getExternallinks.mockResolvedValue({
      data: [
        makeLink({ id: 2, sortOrder: 2 }),
        makeLink({ id: 1, sortOrder: 1 }),
      ],
    });

    const { links } = await clientLoader();

    expect(links.map((l) => l.id)).toEqual([1, 2]);
  });

  it("falls back to an empty list when the response has no data", async () => {
    getExternallinks.mockResolvedValue({ data: undefined });

    expect((await clientLoader()).links).toEqual([]);
  });

  it("propagates a fetch failure to React Router's error boundary", async () => {
    getExternallinks.mockResolvedValue({ error: new Error("boom") });

    await expect(clientLoader()).rejects.toThrow("boom");
  });
});

describe("ExternalLinksPage", () => {
  afterEach(async () => {
    await i18next.changeLanguage("en");
  });

  it("shows the no-links message when the loader found none", () => {
    renderPage([]);

    expect(screen.getByText("no_external_links")).toBeInTheDocument();
  });

  it("renders Dutch titles and descriptions for a Dutch-locale user", async () => {
    await i18next.changeLanguage("nl");
    renderPage([makeLink()]);

    expect(screen.getByText("Koala NL")).toBeInTheDocument();
    expect(screen.getByText("Ledenadministratie")).toBeInTheDocument();
  });

  it("renders English titles otherwise", () => {
    renderPage([makeLink()]);

    expect(screen.getByText("Koala")).toBeInTheDocument();
    expect(screen.getByText("Membership system")).toBeInTheDocument();
  });

  it("renders links in the order the loader gave them", () => {
    renderPage([
      makeLink({ id: 1, titleEnglish: "First" }),
      makeLink({ id: 2, titleEnglish: "Second" }),
    ]);

    const headings = screen.getAllByRole("heading", { level: 3 });
    expect(headings.map((h) => h.textContent)).toEqual(["First", "Second"]);
  });

  it("renders an icon image when iconPath is set", () => {
    renderPage([makeLink({ iconPath: "icon.png" })]);

    expect(document.querySelector("img")).toBeTruthy();
  });
});
