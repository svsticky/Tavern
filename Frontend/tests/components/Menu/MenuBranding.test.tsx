import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it } from "vitest";
import MenuBranding from "~/components/Menu/MenuBranding";

describe("MenuBranding", () => {
  it("renders the default title and links to the homepage", () => {
    render(
      <MemoryRouter>
        <MenuBranding />
      </MemoryRouter>,
    );

    expect(screen.getByText("Sticky")).toBeInTheDocument();
    expect(screen.getByRole("link")).toHaveAttribute("href", "/");
  });

  it("renders a custom title, icon, and homepage link", () => {
    render(
      <MemoryRouter>
        <MenuBranding title="Tavern" icon="/logo.png" homepage="/dashboard" />
      </MemoryRouter>,
    );

    expect(screen.getByText("Tavern")).toBeInTheDocument();
    expect(screen.getByRole("img", { name: "Logo" })).toHaveAttribute(
      "src",
      "/logo.png",
    );
    expect(screen.getByRole("link")).toHaveAttribute("href", "/dashboard");
  });
});
