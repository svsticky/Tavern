import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { NoContentTile } from "~/components/Tiles/NoContentTile";

describe("NoContentTile", () => {
  it("renders the provided text", () => {
    render(<NoContentTile text="No items found" />);
    expect(screen.getByText("No items found")).toBeInTheDocument();
  });

  it("merges a custom className", () => {
    render(<NoContentTile text="Empty" className="custom-class" />);
    expect(screen.getByText("Empty")).toHaveClass("custom-class", "italic");
  });
});
