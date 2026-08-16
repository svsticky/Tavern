import { fireEvent, render, screen } from "@testing-library/react";
import { createContext } from "react";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import ProfileDropdown, {
  type ProfileDropdownContextValues,
} from "~/components/Menu/NavBar/ProfileDropdown/ProfileDropdown";

function renderDropdown(
  props: Partial<React.ComponentProps<typeof ProfileDropdown>> = {},
) {
  return render(
    <MemoryRouter>
      <ProfileDropdown
        username="Jane Doe"
        options={[
          { label: "Account", href: "/account" },
          { label: "Logout", href: "/logout" },
        ]}
        {...props}
      />
    </MemoryRouter>,
  );
}

describe("ProfileDropdown", () => {
  it("renders the username and avatar", () => {
    renderDropdown();
    expect(screen.getByText("Jane Doe")).toBeInTheDocument();
    expect(screen.getByAltText("Jane Doe avatar")).toHaveAttribute(
      "src",
      "/default-avatar.png",
    );
  });

  it("uses a custom avatar url when provided", () => {
    renderDropdown({ avatarUrl: "/custom.png" });
    expect(screen.getByAltText("Jane Doe avatar")).toHaveAttribute(
      "src",
      "/custom.png",
    );
  });

  it("does not show options until the toggle button is clicked", () => {
    renderDropdown();
    expect(screen.queryByText("Account")).not.toBeInTheDocument();
  });

  it("opens the dropdown when the button is clicked and closes it on a second click", () => {
    renderDropdown();
    fireEvent.click(screen.getByRole("button"));
    expect(screen.getByText("Account")).toBeInTheDocument();

    fireEvent.click(screen.getByRole("button"));
    expect(screen.queryByText("Account")).not.toBeInTheDocument();
  });

  it("selects an option, calling onOptionSelect and onClose, and closes the dropdown", () => {
    const onOptionSelect = vi.fn();
    const onClose = vi.fn();
    renderDropdown({ onOptionSelect, onClose });

    fireEvent.click(screen.getByRole("button"));
    fireEvent.click(screen.getByText("Account"));

    expect(onOptionSelect).toHaveBeenCalledWith({
      label: "Account",
      href: "/account",
    });
    expect(onClose).toHaveBeenCalled();
    expect(screen.queryByText("Account")).not.toBeInTheDocument();
  });

  it("calls onClose without toggling selection when the option is ctrl/meta/shift/middle-clicked", () => {
    const onOptionSelect = vi.fn();
    const onClose = vi.fn();
    renderDropdown({ onOptionSelect, onClose });

    fireEvent.click(screen.getByRole("button"));
    fireEvent.click(screen.getByText("Account"), { ctrlKey: true });

    expect(onOptionSelect).not.toHaveBeenCalled();
    expect(onClose).toHaveBeenCalled();
  });

  it("closes the dropdown when clicking outside of it", () => {
    renderDropdown();
    fireEvent.click(screen.getByRole("button"));
    expect(screen.getByText("Account")).toBeInTheDocument();

    fireEvent.mouseDown(document.body);
    expect(screen.queryByText("Account")).not.toBeInTheDocument();
  });

  it("always shows options inline and ignores toggle clicks in compact mode", () => {
    const context = createContext<ProfileDropdownContextValues>({
      compact: true,
      setCompact: () => {},
    });
    renderDropdown({ context });

    expect(screen.getByText("Account")).toBeInTheDocument();
    fireEvent.click(screen.getByRole("button"));
    expect(screen.getByText("Account")).toBeInTheDocument();
  });
});
