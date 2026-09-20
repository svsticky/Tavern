import { fireEvent, screen } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RegistrationDocumentResponseDto } from "~/api";
import { renderWithProviders } from "~/testUtils";

const { handleDocumentSubmit, handleDocumentDelete } = vi.hoisted(() => ({
  handleDocumentSubmit: vi.fn(),
  handleDocumentDelete: vi.fn(),
}));

vi.mock(
  "~/components/Register/EditRegistrationDocumentOverlay/EditRegistrationDocumentOverlay.handlers",
  () => ({
    handleDocumentSubmit,
    handleDocumentDelete,
  }),
);

import EditRegistrationDocumentOverlay from "~/components/Register/EditRegistrationDocumentOverlay/EditRegistrationDocumentOverlay";

// RequiredAsterisk renders a trailing "*" inside the <label>, breaking exact-text
// getByLabelText matches for required fields - use a substring match instead.
function getInput(label: string) {
  return screen.getByLabelText(label, { exact: false });
}

const existingDocument: RegistrationDocumentResponseDto = {
  id: 5,
  nameDutch: "Oud",
  nameEnglish: "Old",
  urlDutch: "https://old.example.com/nl/doc.pdf",
  urlEnglish: "https://old.example.com/en/doc.pdf",
  sortOrder: 2,
} as RegistrationDocumentResponseDto;

describe("EditRegistrationDocumentOverlay", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("renders empty fields and no delete button when creating a new document", () => {
    renderWithProviders(
      <EditRegistrationDocumentOverlay onComplete={vi.fn()} />,
    );

    expect(getInput("title_nl")).toHaveValue("");
    expect(getInput("url_nl")).toHaveValue("");
    expect(getInput("url_en")).toHaveValue("");
    expect(screen.queryByLabelText("sort_order")).not.toBeInTheDocument();
    expect(screen.getByRole("button", { name: "create" })).toBeInTheDocument();
    expect(
      screen.queryByRole("button", { name: "delete" }),
    ).not.toBeInTheDocument();
  });

  it("pre-fills the fields and shows delete/update when editing an existing document", () => {
    renderWithProviders(
      <EditRegistrationDocumentOverlay
        onComplete={vi.fn()}
        document={existingDocument}
      />,
    );

    expect(getInput("title_nl")).toHaveValue("Oud");
    expect(getInput("title_en")).toHaveValue("Old");
    expect(getInput("url_nl")).toHaveValue(existingDocument.urlDutch);
    expect(getInput("url_en")).toHaveValue(existingDocument.urlEnglish);
    expect(screen.getByRole("button", { name: "update" })).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "delete" })).toBeInTheDocument();
  });

  it("updates text fields as the user types", async () => {
    const user = userEvent.setup();
    renderWithProviders(
      <EditRegistrationDocumentOverlay onComplete={vi.fn()} />,
    );

    await user.type(getInput("title_nl"), "Naam");

    expect(getInput("title_nl")).toHaveValue("Naam");
  });

  it("calls handleDocumentSubmit with the current form data on submit", () => {
    const onComplete = vi.fn();
    renderWithProviders(
      <EditRegistrationDocumentOverlay
        onComplete={onComplete}
        document={existingDocument}
      />,
    );

    const form = screen.getByRole("button", { name: "update" }).closest("form");
    expect(form).not.toBeNull();
    fireEvent.submit(form as HTMLFormElement);

    expect(handleDocumentSubmit).toHaveBeenCalledTimes(1);
    const callArgs = handleDocumentSubmit.mock.calls[0][0];
    expect(callArgs.document).toBe(existingDocument);
    expect(callArgs.formData).toEqual({
      nameDutch: existingDocument.nameDutch,
      nameEnglish: existingDocument.nameEnglish,
      urlDutch: existingDocument.urlDutch,
      urlEnglish: existingDocument.urlEnglish,
      sortOrder: existingDocument.sortOrder,
    });
    expect(callArgs.onComplete).toBe(onComplete);
  });

  it("calls handleDocumentDelete with the document when the delete button is clicked", async () => {
    const user = userEvent.setup();
    const onComplete = vi.fn();
    renderWithProviders(
      <EditRegistrationDocumentOverlay
        onComplete={onComplete}
        document={existingDocument}
      />,
    );

    await user.click(screen.getByRole("button", { name: "delete" }));

    expect(handleDocumentDelete).toHaveBeenCalledTimes(1);
    const callArgs = handleDocumentDelete.mock.calls[0][0];
    expect(callArgs.document).toBe(existingDocument);
    expect(callArgs.onComplete).toBe(onComplete);
  });

  it("calls onComplete directly when the cancel button is clicked", async () => {
    const user = userEvent.setup();
    const onComplete = vi.fn();
    renderWithProviders(
      <EditRegistrationDocumentOverlay onComplete={onComplete} />,
    );

    await user.click(screen.getByRole("button", { name: "cancel" }));

    expect(onComplete).toHaveBeenCalledTimes(1);
  });
});
