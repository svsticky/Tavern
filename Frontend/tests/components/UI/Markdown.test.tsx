import { render, screen } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import Markdown from "~/components/UI/Markdown";

describe("Markdown", () => {
  it("renders basic markdown as HTML", () => {
    render(<Markdown>{"# Title\n\nSome **bold** text."}</Markdown>);

    expect(
      screen.getByRole("heading", { level: 1, name: "Title" }),
    ).toBeInTheDocument();
    expect(screen.getByText("bold")).toBeInTheDocument();
  });

  it("renders links with target=_blank and rel=noreferrer", () => {
    render(<Markdown>{"[Tavern](https://example.com)"}</Markdown>);

    const link = screen.getByRole("link", { name: "Tavern" });
    expect(link).toHaveAttribute("href", "https://example.com");
    expect(link).toHaveAttribute("target", "_blank");
    expect(link).toHaveAttribute("rel", "noreferrer");
  });

  it("renders GFM features like tables via remark-gfm", () => {
    const table = "| A | B |\n| - | - |\n| 1 | 2 |";
    render(<Markdown>{table}</Markdown>);

    expect(screen.getByRole("table")).toBeInTheDocument();
    expect(screen.getByText("1")).toBeInTheDocument();
  });

  it("applies custom list styling", () => {
    render(<Markdown>{"- one\n- two"}</Markdown>);
    expect(screen.getByRole("list")).toHaveClass("list-disc");
  });
});
