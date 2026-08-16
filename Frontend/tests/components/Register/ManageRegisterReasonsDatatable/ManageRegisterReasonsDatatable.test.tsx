import { fireEvent, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RegisterReasonResponseDto } from "~/api";
import { renderWithProviders } from "~/testUtils";

const { getRegisterreasons, putRegisterreasonsById } = vi.hoisted(() => ({
  getRegisterreasons: vi.fn(),
  putRegisterreasonsById: vi.fn(),
}));

vi.mock("~/api", () => ({
  getRegisterreasons,
  putRegisterreasonsById,
  // Also used transitively by EditRegisterReasonOverlay.handlers, which this
  // component renders inside its create/edit Modal.
  postRegisterreasons: vi.fn(),
  postRegisterreasonsByIdIcon: vi.fn(),
  deleteRegisterreasonsById: vi.fn(),
}));

vi.mock("react-hot-toast", () => ({
  default: {
    promise: vi.fn((p: Promise<unknown>) => p.catch(() => {})),
  },
}));

import ManageRegisterReasonsDatatable from "~/components/Register/ManageRegisterReasonsDatatable/ManageRegisterReasonsDatatable";

function makeReasons(): RegisterReasonResponseDto[] {
  return [
    {
      id: 1,
      titleDutch: "Reden Een",
      titleEnglish: "Reason One",
      descriptionDutch: "Omschrijving een",
      descriptionEnglish: "Description one",
      sortOrder: 1,
      iconPath: null,
    },
    {
      id: 2,
      titleDutch: "Reden Twee",
      titleEnglish: "Reason Two",
      descriptionDutch: "Omschrijving twee",
      descriptionEnglish: "Description two",
      sortOrder: 2,
      iconPath: "icon.png",
    },
  ] as RegisterReasonResponseDto[];
}

describe("ManageRegisterReasonsDatatable", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows a loading state, then renders fetched reasons sorted by sortOrder", async () => {
    getRegisterreasons.mockResolvedValue({ data: makeReasons().reverse() });

    renderWithProviders(<ManageRegisterReasonsDatatable />);

    expect(screen.getByText("loading")).toBeInTheDocument();

    await waitFor(() =>
      expect(screen.getByText("Reason One")).toBeInTheDocument(),
    );
    expect(screen.getByText("Reason Two")).toBeInTheDocument();

    const headings = screen.getAllByRole("heading", { level: 4 });
    expect(headings[0]).toHaveTextContent("Reason One");
    expect(headings[1]).toHaveTextContent("Reason Two");
  });

  it("shows the empty state when there are no reasons", async () => {
    getRegisterreasons.mockResolvedValue({ data: [] });

    renderWithProviders(<ManageRegisterReasonsDatatable />);

    await waitFor(() =>
      expect(screen.getByText("no_reasons")).toBeInTheDocument(),
    );
  });

  it("renders a custom icon image for reasons with an iconPath", async () => {
    getRegisterreasons.mockResolvedValue({ data: makeReasons() });

    renderWithProviders(<ManageRegisterReasonsDatatable />);

    await waitFor(() =>
      expect(screen.getByText("Reason Two")).toBeInTheDocument(),
    );

    // alt="" gives these images an implicit "presentation" role, so query by tag instead of role.
    const images = Array.from(document.querySelectorAll("img"));
    expect(
      images.some((img) => img.getAttribute("src")?.includes("/2/icon")),
    ).toBe(true);
  });

  it("opens the create modal with an empty form when add_reason is clicked", async () => {
    const user = userEvent.setup();
    getRegisterreasons.mockResolvedValue({ data: [] });

    renderWithProviders(<ManageRegisterReasonsDatatable />);
    await waitFor(() =>
      expect(screen.getByText("no_reasons")).toBeInTheDocument(),
    );

    await user.click(screen.getByRole("button", { name: "add_reason" }));

    expect(
      screen.getByRole("heading", { name: "add_reason" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "create" })).toBeInTheDocument();
  });

  it("opens the edit modal pre-filled when an item's edit button is clicked", async () => {
    const user = userEvent.setup();
    getRegisterreasons.mockResolvedValue({ data: makeReasons() });

    renderWithProviders(<ManageRegisterReasonsDatatable />);
    await waitFor(() =>
      expect(screen.getByText("Reason One")).toBeInTheDocument(),
    );

    await user.click(screen.getAllByRole("button", { name: "edit" })[0]);

    expect(
      screen.getByRole("heading", { name: "edit_reason" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "save" })).toBeInTheDocument();
  });

  it("persists the new order and refetches after a drag-and-drop reorder", async () => {
    getRegisterreasons.mockResolvedValue({ data: makeReasons() });
    putRegisterreasonsById.mockResolvedValue({});

    renderWithProviders(<ManageRegisterReasonsDatatable />);
    await waitFor(() =>
      expect(screen.getByText("Reason One")).toBeInTheDocument(),
    );

    const items = screen.getAllByRole("heading", { level: 4 });
    const firstRow = items[0].closest("[draggable]") as HTMLElement;
    const secondRow = items[1].closest("[draggable]") as HTMLElement;

    fireEvent.dragStart(firstRow, { dataTransfer: {} });
    fireEvent.dragEnter(secondRow, { dataTransfer: {} });
    fireEvent.dragEnd(firstRow, { dataTransfer: {} });

    await waitFor(() =>
      expect(putRegisterreasonsById).toHaveBeenCalledTimes(2),
    );
    // Reason 2 (id 2) should now be first (sortOrder 1) after reordering.
    expect(putRegisterreasonsById).toHaveBeenCalledWith({
      path: { id: 2 },
      body: expect.objectContaining({ sortOrder: 1 }),
    });
    expect(putRegisterreasonsById).toHaveBeenCalledWith({
      path: { id: 1 },
      body: expect.objectContaining({ sortOrder: 2 }),
    });

    await waitFor(() => expect(getRegisterreasons).toHaveBeenCalledTimes(2));
  });

  it("logs an error when fetching reasons fails", async () => {
    getRegisterreasons.mockRejectedValue(new Error("boom"));
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    renderWithProviders(<ManageRegisterReasonsDatatable />);

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("ignores a dragEnter that fires without a preceding dragStart", async () => {
    getRegisterreasons.mockResolvedValue({ data: makeReasons() });

    renderWithProviders(<ManageRegisterReasonsDatatable />);
    await waitFor(() =>
      expect(screen.getByText("Reason One")).toBeInTheDocument(),
    );

    const rows = document.querySelectorAll("[draggable]");
    fireEvent.dragEnter(rows[1], { dataTransfer: {} });

    expect(putRegisterreasonsById).not.toHaveBeenCalled();
  });

  it("prevents the default browser behavior on dragOver", async () => {
    getRegisterreasons.mockResolvedValue({ data: makeReasons() });

    renderWithProviders(<ManageRegisterReasonsDatatable />);
    await waitFor(() =>
      expect(screen.getByText("Reason One")).toBeInTheDocument(),
    );

    const row = document.querySelectorAll("[draggable]")[0];
    const event = new Event("dragover", { bubbles: true, cancelable: true });
    row.dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
  });

  it("logs an error when persisting the reordered reasons fails", async () => {
    getRegisterreasons.mockResolvedValue({ data: makeReasons() });
    putRegisterreasonsById.mockRejectedValue(new Error("boom"));
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    renderWithProviders(<ManageRegisterReasonsDatatable />);
    await waitFor(() =>
      expect(screen.getByText("Reason One")).toBeInTheDocument(),
    );

    const rows = document.querySelectorAll("[draggable]");
    fireEvent.dragStart(rows[0], { dataTransfer: {} });
    fireEvent.dragEnter(rows[1], { dataTransfer: {} });
    fireEvent.dragEnd(rows[0], { dataTransfer: {} });

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("closes the modal and refetches once the overlay reports completion", async () => {
    vi.doMock(
      "~/components/Register/EditRegisterReasonOverlay/EditRegisterReasonOverlay",
      () => ({
        default: ({ onComplete }: { onComplete: () => void }) => (
          <button type="button" onClick={onComplete}>
            complete-reason
          </button>
        ),
      }),
    );
    vi.resetModules();
    const { default: FreshDatatable } = await import(
      "~/components/Register/ManageRegisterReasonsDatatable/ManageRegisterReasonsDatatable"
    );
    getRegisterreasons.mockResolvedValue({ data: [] });

    renderWithProviders(<FreshDatatable />);
    await waitFor(() =>
      expect(screen.getByText("no_reasons")).toBeInTheDocument(),
    );

    fireEvent.click(screen.getByRole("button", { name: "add_reason" }));
    fireEvent.click(await screen.findByText("complete-reason"));

    await waitFor(() =>
      expect(screen.queryByText("complete-reason")).not.toBeInTheDocument(),
    );
    expect(getRegisterreasons).toHaveBeenCalledTimes(2);
    vi.doUnmock("../EditRegisterReasonOverlay/EditRegisterReasonOverlay");
  });
});
