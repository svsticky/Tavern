import i18next from "i18next";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import type { ExternalLinkResponseDto } from "~/api";
import * as route from "~/routes/external-links";
import { renderRoute, screen } from "~/testUtils";

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

  it("shows the no-links message when there are no links", async () => {
    getExternallinks.mockResolvedValue({ data: [] });
    renderRoute(route);

    expect(await screen.findByText("no_external_links")).toBeInTheDocument();
  });

  it("renders Dutch titles and descriptions for a Dutch-locale user", async () => {
    await i18next.changeLanguage("nl");
    getExternallinks.mockResolvedValue({ data: [makeLink()] });
    renderRoute(route);

    expect(await screen.findByText("Koala NL")).toBeInTheDocument();
    expect(screen.getByText("Ledenadministratie")).toBeInTheDocument();
  });

  it("renders the links in the order the API returns them", async () => {
    getExternallinks.mockResolvedValue({
      data: [
        makeLink({ id: 1, titleEnglish: "First", sortOrder: 1 }),
        makeLink({ id: 2, titleEnglish: "Second", sortOrder: 2 }),
      ],
    });
    renderRoute(route);

    const headings = await screen.findAllByRole("heading", { level: 3 });
    expect(headings.map((h) => h.textContent)).toEqual(["First", "Second"]);
  });

  it("renders an icon image when iconPath is set", async () => {
    getExternallinks.mockResolvedValue({
      data: [makeLink({ iconPath: "icon.png" })],
    });
    renderRoute(route);

    await screen.findByText("Koala");
    expect(
      document.querySelector("img[src$='/externallinks/1/icon']"),
    ).toBeTruthy();
  });

  it("shows the error page when the links fail to load", async () => {
    getExternallinks.mockResolvedValue({ error: "boom" });
    renderRoute(route);

    expect(await screen.findByRole("alert")).toHaveTextContent(
      "Failed to load external links",
    );
  });

  it("shows the error page when the request throws", async () => {
    getExternallinks.mockRejectedValue(new Error("boom"));
    renderRoute(route);

    expect(await screen.findByRole("alert")).toHaveTextContent("boom");
  });
});
