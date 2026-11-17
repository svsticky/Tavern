import { render } from "@testing-library/react";
import { describe, expect, it } from "vitest";
import KoalaIllustration from "~/components/ErrorPage/KoalaIllustration";
import { KOALA_MARK_PATHS } from "~/components/KoalaMark";

describe("KoalaIllustration", () => {
  it("is hidden from assistive technology and applies the class name", () => {
    const { container } = render(
      <KoalaIllustration mood="lost" className="custom" />,
    );
    const svg = container.querySelector("svg");
    expect(svg).toHaveAttribute("aria-hidden", "true");
    expect(svg).toHaveClass("custom");
  });

  it("draws the koala logo in every mood", () => {
    for (const mood of ["lost", "forbidden", "error"] as const) {
      const { container, unmount } = render(<KoalaIllustration mood={mood} />);
      expect(container.querySelectorAll("path").length).toBeGreaterThanOrEqual(
        KOALA_MARK_PATHS.length,
      );
      unmount();
    }
  });

  it("shows a no entry sign and no question marks when forbidden", () => {
    const { container } = render(<KoalaIllustration mood="forbidden" />);
    expect(container.querySelector("circle")).toBeInTheDocument();
    expect(container.querySelector("rect")).toBeInTheDocument();
    expect(container.querySelector("text")).not.toBeInTheDocument();
  });

  it("shows question marks and no sign when lost", () => {
    const { container } = render(<KoalaIllustration mood="lost" />);
    const marks = Array.from(container.querySelectorAll("text")).map(
      (t) => t.textContent,
    );
    expect(marks).toEqual(["?", "?"]);
    expect(container.querySelector("circle")).not.toBeInTheDocument();
    expect(container.querySelector("rect")).not.toBeInTheDocument();
  });

  it("shows exclamation marks and no sign when in error", () => {
    const { container } = render(<KoalaIllustration mood="error" />);
    const marks = Array.from(container.querySelectorAll("text")).map((t) =>
      t.textContent?.trim(),
    );
    expect(marks).toEqual(["!", "!"]);
    expect(container.querySelector("circle")).not.toBeInTheDocument();
  });
});
