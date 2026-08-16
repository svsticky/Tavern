import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { Star } from "lucide-react";
import { describe, expect, it } from "vitest";
import BorderedTile from "~/components/Tiles/BorderedTile";

describe("BorderedTile", () => {
  it("renders the title, subtitle, and children", () => {
    render(
      <BorderedTile title="Title" subtitle="Subtitle">
        Body content
      </BorderedTile>,
    );

    expect(screen.getByText("Title")).toBeInTheDocument();
    expect(screen.getByText("Subtitle")).toBeInTheDocument();
    expect(screen.getByText("Body content")).toBeInTheDocument();
  });

  it("renders the provided icon", () => {
    render(
      <BorderedTile title="Title" icon={Star}>
        Body
      </BorderedTile>,
    );
    expect(document.querySelector("svg")).toBeInTheDocument();
  });

  it("toggles the collapsible section's open state when the header is clicked", async () => {
    const user = userEvent.setup();
    render(
      <BorderedTile
        title="Title"
        collapsibleContent={<div>Hidden details</div>}
      >
        Body
      </BorderedTile>,
    );

    const wrapper = screen
      .getByText("Hidden details")
      .closest(".grid") as HTMLElement;
    expect(wrapper.className).toContain("grid-rows-[0fr]");

    await user.click(screen.getByText("Title"));

    expect(wrapper.className).toContain("grid-rows-[1fr]");
  });

  it("starts open when defaultOpen is true", () => {
    render(
      <BorderedTile
        title="Title"
        collapsibleContent={<div>Hidden details</div>}
        defaultOpen
      >
        Body
      </BorderedTile>,
    );

    const wrapper = screen
      .getByText("Hidden details")
      .closest(".grid") as HTMLElement;
    expect(wrapper.className).toContain("grid-rows-[1fr]");
  });
});
