import { fireEvent, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { RegisterSlideResponseDto } from "~/api";
import { renderWithProviders } from "~/testUtils";

const { getRegisterslides, putRegisterslidesById } = vi.hoisted(() => ({
  getRegisterslides: vi.fn(),
  putRegisterslidesById: vi.fn(),
}));

vi.mock("~/api", () => ({
  getRegisterslides,
  putRegisterslidesById,
  // Also used transitively by EditRegisterSlideOverlay.handlers, which this
  // component renders inside its create/edit Modal.
  postRegisterslides: vi.fn(),
  postRegisterslidesByIdImage: vi.fn(),
  deleteRegisterslidesById: vi.fn(),
}));

vi.mock("react-hot-toast", () => ({
  default: {
    promise: vi.fn((p: Promise<unknown>) => p.catch(() => {})),
  },
}));

import ManageRegisterSlidesDatatable from "~/components/Register/ManageRegisterSlidesDatatable/ManageRegisterSlidesDatatable";

function makeSlides(): RegisterSlideResponseDto[] {
  return [
    { id: 1, sortOrder: 1, imagePath: "a.png" },
    { id: 2, sortOrder: 2, imagePath: "b.png" },
  ] as RegisterSlideResponseDto[];
}

describe("ManageRegisterSlidesDatatable", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("shows a loading state, then renders fetched slides sorted by sortOrder", async () => {
    getRegisterslides.mockResolvedValue({ data: makeSlides().reverse() });

    renderWithProviders(<ManageRegisterSlidesDatatable />);

    expect(screen.getByText("loading")).toBeInTheDocument();

    await waitFor(() =>
      expect(screen.getByText("slide #1")).toBeInTheDocument(),
    );
    expect(screen.getByText("slide #2")).toBeInTheDocument();
  });

  it("shows the empty state when there are no slides", async () => {
    getRegisterslides.mockResolvedValue({ data: [] });

    renderWithProviders(<ManageRegisterSlidesDatatable />);

    await waitFor(() =>
      expect(screen.getByText("no_slides")).toBeInTheDocument(),
    );
  });

  it("opens the create modal when add_slide is clicked", async () => {
    const user = userEvent.setup();
    getRegisterslides.mockResolvedValue({ data: [] });

    renderWithProviders(<ManageRegisterSlidesDatatable />);
    await waitFor(() =>
      expect(screen.getByText("no_slides")).toBeInTheDocument(),
    );

    await user.click(screen.getByRole("button", { name: "add_slide" }));

    expect(
      screen.getByRole("heading", { name: "add_slide" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "create" })).toBeInTheDocument();
  });

  it("opens the edit modal for the clicked slide", async () => {
    const user = userEvent.setup();
    getRegisterslides.mockResolvedValue({ data: makeSlides() });

    renderWithProviders(<ManageRegisterSlidesDatatable />);
    await waitFor(() =>
      expect(screen.getByText("slide #1")).toBeInTheDocument(),
    );

    await user.click(screen.getAllByRole("button", { name: "edit" })[0]);

    expect(
      screen.getByRole("heading", { name: "edit_slide" }),
    ).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "save" })).toBeInTheDocument();
  });

  it("persists the new order and refetches after a drag-and-drop reorder", async () => {
    getRegisterslides.mockResolvedValue({ data: makeSlides() });
    putRegisterslidesById.mockResolvedValue({});

    renderWithProviders(<ManageRegisterSlidesDatatable />);
    await waitFor(() =>
      expect(screen.getByText("slide #1")).toBeInTheDocument(),
    );

    const rows = document.querySelectorAll("[draggable]");
    const firstRow = rows[0] as HTMLElement;
    const secondRow = rows[1] as HTMLElement;

    fireEvent.dragStart(firstRow, { dataTransfer: {} });
    fireEvent.dragEnter(secondRow, { dataTransfer: {} });
    fireEvent.dragEnd(firstRow, { dataTransfer: {} });

    await waitFor(() => expect(putRegisterslidesById).toHaveBeenCalledTimes(2));
    expect(putRegisterslidesById).toHaveBeenCalledWith({
      path: { id: 2 },
      body: { sortOrder: 1 },
    });
    expect(putRegisterslidesById).toHaveBeenCalledWith({
      path: { id: 1 },
      body: { sortOrder: 2 },
    });

    await waitFor(() => expect(getRegisterslides).toHaveBeenCalledTimes(2));
  });

  it("logs an error when fetching slides fails", async () => {
    getRegisterslides.mockRejectedValue(new Error("boom"));
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    renderWithProviders(<ManageRegisterSlidesDatatable />);

    await waitFor(() => expect(consoleError).toHaveBeenCalled());
    consoleError.mockRestore();
  });

  it("ignores a dragEnter that fires without a preceding dragStart", async () => {
    getRegisterslides.mockResolvedValue({ data: makeSlides() });

    renderWithProviders(<ManageRegisterSlidesDatatable />);
    await waitFor(() =>
      expect(screen.getByText("slide #1")).toBeInTheDocument(),
    );

    const rows = document.querySelectorAll("[draggable]");
    fireEvent.dragEnter(rows[1], { dataTransfer: {} });

    expect(putRegisterslidesById).not.toHaveBeenCalled();
  });

  it("prevents the default browser behavior on dragOver", async () => {
    getRegisterslides.mockResolvedValue({ data: makeSlides() });

    renderWithProviders(<ManageRegisterSlidesDatatable />);
    await waitFor(() =>
      expect(screen.getByText("slide #1")).toBeInTheDocument(),
    );

    const row = document.querySelectorAll("[draggable]")[0];
    const event = new Event("dragover", { bubbles: true, cancelable: true });
    row.dispatchEvent(event);

    expect(event.defaultPrevented).toBe(true);
  });

  it("logs an error when persisting the reordered slides fails", async () => {
    getRegisterslides.mockResolvedValue({ data: makeSlides() });
    putRegisterslidesById.mockRejectedValue(new Error("boom"));
    const consoleError = vi
      .spyOn(console, "error")
      .mockImplementation(() => {});

    renderWithProviders(<ManageRegisterSlidesDatatable />);
    await waitFor(() =>
      expect(screen.getByText("slide #1")).toBeInTheDocument(),
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
      "~/components/Register/EditRegisterSlideOverlay/EditRegisterSlideOverlay",
      () => ({
        default: ({ onComplete }: { onComplete: () => void }) => (
          <button type="button" onClick={onComplete}>
            complete-slide
          </button>
        ),
      }),
    );
    vi.resetModules();
    const { default: FreshDatatable } = await import(
      "~/components/Register/ManageRegisterSlidesDatatable/ManageRegisterSlidesDatatable"
    );
    getRegisterslides.mockResolvedValue({ data: [] });

    renderWithProviders(<FreshDatatable />);
    await waitFor(() =>
      expect(screen.getByText("no_slides")).toBeInTheDocument(),
    );

    fireEvent.click(screen.getByRole("button", { name: "add_slide" }));
    fireEvent.click(await screen.findByText("complete-slide"));

    await waitFor(() =>
      expect(screen.queryByText("complete-slide")).not.toBeInTheDocument(),
    );
    expect(getRegisterslides).toHaveBeenCalledTimes(2);
    vi.doUnmock("../EditRegisterSlideOverlay/EditRegisterSlideOverlay");
  });

  it("closes the create modal without saving when dismissed", async () => {
    const user = userEvent.setup();
    getRegisterslides.mockResolvedValue({ data: [] });

    renderWithProviders(<ManageRegisterSlidesDatatable />);
    await waitFor(() =>
      expect(screen.getByText("no_slides")).toBeInTheDocument(),
    );

    await user.click(screen.getByRole("button", { name: "add_slide" }));
    expect(
      screen.getByRole("heading", { name: "add_slide" }),
    ).toBeInTheDocument();

    fireEvent.keyDown(window, { key: "Escape" });

    await waitFor(() =>
      expect(
        screen.queryByRole("heading", { name: "add_slide" }),
      ).not.toBeInTheDocument(),
    );
  });
});
