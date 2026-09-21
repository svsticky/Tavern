import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import KoalaMark, {
  KOALA_MARK_PATHS,
  koalaMarkSvgString,
} from "~/components/KoalaMark";

describe("KoalaMark", () => {
  it("renders every path of the logo", () => {
    const { container } = render(
      <svg aria-hidden="true">
        <KoalaMark />
      </svg>,
    );
    expect(container.querySelectorAll("path")).toHaveLength(
      KOALA_MARK_PATHS.length,
    );
  });

  it("tints the ears with the board's primary color by default", () => {
    const { container } = render(
      <svg aria-hidden="true">
        <KoalaMark />
      </svg>,
    );
    const fills = Array.from(container.querySelectorAll("path")).map((p) =>
      p.getAttribute("fill"),
    );
    expect(fills.filter((f) => f === "var(--board-primary)")).toHaveLength(2);
  });

  it("tints the ears with a custom primary color", () => {
    const { container } = render(
      <svg aria-hidden="true">
        <KoalaMark primary="#123456" />
      </svg>,
    );
    const fills = Array.from(container.querySelectorAll("path")).map((p) =>
      p.getAttribute("fill"),
    );
    expect(fills.filter((f) => f === "#123456")).toHaveLength(2);
    expect(fills).not.toContain("var(--board-primary)");
  });
});

describe("koalaMarkSvgString", () => {
  it("builds a standalone svg with a concrete primary color", () => {
    const svg = koalaMarkSvgString("#abcdef");
    expect(svg.startsWith("<svg")).toBe(true);
    expect(svg).toContain('xmlns="http://www.w3.org/2000/svg"');
    expect(svg.match(/<path /g)).toHaveLength(KOALA_MARK_PATHS.length);
    expect(svg.match(/fill="#abcdef"/g)).toHaveLength(2);
    expect(svg).not.toContain("var(--board-primary)");
  });
});
