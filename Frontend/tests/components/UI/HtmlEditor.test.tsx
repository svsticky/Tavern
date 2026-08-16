import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import HtmlEditor from "~/components/UI/HtmlEditor";

vi.mock("react-quill-new/dist/quill.snow.css", () => ({}));
vi.mock("react-quill-new", () => ({
  default: ({
    value,
    onChange,
    placeholder,
  }: {
    value: string;
    onChange: (v: string) => void;
    placeholder?: string;
  }) => (
    <textarea
      data-testid="quill-stub"
      value={value}
      placeholder={placeholder}
      onChange={(e) => onChange(e.target.value)}
    />
  ),
}));

describe("HtmlEditor", () => {
  it("shows a loading placeholder before the editor module has mounted", () => {
    render(<HtmlEditor value="" onChange={vi.fn()} />);
    expect(screen.getByText("loading")).toBeInTheDocument();
  });

  it("renders the label and the editor once mounted", async () => {
    render(<HtmlEditor value="<p>hi</p>" onChange={vi.fn()} label="Body" />);

    await waitFor(() =>
      expect(screen.getByTestId("quill-stub")).toBeInTheDocument(),
    );
    expect(screen.getByText("Body")).toBeInTheDocument();
    expect(screen.getByTestId("quill-stub")).toHaveValue("<p>hi</p>");
  });

  it("forwards onChange calls from the editor", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<HtmlEditor value="" onChange={onChange} />);

    const editor = await waitFor(() => screen.getByTestId("quill-stub"));
    await user.type(editor, "x");

    expect(onChange).toHaveBeenCalledWith("x");
  });
});
