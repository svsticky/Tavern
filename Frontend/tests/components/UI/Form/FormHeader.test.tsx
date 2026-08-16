import { describe, expect, it } from "vitest";
import { FormHeader } from "~/components/UI/Form/FormHeader";
import { render, screen } from "~/testUtils";

describe("FormHeader", () => {
  it("renders the title", () => {
    render(<FormHeader title="Details" />);
    expect(screen.getByText("Details")).toBeInTheDocument();
  });

  it("applies border classes by default", () => {
    const { container } = render(<FormHeader title="Details" />);
    expect(container.firstChild).toHaveClass("border-b", "mb-4");
  });

  it("omits the border classes and uses mb-2 when border is false", () => {
    const { container } = render(<FormHeader title="Details" border={false} />);
    expect(container.firstChild).toHaveClass("mb-2");
    expect(container.firstChild).not.toHaveClass("border-b");
  });

  it("renders children on the right side", () => {
    render(
      <FormHeader title="Details">
        <button type="button">Action</button>
      </FormHeader>,
    );
    expect(screen.getByRole("button", { name: "Action" })).toBeInTheDocument();
  });
});
