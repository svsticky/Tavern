import { act, fireEvent, render, screen } from "@testing-library/react";
import { MemoryRouter } from "react-router";
import { describe, expect, it, vi } from "vitest";
import NavBar from "~/components/Menu/NavBar/NavBar";

describe("NavBar", () => {
  it("renders branding, items, and the profile dropdown", () => {
    render(
      <MemoryRouter>
        <NavBar>
          <NavBar.Branding title="Tavern" homepage="/" />
          <NavBar.Item item={{ id: "home", label: "Home", href: "/" }} />
          <NavBar.ProfileDropdown
            username="Jane Doe"
            avatarUrl="/a.png"
            options={[]}
          />
        </NavBar>
      </MemoryRouter>,
    );

    expect(screen.getAllByText("Tavern").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Home").length).toBeGreaterThan(0);
    expect(screen.getAllByText("Jane Doe").length).toBeGreaterThan(0);
  });

  it("applies a custom className to the header", () => {
    const { container } = render(
      <MemoryRouter>
        <NavBar className="my-custom-class">
          <NavBar.Branding title="Tavern" homepage="/" />
        </NavBar>
      </MemoryRouter>,
    );

    expect(container.querySelector("header.my-custom-class")).toBeTruthy();
  });

  it("applies a custom background color to the header", () => {
    const { container } = render(
      <MemoryRouter>
        <NavBar color="#ff0000">
          <NavBar.Branding title="Tavern" homepage="/" />
        </NavBar>
      </MemoryRouter>,
    );

    const header = container.querySelector("header");
    expect(header?.className).toContain("bg-[#ff0000]");
  });

  it("forwards onOptionSelect through the dropdown footer's cloned ProfileDropdown", () => {
    const onOptionSelect = vi.fn();
    render(
      <MemoryRouter>
        <NavBar>
          <NavBar.Branding title="Tavern" homepage="/" />
          <NavBar.ProfileDropdown
            username="Jane Doe"
            avatarUrl="/a.png"
            options={[{ label: "Logout", href: "/logout" }]}
            onOptionSelect={onOptionSelect}
          />
        </NavBar>
      </MemoryRouter>,
    );

    // The footer clone is only rendered once the mobile hamburger menu is opened.
    fireEvent.click(screen.getByLabelText("Open navigation menu"));

    screen.getAllByRole("button", { name: /Jane Doe/ }).forEach((btn) => {
      fireEvent.click(btn);
    });

    const options = screen.getAllByText("Logout");
    fireEvent.click(options[0]);

    expect(onOptionSelect).toHaveBeenCalledWith({
      label: "Logout",
      href: "/logout",
    });
  });

  it("does not throw when the profile dropdown has no onOptionSelect handler", () => {
    render(
      <MemoryRouter>
        <NavBar>
          <NavBar.Branding title="Tavern" homepage="/" />
          <NavBar.ProfileDropdown
            username="Jane Doe"
            avatarUrl="/a.png"
            options={[{ label: "Logout", href: "/logout" }]}
          />
        </NavBar>
      </MemoryRouter>,
    );

    fireEvent.click(screen.getByLabelText("Open navigation menu"));
    screen.getAllByRole("button", { name: /Jane Doe/ }).forEach((btn) => {
      fireEvent.click(btn);
    });
    fireEvent.click(screen.getAllByText("Logout")[0]);

    expect(screen.getAllByText("Jane Doe").length).toBeGreaterThan(0);
  });

  it("also calls the profile dropdown's own onClose prop when an option is selected", () => {
    const onClose = vi.fn();
    render(
      <MemoryRouter>
        <NavBar>
          <NavBar.Branding title="Tavern" homepage="/" />
          <NavBar.ProfileDropdown
            username="Jane Doe"
            avatarUrl="/a.png"
            options={[{ label: "Logout", href: "/logout" }]}
            // onClose isn't part of ProfileDropdown's declared props, but NavBar reads it off
            // the raw child.props at runtime (see NavBar.tsx) - cast to exercise that path.
            {...({ onClose } as any)}
          />
        </NavBar>
      </MemoryRouter>,
    );

    fireEvent.click(screen.getByLabelText("Open navigation menu"));
    screen.getAllByRole("button", { name: /Jane Doe/ }).forEach((btn) => {
      fireEvent.click(btn);
    });
    fireEvent.click(screen.getAllByText("Logout")[0]);

    expect(onClose).toHaveBeenCalled();
  });

  it("toggles compact mode when the container is resized below the threshold", () => {
    let resizeCallback: ResizeObserverCallback | null = null;
    class ResizeObserverStub {
      constructor(cb: ResizeObserverCallback) {
        resizeCallback = cb;
      }
      observe() {}
      unobserve() {}
      disconnect() {}
    }
    vi.stubGlobal("ResizeObserver", ResizeObserverStub);

    const { container } = render(
      <MemoryRouter>
        <NavBar maxWidthBeforeCompact={900}>
          <NavBar.Branding title="Tavern" homepage="/" />
        </NavBar>
      </MemoryRouter>,
    );

    const target = container.querySelector("div.w-full") as HTMLElement;
    Object.defineProperty(target, "offsetWidth", {
      value: 500,
      configurable: true,
    });
    act(() => {
      (resizeCallback as ResizeObserverCallback | null)?.([] as any, {} as any);
    });

    // NavBar's own desktop <header> is the second one in the DOM - the first
    // <header> belongs to the mobile DropdownMenu it renders internally.
    const headers = container.querySelectorAll("header");
    expect(headers[0].className).not.toContain("hidden");
    expect(headers[1].className).toContain("hidden");

    vi.unstubAllGlobals();
  });
});
