import { render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import TriStateFilter from "~/components/UI/TriStateFilter";

describe("TriStateFilter", () => {
  it("renders the label and all three options", () => {
    render(<TriStateFilter label="Active" value={null} onChange={vi.fn()} />);

    expect(screen.getByText("Active")).toBeInTheDocument();
    expect(screen.getByText("yes")).toBeInTheDocument();
    expect(screen.getByText("no")).toBeInTheDocument();
    expect(screen.getByText("all")).toBeInTheDocument();
  });

  it("highlights the option matching the current value", () => {
    render(<TriStateFilter label="Active" value={true} onChange={vi.fn()} />);
    expect(screen.getByText("yes")).toHaveClass("bg-white");
    expect(screen.getByText("no")).not.toHaveClass("bg-white");
  });

  it("calls onChange with true, false, and null for each option", async () => {
    const user = userEvent.setup();
    const onChange = vi.fn();
    render(<TriStateFilter label="Active" value={null} onChange={onChange} />);

    await user.click(screen.getByText("yes"));
    expect(onChange).toHaveBeenLastCalledWith(true);

    await user.click(screen.getByText("no"));
    expect(onChange).toHaveBeenLastCalledWith(false);

    await user.click(screen.getByText("all"));
    expect(onChange).toHaveBeenLastCalledWith(null);
  });
});
