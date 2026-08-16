import { fireEvent, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { ExternalLinkResponseDto } from "~/api";
import { renderWithProviders } from "~/testUtils";

const { handleLinkSubmit, handleLinkDelete } = vi.hoisted(() => ({
  handleLinkSubmit: vi.fn(),
  handleLinkDelete: vi.fn(),
}));

vi.mock(
  "~/components/Admin/EditExternalLinkOverlay/EditExternalLinkOverlay.handlers",
  () => ({
    handleLinkSubmit,
    handleLinkDelete,
  }),
);

import EditExternalLinkOverlay from "~/components/Admin/EditExternalLinkOverlay/EditExternalLinkOverlay";

const existingLink: ExternalLinkResponseDto = {
  id: 5,
  titleDutch: "Oud",
  titleEnglish: "Old",
  descriptionDutch: "Oude omschrijving",
  descriptionEnglish: "Old description",
  url: "https://old.example.com",
  sortOrder: 2,
  iconPath: null,
} as ExternalLinkResponseDto;

describe("EditExternalLinkOverlay", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders empty fields and a disabled create button when creating a new link", () => {
    renderWithProviders(<EditExternalLinkOverlay onComplete={vi.fn()} />);

    expect(screen.getByLabelText(/title_nl/)).toHaveValue("");
    expect(screen.getByLabelText(/url/)).toHaveValue("");
    expect(screen.getByRole("button", { name: "create" })).toBeDisabled();
    expect(
      screen.queryByRole("button", { name: "delete" }),
    ).not.toBeInTheDocument();
  });

  it("pre-fills the fields and enables save when editing an existing link", () => {
    renderWithProviders(
      <EditExternalLinkOverlay onComplete={vi.fn()} link={existingLink} />,
    );

    expect(screen.getByLabelText(/title_nl/)).toHaveValue("Oud");
    expect(screen.getByLabelText(/title_en/)).toHaveValue("Old");
    expect(screen.getByLabelText(/url/)).toHaveValue("https://old.example.com");
    expect(screen.getByRole("button", { name: "save" })).toBeEnabled();
    expect(screen.getByRole("button", { name: "delete" })).toBeInTheDocument();
    expect(screen.getByText("leave_empty_to_keep_current")).toBeInTheDocument();
  });

  it("updates form state as the user types and enables the create button once all required fields are filled", async () => {
    const user = userEvent.setup();
    renderWithProviders(<EditExternalLinkOverlay onComplete={vi.fn()} />);

    await user.type(screen.getByLabelText(/title_nl/), "Titel");
    await user.type(screen.getByLabelText(/title_en/), "Title");
    await user.type(screen.getByLabelText(/description_nl/), "Omschrijving");
    await user.type(screen.getByLabelText(/description_en/), "Description");
    await user.type(screen.getByLabelText(/url/), "https://example.com");

    expect(screen.getByRole("button", { name: "create" })).toBeEnabled();
  });

  it("calls handleLinkSubmit with the current form data on submit", async () => {
    const onComplete = vi.fn();
    renderWithProviders(
      <EditExternalLinkOverlay onComplete={onComplete} link={existingLink} />,
    );

    const form = screen.getByRole("button", { name: "save" }).closest("form");
    expect(form).not.toBeNull();
    fireEvent.submit(form as HTMLFormElement);

    expect(handleLinkSubmit).toHaveBeenCalledTimes(1);
    const callArgs = handleLinkSubmit.mock.calls[0][0];
    expect(callArgs.link).toBe(existingLink);
    expect(callArgs.formData).toEqual({
      titleDutch: existingLink.titleDutch,
      titleEnglish: existingLink.titleEnglish,
      descriptionDutch: existingLink.descriptionDutch,
      descriptionEnglish: existingLink.descriptionEnglish,
      url: existingLink.url,
      sortOrder: existingLink.sortOrder,
    });
    expect(callArgs.iconFile).toBeNull();
    expect(callArgs.onComplete).toBe(onComplete);
  });

  it("sets the icon file on the form data when a file is chosen", async () => {
    const user = userEvent.setup();
    renderWithProviders(
      <EditExternalLinkOverlay onComplete={vi.fn()} link={existingLink} />,
    );

    const file = new File(["icon"], "icon.png", { type: "image/png" });
    const fileInput = document.querySelector(
      "input[type='file']",
    ) as HTMLInputElement;
    await user.upload(fileInput, file);

    const form = screen.getByRole("button", { name: "save" }).closest("form");
    fireEvent.submit(form as HTMLFormElement);

    const callArgs = handleLinkSubmit.mock.calls[0][0];
    expect(callArgs.iconFile).toBe(file);
  });

  it("calls handleLinkDelete with the link when the delete button is clicked", async () => {
    const user = userEvent.setup();
    const onComplete = vi.fn();
    renderWithProviders(
      <EditExternalLinkOverlay onComplete={onComplete} link={existingLink} />,
    );

    await user.click(screen.getByRole("button", { name: "delete" }));

    expect(handleLinkDelete).toHaveBeenCalledTimes(1);
    const callArgs = handleLinkDelete.mock.calls[0][0];
    expect(callArgs.link).toBe(existingLink);
    expect(callArgs.onComplete).toBe(onComplete);
  });
});
