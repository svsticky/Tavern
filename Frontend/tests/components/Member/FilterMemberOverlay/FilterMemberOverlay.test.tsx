import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Study } from "~/api";
import FilterMemberOverlay from "~/components/Member/FilterMemberOverlay/FilterMemberOverlay";

const { handleApplyFilters, handleResetFilters, loadStudies } = vi.hoisted(
  () => ({
    handleApplyFilters: vi.fn(),
    handleResetFilters: vi.fn(),
    loadStudies: vi.fn(),
  }),
);

vi.mock(
  "~/components/Member/FilterMemberOverlay/FilterMemberOverlay.handlers",
  () => ({
    handleApplyFilters,
    handleResetFilters,
    loadStudies,
  }),
);

describe("FilterMemberOverlay", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders every filter control", () => {
    render(<FilterMemberOverlay filters={null} onFilter={vi.fn()} />);

    expect(screen.getByLabelText("study_type")).toBeInTheDocument();
    expect(screen.getByLabelText("study")).toBeInTheDocument();
    expect(screen.getByText("gratie")).toBeInTheDocument();
    expect(screen.getByText("lid_van_verdienste")).toBeInTheDocument();
    expect(screen.getByText("ere_lid")).toBeInTheDocument();
    expect(screen.getByText("begunstiger")).toBeInTheDocument();
    expect(screen.getByText("suspended")).toBeInTheDocument();
    expect(screen.getByText("inactive")).toBeInTheDocument();
  });

  it("loads studies on mount", () => {
    render(<FilterMemberOverlay filters={null} onFilter={vi.fn()} />);
    expect(loadStudies).toHaveBeenCalledWith(
      expect.any(Function),
      expect.any(Function),
    );
  });

  it("calls handleResetFilters when reset is clicked", async () => {
    const user = userEvent.setup();
    render(<FilterMemberOverlay filters={null} onFilter={vi.fn()} />);

    await user.click(screen.getByRole("button", { name: "reset" }));

    expect(handleResetFilters).toHaveBeenCalledTimes(1);
  });

  it("calls handleApplyFilters with the current filter state when applying", async () => {
    const user = userEvent.setup();
    const onFilter = vi.fn();
    render(
      <FilterMemberOverlay
        filters={{ studyId: 5, gratie: true } as any}
        onFilter={onFilter}
      />,
    );

    await user.click(screen.getByRole("button", { name: "apply_filters" }));

    expect(handleApplyFilters).toHaveBeenCalledWith(
      expect.objectContaining({ onFilter, studyId: 5, gratie: true }),
    );
  });

  it("applies a null studyId when no study is selected", async () => {
    const user = userEvent.setup();
    const onFilter = vi.fn();
    render(<FilterMemberOverlay filters={null} onFilter={onFilter} />);

    await user.click(screen.getByRole("button", { name: "apply_filters" }));

    expect(handleApplyFilters).toHaveBeenCalledWith(
      expect.objectContaining({ studyId: null }),
    );
  });

  it("updates the study type when the select changes", () => {
    render(<FilterMemberOverlay filters={null} onFilter={vi.fn()} />);
    fireEvent.change(screen.getByLabelText("study_type"), {
      target: { value: "Bachelor" },
    });
    expect(screen.getByLabelText("study_type")).toHaveValue("Bachelor");
  });

  it("resets the study type back to null when the select is cleared", () => {
    render(<FilterMemberOverlay filters={null} onFilter={vi.fn()} />);
    const select = screen.getByLabelText("study_type");
    fireEvent.change(select, { target: { value: "Bachelor" } });
    fireEvent.change(select, { target: { value: "" } });
    expect(select).toHaveValue("");
  });

  it("populates and selects a specific study once studies have loaded", async () => {
    vi.mocked(loadStudies).mockImplementation(
      async (_setLoading, setStudies) => {
        setStudies([{ id: 1, title: "Computer Science" } as Study]);
      },
    );
    const onFilter = vi.fn();
    render(<FilterMemberOverlay filters={null} onFilter={onFilter} />);

    const studySelect = await screen.findByRole("option", {
      name: "Computer Science",
    });
    void studySelect;
    fireEvent.change(screen.getByLabelText("study"), {
      target: { value: "1" },
    });

    await userEvent
      .setup()
      .click(screen.getByRole("button", { name: "apply_filters" }));
    expect(handleApplyFilters).toHaveBeenCalledWith(
      expect.objectContaining({ studyId: 1 }),
    );
  });
});
