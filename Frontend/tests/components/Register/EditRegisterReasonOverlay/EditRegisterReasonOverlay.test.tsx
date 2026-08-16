import { fireEvent, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RegisterReasonResponseDto } from "~/api";
import { renderWithProviders } from "~/testUtils";

const { handleReasonSubmit, handleReasonDelete } = vi.hoisted(() => ({
  handleReasonSubmit: vi.fn(),
  handleReasonDelete: vi.fn(),
}));

vi.mock(
  "~/components/Register/EditRegisterReasonOverlay/EditRegisterReasonOverlay.handlers",
  () => ({
    handleReasonSubmit,
    handleReasonDelete,
  }),
);

import EditRegisterReasonOverlay from "~/components/Register/EditRegisterReasonOverlay/EditRegisterReasonOverlay";

// RequiredAsterisk renders a trailing "*" inside the <label>, so an exact-text
// getByLabelText match fails for required fields - use a substring match instead.
function getInput(label: string) {
  return screen.getByLabelText(label, { exact: false });
}

const existingReason: RegisterReasonResponseDto = {
  id: 5,
  titleDutch: "Oud",
  titleEnglish: "Old",
  descriptionDutch: "Oude omschrijving",
  descriptionEnglish: "Old description",
  sortOrder: 2,
  iconPath: null,
} as RegisterReasonResponseDto;

describe("EditRegisterReasonOverlay", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders empty fields and a disabled create button when creating a new reason", () => {
    renderWithProviders(<EditRegisterReasonOverlay onComplete={vi.fn()} />);

    expect(getInput("title_nl")).toHaveValue("");
    expect(screen.getByRole("button", { name: "create" })).toBeDisabled();
    expect(
      screen.queryByRole("button", { name: "delete" }),
    ).not.toBeInTheDocument();
  });

  it("pre-fills the fields and enables save when editing an existing reason", () => {
    renderWithProviders(
      <EditRegisterReasonOverlay
        onComplete={vi.fn()}
        reason={existingReason}
      />,
    );

    expect(getInput("title_nl")).toHaveValue("Oud");
    expect(getInput("title_en")).toHaveValue("Old");
    expect(getInput("description_nl")).toHaveValue("Oude omschrijving");
    expect(screen.getByRole("button", { name: "save" })).toBeEnabled();
    expect(screen.getByRole("button", { name: "delete" })).toBeInTheDocument();
    expect(screen.getByText("leave_empty_to_keep_current")).toBeInTheDocument();
  });

  it("updates form state as the user types and enables the create button once all required fields are filled", async () => {
    const user = userEvent.setup();
    renderWithProviders(<EditRegisterReasonOverlay onComplete={vi.fn()} />);

    await user.type(getInput("title_nl"), "Titel");
    await user.type(getInput("title_en"), "Title");
    await user.type(getInput("description_nl"), "Omschrijving");
    await user.type(getInput("description_en"), "Description");

    expect(screen.getByRole("button", { name: "create" })).toBeEnabled();
  });

  it("calls handleReasonSubmit with the current form data on submit", async () => {
    const onComplete = vi.fn();
    renderWithProviders(
      <EditRegisterReasonOverlay
        onComplete={onComplete}
        reason={existingReason}
      />,
    );

    const form = screen.getByRole("button", { name: "save" }).closest("form");
    expect(form).not.toBeNull();
    fireEvent.submit(form as HTMLFormElement);

    expect(handleReasonSubmit).toHaveBeenCalledTimes(1);
    const callArgs = handleReasonSubmit.mock.calls[0][0];
    expect(callArgs.reason).toBe(existingReason);
    expect(callArgs.formData).toEqual({
      titleDutch: existingReason.titleDutch,
      titleEnglish: existingReason.titleEnglish,
      descriptionDutch: existingReason.descriptionDutch,
      descriptionEnglish: existingReason.descriptionEnglish,
      sortOrder: existingReason.sortOrder,
    });
    expect(callArgs.iconFile).toBeNull();
    expect(callArgs.onComplete).toBe(onComplete);
  });

  it("sets the icon file on the form data when a file is chosen", async () => {
    const user = userEvent.setup();
    renderWithProviders(
      <EditRegisterReasonOverlay
        onComplete={vi.fn()}
        reason={existingReason}
      />,
    );

    const file = new File(["icon"], "icon.png", { type: "image/png" });
    const fileInput = document.querySelector(
      "input[type='file']",
    ) as HTMLInputElement;
    await user.upload(fileInput, file);

    const form = screen.getByRole("button", { name: "save" }).closest("form");
    fireEvent.submit(form as HTMLFormElement);

    const callArgs = handleReasonSubmit.mock.calls[0][0];
    expect(callArgs.iconFile).toBe(file);
  });

  it("calls handleReasonDelete with the reason when the delete button is clicked", async () => {
    const user = userEvent.setup();
    const onComplete = vi.fn();
    renderWithProviders(
      <EditRegisterReasonOverlay
        onComplete={onComplete}
        reason={existingReason}
      />,
    );

    await user.click(screen.getByRole("button", { name: "delete" }));

    expect(handleReasonDelete).toHaveBeenCalledTimes(1);
    const callArgs = handleReasonDelete.mock.calls[0][0];
    expect(callArgs.reason).toBe(existingReason);
    expect(callArgs.onComplete).toBe(onComplete);
  });
});
