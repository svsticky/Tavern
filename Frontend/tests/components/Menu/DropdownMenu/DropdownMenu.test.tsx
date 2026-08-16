import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { MemoryRouter } from "react-router";
import { describe, expect, it } from "vitest";
import DropdownMenu from "~/components/Menu/DropdownMenu/DropdownMenu";

function renderMenu() {
  return render(
    <MemoryRouter initialEntries={["/"]}>
      <DropdownMenu>
        <DropdownMenu.Branding title="Tavern" />
        <DropdownMenu.Item item={{ id: "home", label: "Home", href: "/" }} />
        <DropdownMenu.Item
          item={{ id: "activities", label: "Activities", href: "/activities" }}
        />
        <DropdownMenu.Footer>
          <div>Footer content</div>
        </DropdownMenu.Footer>
      </DropdownMenu>
    </MemoryRouter>,
  );
}

describe("DropdownMenu", () => {
  it("applies a custom background color when provided", () => {
    render(
      <MemoryRouter initialEntries={["/"]}>
        <DropdownMenu color="#ff0000">
          <DropdownMenu.Branding title="Tavern" />
        </DropdownMenu>
      </MemoryRouter>,
    );
    expect(screen.getByText("Tavern").closest("header")).toHaveClass(
      "bg-[#ff0000]",
    );
  });

  it("always renders the branding", () => {
    renderMenu();
    expect(screen.getByText("Tavern")).toBeInTheDocument();
  });

  it("hides the nav items and footer until the toggle button is clicked", () => {
    renderMenu();
    expect(screen.queryByText("Activities")).not.toBeInTheDocument();
    expect(screen.queryByText("Footer content")).not.toBeInTheDocument();
  });

  it("shows the nav items and footer after clicking the toggle button", async () => {
    const user = userEvent.setup();
    renderMenu();

    await user.click(
      screen.getByRole("button", { name: /open navigation menu/i }),
    );

    expect(screen.getByText("Activities")).toBeInTheDocument();
    expect(screen.getByText("Footer content")).toBeInTheDocument();
  });

  it("closes the menu when a nav item is clicked", async () => {
    const user = userEvent.setup();
    renderMenu();

    await user.click(
      screen.getByRole("button", { name: /open navigation menu/i }),
    );
    await user.click(screen.getByText("Activities"));

    expect(screen.queryByText("Activities")).not.toBeInTheDocument();
  });

  it("closes the menu when the footer's onClose is invoked", async () => {
    function FooterChild({ onClose }: { onClose?: () => void }) {
      return (
        <button type="button" onClick={onClose}>
          close-footer
        </button>
      );
    }

    const user = userEvent.setup();
    render(
      <MemoryRouter initialEntries={["/"]}>
        <DropdownMenu>
          <DropdownMenu.Branding title="Tavern" />
          <DropdownMenu.Item item={{ id: "home", label: "Home", href: "/" }} />
          <DropdownMenu.Footer>
            <FooterChild />
          </DropdownMenu.Footer>
        </DropdownMenu>
      </MemoryRouter>,
    );

    await user.click(
      screen.getByRole("button", { name: /open navigation menu/i }),
    );
    expect(screen.getByText("close-footer")).toBeInTheDocument();

    await user.click(screen.getByText("close-footer"));

    expect(screen.queryByText("close-footer")).not.toBeInTheDocument();
  });
});
