import { render, screen } from "@testing-library/react";
import { describe, expect, it, vi } from "vitest";
import MenuFooter from "~/components/Menu/MenuFooter";

function Probe({ onClose }: { onClose?: () => void }) {
  return <button onClick={onClose}>Probe</button>;
}

describe("MenuFooter", () => {
  it("renders its children", () => {
    render(
      <MenuFooter>
        <div>Footer content</div>
      </MenuFooter>,
    );
    expect(screen.getByText("Footer content")).toBeInTheDocument();
  });

  it("injects the onClose prop into valid element children", () => {
    const onClose = vi.fn();
    render(
      <MenuFooter onClose={onClose}>
        <Probe />
      </MenuFooter>,
    );

    screen.getByText("Probe").click();
    expect(onClose).toHaveBeenCalledTimes(1);
  });

  it("passes through non-element children (e.g. plain text) unchanged", () => {
    render(<MenuFooter>{"plain text child"}</MenuFooter>);
    expect(screen.getByText("plain text child")).toBeInTheDocument();
  });

  it("renders nothing extra when there are no children", () => {
    const { container } = render(<MenuFooter />);
    expect(container.querySelector("div")).toBeEmptyDOMElement();
  });
});
