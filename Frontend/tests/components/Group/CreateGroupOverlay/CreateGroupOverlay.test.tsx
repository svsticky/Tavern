import { fireEvent, render, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import CreateGroupOverlay from "~/components/Group/CreateGroupOverlay/CreateGroupOverlay";

const { handleCreateGroupSubmit, handleFileChange, resetCreateGroupForm } =
  vi.hoisted(() => ({
    handleCreateGroupSubmit: vi.fn(),
    handleFileChange: vi.fn(),
    resetCreateGroupForm: vi.fn(),
  }));

vi.mock(
  "~/components/Group/CreateGroupOverlay/CreateGroupOverlay.handlers",
  () => ({
    handleCreateGroupSubmit,
    handleFileChange,
    resetCreateGroupForm,
  }),
);

describe("CreateGroupOverlay", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders the upload prompt, name field, and type select", () => {
    render(<CreateGroupOverlay onSuccess={vi.fn()} />);

    expect(screen.getByText("upload_picture")).toBeInTheDocument();
    expect(screen.getByLabelText(/group_name/)).toBeInTheDocument();
    expect(screen.getByLabelText("group_type")).toBeInTheDocument();
  });

  it("disables the create button until a name and picture are provided", () => {
    render(<CreateGroupOverlay onSuccess={vi.fn()} />);
    expect(screen.getByRole("button", { name: "create" })).toBeDisabled();
  });

  it("calls handleFileChange when a picture is chosen", async () => {
    const user = userEvent.setup();
    render(<CreateGroupOverlay onSuccess={vi.fn()} />);

    const fileInput = document.querySelector(
      "input[type='file']",
    ) as HTMLInputElement;
    const file = new File(["data"], "group.png", { type: "image/png" });
    await user.upload(fileInput, file);

    expect(handleFileChange).toHaveBeenCalledTimes(1);
  });

  it("calls handleCreateGroupSubmit on form submission", () => {
    render(<CreateGroupOverlay onSuccess={vi.fn()} />);

    const form = document.querySelector("form") as HTMLFormElement;
    form.dispatchEvent(
      new Event("submit", { bubbles: true, cancelable: true }),
    );

    expect(handleCreateGroupSubmit).toHaveBeenCalledTimes(1);
  });

  it("shows a preview image once a picture has been selected", async () => {
    handleFileChange.mockImplementation(
      (
        _e: unknown,
        _formData: unknown,
        setFormData: any,
        setImagePreview: any,
      ) => {
        setFormData((prev: any) => ({
          ...prev,
          groupPicture: new File(["data"], "group.png"),
        }));
        setImagePreview("blob:preview-url");
      },
    );
    const user = userEvent.setup();
    render(<CreateGroupOverlay onSuccess={vi.fn()} />);

    const fileInput = document.querySelector(
      "input[type='file']",
    ) as HTMLInputElement;
    const file = new File(["data"], "group.png", { type: "image/png" });
    await user.upload(fileInput, file);

    expect(await screen.findByAltText("Preview")).toHaveAttribute(
      "src",
      "blob:preview-url",
    );
    expect(screen.queryByText("upload_picture")).not.toBeInTheDocument();
  });

  it("updates the group name and type as the user types/selects", () => {
    render(<CreateGroupOverlay onSuccess={vi.fn()} />);

    fireEvent.change(screen.getByLabelText(/group_name/), {
      target: { value: "Party Committee" },
    });
    expect(screen.getByLabelText(/group_name/)).toHaveValue("Party Committee");

    fireEvent.change(screen.getByLabelText("group_type"), {
      target: { value: "WorkingGroup" },
    });
    expect(screen.getByLabelText("group_type")).toHaveValue("WorkingGroup");
  });

  it("resets the form via resetCreateGroupForm when the submit handler calls resetForm", () => {
    handleCreateGroupSubmit.mockImplementation(({ resetForm }: any) => {
      resetForm();
    });
    render(<CreateGroupOverlay onSuccess={vi.fn()} />);

    const form = document.querySelector("form") as HTMLFormElement;
    form.dispatchEvent(
      new Event("submit", { bubbles: true, cancelable: true }),
    );

    expect(resetCreateGroupForm).toHaveBeenCalledWith(
      expect.any(Function),
      expect.any(Function),
    );
  });

  it("opens the file picker when the upload area is clicked", () => {
    render(<CreateGroupOverlay onSuccess={vi.fn()} />);

    const fileInput = document.querySelector(
      "input[type='file']",
    ) as HTMLInputElement;
    const clickSpy = vi.spyOn(fileInput, "click");

    fireEvent.click(screen.getByText("upload_picture").parentElement!);

    expect(clickSpy).toHaveBeenCalled();
  });
});
