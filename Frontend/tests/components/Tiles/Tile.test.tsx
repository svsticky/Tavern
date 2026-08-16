import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import Tile from "~/components/Tiles/Tile";

describe("Tile", () => {
  it("renders its children", () => {
    render(<Tile>Content</Tile>);
    expect(screen.getByText("Content")).toBeInTheDocument();
  });

  it("merges a custom className with the base styling", () => {
    render(<Tile className="custom-class">Content</Tile>);
    expect(screen.getByText("Content")).toHaveClass(
      "custom-class",
      "rounded-2xl",
    );
  });
});
