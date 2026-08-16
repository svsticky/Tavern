import { fireEvent, render, screen } from "@testing-library/react";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Study } from "~/api";
import EditStudyOverlay from "~/components/Study/EditStudyOverlay/EditStudyOverlay";
import {
  handleStudyDelete,
  handleStudySubmit,
} from "~/components/Study/EditStudyOverlay/EditStudyOverlay.handlers";

vi.mock(
  "~/components/Study/EditStudyOverlay/EditStudyOverlay.handlers",
  () => ({
    handleStudySubmit: vi.fn(),
    handleStudyDelete: vi.fn(),
  }),
);

describe("EditStudyOverlay", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders empty inputs and a 'create' button in create mode", () => {
    render(<EditStudyOverlay onStudyAdded={vi.fn()} />);
    expect(screen.getByLabelText(/^name/)).toHaveValue("");
    expect(screen.getByText("create")).toBeInTheDocument();
    expect(screen.queryByText("delete")).not.toBeInTheDocument();
  });

  it("pre-fills the form and shows 'save'/'delete' buttons in edit mode", () => {
    const study = {
      id: 1,
      title: "Computer Science",
      type: "Master",
      nominalDurationYears: 2,
    } as Study;
    render(<EditStudyOverlay onStudyAdded={vi.fn()} study={study} />);

    expect(screen.getByLabelText(/^name/)).toHaveValue("Computer Science");
    expect(screen.getByText("save")).toBeInTheDocument();
    expect(screen.getByText("delete")).toBeInTheDocument();
  });

  it("disables submit until required fields are filled", () => {
    render(<EditStudyOverlay onStudyAdded={vi.fn()} />);
    expect(screen.getByText("create")).toBeDisabled();

    fireEvent.change(screen.getByLabelText(/^name/), {
      target: { value: "CS" },
    });
    fireEvent.change(screen.getByLabelText(/^nominal_duration/), {
      target: { value: "3" },
    });
    expect(screen.getByText("create")).not.toBeDisabled();
  });

  it("updates the study type when the select changes", () => {
    render(<EditStudyOverlay onStudyAdded={vi.fn()} />);
    fireEvent.change(screen.getByLabelText(/^study_type/), {
      target: { value: "Master" },
    });
    expect(screen.getByLabelText(/^study_type/)).toHaveValue("Master");
  });

  it("calls handleStudySubmit on form submission", () => {
    render(<EditStudyOverlay onStudyAdded={vi.fn()} />);
    fireEvent.change(screen.getByLabelText(/^name/), {
      target: { value: "CS" },
    });
    fireEvent.change(screen.getByLabelText(/^nominal_duration/), {
      target: { value: "3" },
    });
    fireEvent.click(screen.getByText("create"));

    expect(handleStudySubmit).toHaveBeenCalledWith(
      expect.objectContaining({
        formData: expect.objectContaining({
          title: "CS",
          nominalDurationYears: 3,
        }),
      }),
    );
  });

  it("calls handleStudyDelete when the delete button is clicked", () => {
    const study = {
      id: 1,
      title: "CS",
      type: "Bachelor",
      nominalDurationYears: 3,
    } as Study;
    render(<EditStudyOverlay onStudyAdded={vi.fn()} study={study} />);
    fireEvent.click(screen.getByText("delete"));

    expect(handleStudyDelete).toHaveBeenCalledWith(
      expect.objectContaining({ study }),
    );
  });
});
