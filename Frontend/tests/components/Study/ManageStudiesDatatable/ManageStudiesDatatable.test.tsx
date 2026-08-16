import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Study } from "~/api";
import ManageStudiesDatatable from "~/components/Study/ManageStudiesDatatable/ManageStudiesDatatable";
import {
  fetchStudies,
  handleStudyEdited,
} from "~/components/Study/ManageStudiesDatatable/ManageStudiesDatatable.handlers";

vi.mock(
  "~/components/Study/ManageStudiesDatatable/ManageStudiesDatatable.handlers",
  () => ({
    fetchStudies: vi.fn(),
    handleStudyEdited: vi.fn(),
  }),
);

vi.mock("~/components/Study/EditStudyOverlay/EditStudyOverlay", () => ({
  default: ({ onStudyAdded }: { onStudyAdded: (s?: Study) => void }) => (
    <button type="button" onClick={() => onStudyAdded({ id: 9 } as Study)}>
      complete-study-edit
    </button>
  ),
}));

describe("ManageStudiesDatatable", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("calls fetchStudies on mount", () => {
    render(<ManageStudiesDatatable />);
    expect(fetchStudies).toHaveBeenCalled();
  });

  it("renders studies passed via setStudies", async () => {
    vi.mocked(fetchStudies).mockImplementation(
      async (setLoading, setStudies) => {
        setStudies([
          { id: 1, title: "CS", type: "Bachelor", nominalDurationYears: 3 },
        ] as Study[]);
        setLoading(false);
      },
    );
    render(<ManageStudiesDatatable />);
    expect(await screen.findByText("CS")).toBeInTheDocument();
  });

  it("opens the add-study modal and forwards completion to handleStudyEdited", () => {
    render(<ManageStudiesDatatable />);
    fireEvent.click(screen.getAllByText("add_study")[0]);
    fireEvent.click(screen.getByText("complete-study-edit"));

    expect(handleStudyEdited).toHaveBeenCalledWith(
      expect.objectContaining({ study: { id: 9 } }),
    );
  });

  it("closes the modal without saving when dismissed", () => {
    render(<ManageStudiesDatatable />);
    fireEvent.click(screen.getAllByText("add_study")[0]);
    expect(screen.getByText("complete-study-edit")).toBeInTheDocument();

    fireEvent.keyDown(window, { key: "Escape" });

    expect(screen.queryByText("complete-study-edit")).not.toBeInTheDocument();
  });

  it("opens the edit modal for an existing study", async () => {
    vi.mocked(fetchStudies).mockImplementation(
      async (setLoading, setStudies) => {
        setStudies([
          { id: 1, title: "CS", type: "Bachelor", nominalDurationYears: 3 },
        ] as Study[]);
        setLoading(false);
      },
    );
    render(<ManageStudiesDatatable />);

    fireEvent.click((await screen.findAllByText("edit"))[0]);
    fireEvent.click(screen.getByText("complete-study-edit"));

    expect(handleStudyEdited).toHaveBeenCalledWith(
      expect.objectContaining({
        editedStudy: expect.objectContaining({ id: 1 }),
      }),
    );
  });
});
