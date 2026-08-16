import { fireEvent, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RegistrationDocumentResponseDto } from "~/api";
import { renderWithProviders } from "~/testUtils";

const { getRegistrationdocuments, putRegistrationdocumentsById } = vi.hoisted(
  () => ({
    getRegistrationdocuments: vi.fn(),
    putRegistrationdocumentsById: vi.fn(),
  }),
);

vi.mock("~/api", () => ({
  getRegistrationdocuments,
  putRegistrationdocumentsById,
  // Also used transitively by EditRegistrationDocumentOverlay.handlers, which this
  // component renders inside its create/edit Modal.
  postRegistrationdocuments: vi.fn(),
  deleteRegistrationdocumentsById: vi.fn(),
}));

vi.mock("react-hot-toast", () => ({
  default: {
    promise: vi.fn((p: Promise<unknown>) => p.catch(() => {})),
  },
}));

import ManageRegistrationDocumentsDatatable from "~/components/Register/ManageRegistrationDocumentsDatatable/ManageRegistrationDocumentsDatatable";

function makeDocuments(): RegistrationDocumentResponseDto[] {
  return [
    {
      id: 1,
      nameDutch: "Doc Een",
      nameEnglish: "Doc One",
      url: "https://example.com/one.pdf",
      sortOrder: 1,
    },
    {
      id: 2,
      nameDutch: "Doc Twee",
      nameEnglish: "Doc Two",
      url: "https://example.com/two.pdf",
      sortOrder: 2,
    },
  ] as RegistrationDocumentResponseDto[];
}

describe("ManageRegistrationDocumentsDatatable", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows a loading state, then renders fetched documents sorted by sortOrder", async () => {
    getRegistrationdocuments.mockResolvedValue({
      data: makeDocuments().reverse(),
    });

    renderWithProviders(<ManageRegistrationDocumentsDatatable />);

    expect(screen.getByText("loading")).toBeInTheDocument();

    await waitFor(() =>
      expect(screen.getByText("Doc One")).toBeInTheDocument(),
    );
    const headings = screen.getAllByRole("heading", { level: 4 });
    expect(headings[0]).toHaveTextContent("Doc One");
    expect(headings[1]).toHaveTextContent("Doc Two");
    expect(
      screen.getByRole("link", { name: "https://example.com/one.pdf" }),
    ).toHaveAttribute("href", "https://example.com/one.pdf");
  });

  it("shows the empty state when there are no documents", async () => {
    getRegistrationdocuments.mockResolvedValue({ data: [] });

    renderWithProviders(<ManageRegistrationDocumentsDatatable />);

    await waitFor(() =>
      expect(screen.getByText("no_documents")).toBeInTheDocument(),
    );
  });

  it("opens the create modal when add_document is clicked", async () => {
    const user = userEvent.setup();
    getRegistrationdocuments.mockResolvedValue({ data: [] });

    renderWithProviders(<ManageRegistrationDocumentsDatatable />);
    await waitFor(() =>
      expect(screen.getByText("no_documents")).toBeInTheDocument(),
    );

    await user.click(screen.getByRole("button", { name: "add_document" }));

    expect(
      screen.getByRole("heading", { name: "add_document" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "create" })).toBeInTheDocument();
  });

  it("opens the edit modal pre-filled for the clicked document", async () => {
    const user = userEvent.setup();
    getRegistrationdocuments.mockResolvedValue({ data: makeDocuments() });

    renderWithProviders(<ManageRegistrationDocumentsDatatable />);
    await waitFor(() =>
      expect(screen.getByText("Doc One")).toBeInTheDocument(),
    );

    await user.click(screen.getAllByRole("button", { name: "edit" })[0]);

    expect(
      screen.getByRole("heading", { name: "edit_document" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "update" })).toBeInTheDocument();
  });

  it("persists the new order and refetches after a drag-and-drop reorder", async () => {
    getRegistrationdocuments.mockResolvedValue({ data: makeDocuments() });
    putRegistrationdocumentsById.mockResolvedValue({});

    renderWithProviders(<ManageRegistrationDocumentsDatatable />);
    await waitFor(() =>
      expect(screen.getByText("Doc One")).toBeInTheDocument(),
    );

    const rows = document.querySelectorAll("[draggable]");
    const firstRow = rows[0] as HTMLElement;
    const secondRow = rows[1] as HTMLElement;

    fireEvent.dragStart(firstRow, { dataTransfer: {} });
    fireEvent.dragEnter(secondRow, { dataTransfer: {} });
    fireEvent.dragEnd(firstRow, { dataTransfer: {} });

    await waitFor(() =>
      expect(putRegistrationdocumentsById).toHaveBeenCalledTimes(2),
    );
    expect(putRegistrationdocumentsById).toHaveBeenCalledWith({
      path: { id: 2 },
      body: expect.objectContaining({ sortOrder: 1 }),
    });
    expect(putRegistrationdocumentsById).toHaveBeenCalledWith({
      path: { id: 1 },
      body: expect.objectContaining({ sortOrder: 2 }),
    });

    await waitFor(() =>
      expect(getRegistrationdocuments).toHaveBeenCalledTimes(2),
    );
  });

  it("logs an error when fetching documents fails", async () => {
    getRegistrationdocuments.mockRejectedValue(new Error("boom"));
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    renderWithProviders(<ManageRegistrationDocumentsDatatable />);

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("ignores a dragEnter that fires without a preceding dragStart", async () => {
    getRegistrationdocuments.mockResolvedValue({ data: makeDocuments() });

    renderWithProviders(<ManageRegistrationDocumentsDatatable />);
    await waitFor(() =>
      expect(screen.getByText("Doc One")).toBeInTheDocument(),
    );

    const rows = document.querySelectorAll("[draggable]");
    fireEvent.dragEnter(rows[1], { dataTransfer: {} });

    expect(putRegistrationdocumentsById).not.toHaveBeenCalled();
  });

  it("prevents the default browser behavior on dragOver", async () => {
    getRegistrationdocuments.mockResolvedValue({ data: makeDocuments() });

    renderWithProviders(<ManageRegistrationDocumentsDatatable />);
    await waitFor(() =>
      expect(screen.getByText("Doc One")).toBeInTheDocument(),
    );

    const row = document.querySelectorAll("[draggable]")[0];
    const event = new Event("dragover", { bubbles: true, cancelable: true });
    row.dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
  });

  it("logs an error when persisting the reordered documents fails", async () => {
    getRegistrationdocuments.mockResolvedValue({ data: makeDocuments() });
    putRegistrationdocumentsById.mockRejectedValue(new Error("boom"));
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    renderWithProviders(<ManageRegistrationDocumentsDatatable />);
    await waitFor(() =>
      expect(screen.getByText("Doc One")).toBeInTheDocument(),
    );

    const rows = document.querySelectorAll("[draggable]");
    fireEvent.dragStart(rows[0], { dataTransfer: {} });
    fireEvent.dragEnter(rows[1], { dataTransfer: {} });
    fireEvent.dragEnd(rows[0], { dataTransfer: {} });

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("stops the click on the document link from bubbling to the row", async () => {
    getRegistrationdocuments.mockResolvedValue({ data: makeDocuments() });

    renderWithProviders(<ManageRegistrationDocumentsDatatable />);
    await waitFor(() =>
      expect(screen.getByText("Doc One")).toBeInTheDocument(),
    );

    const link = screen.getByRole("link", {
      name: "https://example.com/one.pdf",
    });
    fireEvent.click(link);

    expect(
      screen.queryByRole("heading", { name: "edit_document" }),
    ).not.toBeInTheDocument();
  });

  it("closes the edit modal without saving when dismissed", async () => {
    const user = userEvent.setup();
    getRegistrationdocuments.mockResolvedValue({ data: makeDocuments() });

    renderWithProviders(<ManageRegistrationDocumentsDatatable />);
    await waitFor(() =>
      expect(screen.getByText("Doc One")).toBeInTheDocument(),
    );

    await user.click(screen.getAllByRole("button", { name: "edit" })[0]);
    expect(
      screen.getByRole("heading", { name: "edit_document" }),
    ).toBeInTheDocument();

    fireEvent.keyDown(window, { key: "Escape" });

    await waitFor(() =>
      expect(
        screen.queryByRole("heading", { name: "edit_document" }),
      ).not.toBeInTheDocument(),
    );
  });

  it("closes the modal and refetches once the overlay reports completion", async () => {
    vi.doMock(
      "~/components/Register/EditRegistrationDocumentOverlay/EditRegistrationDocumentOverlay",
      () => ({
        default: ({ onComplete }: { onComplete: () => void }) => (
          <button type="button" onClick={onComplete}>
            complete-document
          </button>
        ),
      }),
    );
    vi.resetModules();
    const { default: FreshDatatable } = await import(
      "~/components/Register/ManageRegistrationDocumentsDatatable/ManageRegistrationDocumentsDatatable"
    );
    getRegistrationdocuments.mockResolvedValue({ data: [] });

    renderWithProviders(<FreshDatatable />);
    await waitFor(() =>
      expect(screen.getByText("no_documents")).toBeInTheDocument(),
    );

    fireEvent.click(screen.getByRole("button", { name: "add_document" }));
    fireEvent.click(await screen.findByText("complete-document"));

    await waitFor(() =>
      expect(screen.queryByText("complete-document")).not.toBeInTheDocument(),
    );
    expect(getRegistrationdocuments).toHaveBeenCalledTimes(2);
    vi.doUnmock(
      "../EditRegistrationDocumentOverlay/EditRegistrationDocumentOverlay",
    );
  });
});
