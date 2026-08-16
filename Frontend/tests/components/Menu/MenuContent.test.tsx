import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import MenuContent from "~/components/Menu/MenuContent";

describe("MenuContent", () => {
  it("renders its children inside a scrollable container", () => {
    render(
      <MenuContent>
        <div>Nav links</div>
      </MenuContent>,
    );

    const content = screen.getByText("Nav links");
    expect(content).toBeInTheDocument();
    expect(content.parentElement).toHaveClass("overflow-y-auto");
  });
});
