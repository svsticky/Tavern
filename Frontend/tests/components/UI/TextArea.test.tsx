import { describe, expect, it, vi } from "vitest";
import TextArea from "~/components/UI/TextArea";
import { fireEvent, render, screen } from "~/testUtils";

describe("TextArea", () => {
  it("renders a textarea with a label", () => {
    render(<TextArea label="Notes" value="" onChange={vi.fn()} />);
    expect(screen.getByText("Notes")).toBeInTheDocument();
    expect(screen.getByRole("textbox")).toBeInTheDocument();
  });

  it("shows a required asterisk when required", () => {
    render(<TextArea label="Notes" required value="" onChange={vi.fn()} />);
    expect(screen.getByText("*")).toBeInTheDocument();
  });

  it("applies disabled styling and attribute", () => {
    render(<TextArea label="Notes" disabled value="" onChange={vi.fn()} />);
    const textarea = screen.getByRole("textbox");
    expect(textarea).toBeDisabled();
    expect(textarea).toHaveClass("bg-gray-100", "cursor-not-allowed");
  });

  // NOTE: unlike Input/Select, TextArea does not destructure `className` out of
  // `...props` before spreading it onto the <textarea>, so the explicit
  // className={cn(...)} on the element (which comes after the spread in JSX)
  // always wins and silently discards any custom className passed in. This
  // documents the current (likely unintended) behavior rather than the
  // Input/Select merging behavior.
  it("always applies the base styling classes regardless of a passed className", () => {
    render(
      <TextArea
        label="Notes"
        value=""
        onChange={vi.fn()}
        className="custom-area"
      />,
    );
    const textarea = screen.getByRole("textbox");
    expect(textarea).toHaveClass("p-2", "rounded-md", "border");
    expect(textarea).not.toHaveClass("custom-area");
  });

  it("passes through rows and placeholder attributes", () => {
    render(
      <TextArea
        label="Notes"
        rows={5}
        placeholder="Type here"
        value=""
        onChange={vi.fn()}
      />,
    );
    const textarea = screen.getByRole("textbox");
    expect(textarea).toHaveAttribute("rows", "5");
    expect(textarea).toHaveAttribute("placeholder", "Type here");
  });

  it("renders a character counter when maxLength is provided and updates on input", () => {
    const handleChange = vi.fn();
    render(
      <TextArea
        label="Bio"
        maxLength={100}
        defaultValue="Hello"
        onChange={handleChange}
      />,
    );

    expect(screen.getByText("5 / 100")).toBeInTheDocument();

    const textarea = screen.getByRole("textbox");
    fireEvent.change(textarea, { target: { value: "Hello world!" } });

    expect(screen.getByText("12 / 100")).toBeInTheDocument();
    expect(handleChange).toHaveBeenCalled();
  });
});
