import { render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it } from "vitest";
import SideBar from "~/components/Menu/SideBar/Sidebar";

describe("SideBar", () => {
  it("applies a custom background color when provided", () => {
    render(
      <MemoryRouter>
        <SideBar color="#ff0000">
          <SideBar.Branding title="Tavern" />
        </SideBar>
      </MemoryRouter>,
    );
    const aside = document.querySelector("aside");
    expect(aside).toHaveClass("bg-[#ff0000]");
  });

  it("renders branding, nav items, and main content in the desktop aside", () => {
    render(
      <MemoryRouter>
        <SideBar>
          <SideBar.Branding title="Tavern" />
          <SideBar.Item item={{ id: "home", label: "Home", href: "/" }} />
          <SideBar.Content>
            <div>Main content</div>
          </SideBar.Content>
        </SideBar>
      </MemoryRouter>,
    );

    const aside = document.querySelector("aside");
    expect(aside).not.toBeNull();
    expect(aside).toHaveTextContent("Tavern");
    expect(aside).toHaveTextContent("Home");
    expect(screen.getByText("Main content")).toBeInTheDocument();
  });

  it("also renders a mobile DropdownMenu fallback with the same branding/items", () => {
    render(
      <MemoryRouter>
        <SideBar>
          <SideBar.Branding title="Tavern" />
          <SideBar.Item item={{ id: "home", label: "Home", href: "/" }} />
          <SideBar.Content>
            <div>Main content</div>
          </SideBar.Content>
        </SideBar>
      </MemoryRouter>,
    );

    // Branding is duplicated between the desktop aside and the mobile DropdownMenu.
    expect(screen.getAllByText("Tavern")).toHaveLength(2);
  });

  it("renders the footer inside the desktop aside", () => {
    render(
      <MemoryRouter>
        <SideBar>
          <SideBar.Footer>
            <div>Footer content</div>
          </SideBar.Footer>
        </SideBar>
      </MemoryRouter>,
    );

    const aside = document.querySelector("aside");
    expect(aside).toHaveTextContent("Footer content");
  });
});
