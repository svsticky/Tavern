import { describe, expect, it } from "vitest";
import { FormSection } from "~/components/UI/Form/FormSection";
import { render, screen } from "~/testUtils";

describe("FormSection", () => {
  it("renders children", () => {
    render(
      <FormSection>
        <span>Field</span>
      </FormSection>,
    );
    expect(screen.getByText("Field")).toBeInTheDocument();
  });

  it("renders a FormHeader when title is provided", () => {
    render(
      <FormSection title="Personal info">
        <span>Field</span>
      </FormSection>,
    );
    expect(screen.getByText("Personal info")).toBeInTheDocument();
  });

  it("does not render a header when title is omitted", () => {
    render(
      <FormSection>
        <span>Field</span>
      </FormSection>,
    );
    expect(screen.queryByRole("heading")).not.toBeInTheDocument();
  });

  it("defaults to a 2-column grid", () => {
    const { container } = render(
      <FormSection>
        <span>Field</span>
      </FormSection>,
    );
    const grid = container.querySelector("section > div");
    expect(grid).toHaveClass("grid-cols-1", "md:grid-cols-2");
  });

  it("uses a 3-column grid when columns is 3", () => {
    const { container } = render(
      <FormSection columns={3}>
        <span>Field</span>
      </FormSection>,
    );
    const grid = container.querySelector("section > div");
    expect(grid).toHaveClass("grid-cols-1", "md:grid-cols-3");
  });

  it("uses a single column grid when columns is 1", () => {
    const { container } = render(
      <FormSection columns={1}>
        <span>Field</span>
      </FormSection>,
    );
    const grid = container.querySelector("section > div");
    expect(grid).toHaveClass("grid-cols-1");
    expect(grid).not.toHaveClass("md:grid-cols-2");
  });

  it("applies a custom className to the section", () => {
    const { container } = render(
      <FormSection className="custom-section">
        <span>Field</span>
      </FormSection>,
    );
    expect(container.querySelector("section")).toHaveClass("custom-section");
  });
});
