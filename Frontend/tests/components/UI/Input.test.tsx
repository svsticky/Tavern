import { describe, expect, it, vi } from "vitest";
import Input from "~/components/UI/Input";
import { render, screen } from "~/testUtils";

describe("Input", () => {
  it("renders a text input with a label", () => {
    render(<Input label="Name" value="" onChange={vi.fn()} />);
    expect(screen.getByText("Name")).toBeInTheDocument();
    expect(screen.getByRole("textbox")).toBeInTheDocument();
  });

  it("renders without a label", () => {
    render(<Input value="" onChange={vi.fn()} />);
    expect(screen.getByRole("textbox")).toBeInTheDocument();
  });

  it("shows a required asterisk next to the label when required", () => {
    render(<Input label="Name" required value="" onChange={vi.fn()} />);
    expect(screen.getByText("*")).toBeInTheDocument();
  });

  it("applies disabled styling and attribute", () => {
    render(<Input label="Name" disabled value="" onChange={vi.fn()} />);
    const input = screen.getByRole("textbox");
    expect(input).toBeDisabled();
    expect(input).toHaveClass("bg-gray-100", "cursor-not-allowed");
  });

  it("merges a custom className", () => {
    render(<Input value="" onChange={vi.fn()} className="custom-input" />);
    expect(screen.getByRole("textbox")).toHaveClass("custom-input");
  });

  it("renders an inline checkbox layout when type is checkbox", () => {
    render(
      <Input
        type="checkbox"
        label="Agree"
        checked={false}
        onChange={vi.fn()}
      />,
    );
    expect(screen.getByRole("checkbox")).toBeInTheDocument();
    expect(screen.getByText("Agree")).toBeInTheDocument();
  });

  it("shows a required asterisk for checkboxes when required", () => {
    render(
      <Input
        type="checkbox"
        label="Agree"
        required
        checked={false}
        onChange={vi.fn()}
      />,
    );
    expect(screen.getByText("*")).toBeInTheDocument();
  });
});
