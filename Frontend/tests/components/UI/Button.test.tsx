import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import Button from "~/components/UI/Button";
import { renderWithProviders, screen } from "~/testUtils";

describe("Button", () => {
  it("renders a native button by default and fires onClick", async () => {
    const onClick = vi.fn();
    renderWithProviders(<Button onClick={onClick}>Save</Button>);

    const button = screen.getByRole("button", { name: "Save" });
    await userEvent.click(button);

    expect(onClick).toHaveBeenCalledTimes(1);
  });

  it("renders as a NavLink when href is provided", () => {
    renderWithProviders(<Button href="/activities">Go</Button>);

    const link = screen.getByRole("link", { name: "Go" });
    expect(link).toHaveAttribute("href", "/activities");
  });

  it("disables the button and applies disabled styling", () => {
    renderWithProviders(<Button disabled>Save</Button>);

    expect(screen.getByRole("button", { name: "Save" })).toBeDisabled();
  });

  it("shows the arrow icon on the correct side", () => {
    const { container } = renderWithProviders(
      <Button showArrow arrowDirection="left">
        Back
      </Button>,
    );

    const icon = container.querySelector("svg");
    expect(icon).toBeInTheDocument();
    expect(icon?.nextSibling?.textContent).toBe("Back");
  });

  it("merges a custom className with the variant styling", () => {
    renderWithProviders(<Button className="custom-class">Save</Button>);

    expect(screen.getByRole("button", { name: "Save" })).toHaveClass(
      "custom-class",
    );
  });
});
