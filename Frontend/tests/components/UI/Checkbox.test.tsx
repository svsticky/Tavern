import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import Checkbox from "~/components/UI/Checkbox";
import { render, screen } from "~/testUtils";

describe("Checkbox", () => {
  it("renders the label text", () => {
    render(<Checkbox label="Accept terms" />);
    expect(screen.getByText("Accept terms")).toBeInTheDocument();
  });

  it("renders a checkbox input", () => {
    render(<Checkbox label="Accept terms" />);
    expect(screen.getByRole("checkbox")).toBeInTheDocument();
  });

  it("passes through checked and onChange props", async () => {
    const onChange = vi.fn();
    render(
      <Checkbox label="Accept terms" checked={false} onChange={onChange} />,
    );

    const checkbox = screen.getByRole("checkbox");
    await userEvent.click(checkbox);

    expect(onChange).toHaveBeenCalledTimes(1);
  });

  it("reflects the checked state", () => {
    render(<Checkbox label="Accept terms" checked readOnly />);
    expect(screen.getByRole("checkbox")).toBeChecked();
  });

  it("respects the disabled attribute", () => {
    render(<Checkbox label="Accept terms" disabled />);
    expect(screen.getByRole("checkbox")).toBeDisabled();
  });
});
