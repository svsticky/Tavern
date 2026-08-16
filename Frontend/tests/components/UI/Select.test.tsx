import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import Select from "~/components/UI/Select";
import { render, screen } from "~/testUtils";

const options = [
  { value: "a", label: "Option A" },
  { value: "b", label: "Option B" },
];

describe("Select", () => {
  it("renders all options", () => {
    render(<Select options={options} onChange={vi.fn()} />);
    expect(
      screen.getByRole("option", { name: "Option A" }),
    ).toBeInTheDocument();
    expect(
      screen.getByRole("option", { name: "Option B" }),
    ).toBeInTheDocument();
  });

  it("renders a label when provided", () => {
    render(<Select label="Pick one" options={options} onChange={vi.fn()} />);
    expect(screen.getByText("Pick one")).toBeInTheDocument();
  });

  it("shows a required asterisk when required", () => {
    render(
      <Select label="Pick one" required options={options} onChange={vi.fn()} />,
    );
    expect(screen.getByText("*")).toBeInTheDocument();
  });

  it("fires onChange when a new option is selected", async () => {
    const onChange = vi.fn();
    render(<Select options={options} onChange={onChange} defaultValue="a" />);

    await userEvent.selectOptions(screen.getByRole("combobox"), "b");

    expect(onChange).toHaveBeenCalled();
    expect(screen.getByRole("combobox")).toHaveValue("b");
  });

  it("applies disabled styling and attribute", () => {
    render(<Select options={options} onChange={vi.fn()} disabled />);
    expect(screen.getByRole("combobox")).toBeDisabled();
    expect(screen.getByRole("combobox")).toHaveClass(
      "bg-gray-100",
      "cursor-not-allowed",
    );
  });
});
