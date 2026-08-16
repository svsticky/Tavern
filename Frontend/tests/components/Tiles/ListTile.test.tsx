import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import { ListTile } from "~/components/Tiles/ListTile";

describe("ListTile", () => {
  it("renders every child", () => {
    const { getByText } = render(
      <ListTile>
        <div>Item 1</div>
        <div>Item 2</div>
        <div>Item 3</div>
      </ListTile>,
    );

    expect(getByText("Item 1")).toBeInTheDocument();
    expect(getByText("Item 2")).toBeInTheDocument();
    expect(getByText("Item 3")).toBeInTheDocument();
  });

  it("adds a bottom border to every item except the last", () => {
    const { getByText } = render(
      <ListTile>
        <div>Item 1</div>
        <div>Item 2</div>
      </ListTile>,
    );

    expect(getByText("Item 1").parentElement).toHaveClass("border-b");
    expect(getByText("Item 2").parentElement).not.toHaveClass("border-b");
  });
});
