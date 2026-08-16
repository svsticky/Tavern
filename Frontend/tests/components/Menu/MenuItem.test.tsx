import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Home } from "lucide-react";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import MenuItem from "~/components/Menu/MenuItem";

describe("MenuItem", () => {
  it("renders the label and links to the item's href", () => {
    render(
      <MemoryRouter initialEntries={["/"]}>
        <MenuItem item={{ id: "home", label: "Home", href: "/home" }} />
      </MemoryRouter>,
    );

    const link = screen.getByRole("link", { name: "Home" });
    expect(link).toHaveAttribute("href", "/home");
  });

  it("renders the icon component when provided", () => {
    render(
      <MemoryRouter>
        <MenuItem
          item={{ id: "home", label: "Home", href: "/home", icon: Home }}
        />
      </MemoryRouter>,
    );

    expect(document.querySelector("svg")).toBeInTheDocument();
  });

  it("applies active styling when the current path matches the item's href", () => {
    render(
      <MemoryRouter initialEntries={["/home"]}>
        <MenuItem item={{ id: "home", label: "Home", href: "/home" }} />
      </MemoryRouter>,
    );

    expect(screen.getByRole("link", { name: "Home" })).toHaveClass(
      "bg-[var(--board-primary-light)]",
    );
  });

  it("does not apply active styling for a non-matching path", () => {
    render(
      <MemoryRouter initialEntries={["/other"]}>
        <MenuItem item={{ id: "home", label: "Home", href: "/home" }} />
      </MemoryRouter>,
    );

    expect(screen.getByRole("link", { name: "Home" })).not.toHaveClass(
      "bg-[var(--board-primary-light)]",
    );
  });

  it("calls the onClick callback when clicked", async () => {
    const user = userEvent.setup();
    const onClick = vi.fn();
    render(
      <MemoryRouter>
        <MenuItem
          item={{ id: "home", label: "Home", href: "/home" }}
          onClick={onClick}
        />
      </MemoryRouter>,
    );

    await user.click(screen.getByRole("link", { name: "Home" }));
    expect(onClick).toHaveBeenCalledTimes(1);
  });
});
