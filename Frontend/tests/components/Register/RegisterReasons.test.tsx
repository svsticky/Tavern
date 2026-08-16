import { screen } from "@testing-library/react";
import i18next from "i18next";
import { afterEach, describe, expect, it } from "vitest";
import type { RegisterReasonResponseDto } from "~/api";
import RegisterReasons from "~/components/Register/RegisterReasons";
import { render } from "~/testUtils";

describe("RegisterReasons", () => {
  afterEach(async () => {
    await i18next.changeLanguage("en");
  });

  it("renders dynamic reasons using the Dutch fields for a Dutch-locale user", async () => {
    await i18next.changeLanguage("nl");
    const reasons: RegisterReasonResponseDto[] = [
      {
        id: 1,
        titleDutch: "Titel NL",
        titleEnglish: "Title EN",
        descriptionDutch: "Omschrijving NL",
        descriptionEnglish: "Description EN",
        sortOrder: 1,
        iconPath: null,
      } as RegisterReasonResponseDto,
    ];

    render(<RegisterReasons reasons={reasons} />);

    expect(screen.getByText("Titel NL")).toBeInTheDocument();
    expect(screen.getByText("Omschrijving NL")).toBeInTheDocument();
  });

  it("falls back to a default icon component when there are more dynamic reasons than default icons", () => {
    const reasons: RegisterReasonResponseDto[] = Array.from(
      { length: 7 },
      (_, idx) =>
        ({
          id: idx + 1,
          titleDutch: `Titel ${idx}`,
          titleEnglish: `Title ${idx}`,
          descriptionDutch: `Omschrijving ${idx}`,
          descriptionEnglish: `Description ${idx}`,
          sortOrder: idx,
          iconPath: null,
        }) as RegisterReasonResponseDto,
    );

    render(<RegisterReasons reasons={reasons} />);

    expect(screen.getByText("Title 6")).toBeInTheDocument();
  });
  it("renders a skeleton with 6 placeholder tiles while loading", () => {
    const { container } = render(<RegisterReasons loading />);

    expect(container.querySelectorAll(".animate-pulse")).toHaveLength(6);
  });

  it("renders the fallback reasons when no dynamic reasons are given", () => {
    render(<RegisterReasons />);

    expect(screen.getByText("book_discounts")).toBeInTheDocument();
    expect(screen.getByText("book_discounts_description")).toBeInTheDocument();
    expect(screen.getByText("members")).toBeInTheDocument();
  });

  it("renders the fallback reasons when the dynamic reasons array is empty", () => {
    render(<RegisterReasons reasons={[]} />);

    expect(screen.getByText("book_discounts")).toBeInTheDocument();
  });

  it("renders dynamic reasons using the English fields and a plain icon when there is no iconPath", () => {
    const reasons: RegisterReasonResponseDto[] = [
      {
        id: 1,
        titleDutch: "Titel NL",
        titleEnglish: "Title EN",
        descriptionDutch: "Omschrijving NL",
        descriptionEnglish: "Description EN",
        sortOrder: 1,
        iconPath: null,
      } as RegisterReasonResponseDto,
    ];

    const { container } = render(<RegisterReasons reasons={reasons} />);

    expect(screen.getByText("Title EN")).toBeInTheDocument();
    expect(screen.getByText("Description EN")).toBeInTheDocument();
    expect(screen.queryByText("book_discounts")).not.toBeInTheDocument();
    expect(container.querySelector("img")).not.toBeInTheDocument();
  });

  it("builds an icon image URL for dynamic reasons that have an iconPath", () => {
    const reasons: RegisterReasonResponseDto[] = [
      {
        id: 7,
        titleDutch: "Titel NL",
        titleEnglish: "Title EN",
        descriptionDutch: "Omschrijving NL",
        descriptionEnglish: "Description EN",
        sortOrder: 1,
        iconPath: "uploaded-icon.png",
      } as RegisterReasonResponseDto,
    ];

    const { container } = render(<RegisterReasons reasons={reasons} />);

    const img = container.querySelector("img");
    expect(img).toHaveAttribute(
      "src",
      expect.stringContaining("/registerreasons/7/icon"),
    );
  });
});
