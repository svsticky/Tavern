import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { Mailinglist } from "~/api";
import ManageMailingListsDatatable from "~/components/Mailinglist/ManageMailinglistsDatatable/ManageMailinglistsDatatable";

const { fetchMailingLists, handleMailingListEdited } = vi.hoisted(() => ({
  fetchMailingLists: vi.fn(),
  handleMailingListEdited: vi.fn(),
}));

vi.mock(
  "~/components/Mailinglist/ManageMailinglistsDatatable/ManageMailinglistsDatatable.handlers",
  () => ({
    fetchMailingLists,
    handleMailingListEdited,
  }),
);

vi.mock(
  "~/components/Mailinglist/EditMailinglistOverlay/EditMailinglistOverlay",
  () => ({
    default: ({ onMailingListEdited }: { onMailingListEdited: () => void }) => (
      <button onClick={() => onMailingListEdited()}>Overlay stub</button>
    ),
  }),
);

describe("ManageMailingListsDatatable", () => {
  it("fetches mailing lists on mount", () => {
    render(<ManageMailingListsDatatable />);
    expect(fetchMailingLists).toHaveBeenCalledWith(
      expect.any(Function),
      expect.any(Function),
    );
  });

  it("shows a loading message while fetching", () => {
    render(<ManageMailingListsDatatable />);
    expect(screen.getByText("loading")).toBeInTheDocument();
  });

  it("opens the modal in create mode when 'add_mailing_list' is clicked", async () => {
    const user = userEvent.setup();
    render(<ManageMailingListsDatatable />);

    await user.click(
      screen.getAllByRole("button", { name: "add_mailing_list" })[0],
    );

    expect(screen.getByText("Overlay stub")).toBeInTheDocument();
  });

  it("calls handleMailingListEdited when the overlay reports completion", async () => {
    const user = userEvent.setup();
    render(<ManageMailingListsDatatable />);

    await user.click(
      screen.getAllByRole("button", { name: "add_mailing_list" })[0],
    );
    await user.click(screen.getByText("Overlay stub"));

    expect(handleMailingListEdited).toHaveBeenCalledTimes(1);
  });

  it("renders mailing list rows once fetchMailingLists populates state", async () => {
    fetchMailingLists.mockImplementation(
      async (
        setLoading: (l: boolean) => void,
        setMailingLists: (lists: Mailinglist[]) => void,
      ) => {
        setMailingLists([{ id: 1, name: "Newsletter" } as Mailinglist]);
        setLoading(false);
      },
    );

    render(<ManageMailingListsDatatable />);

    await waitFor(() =>
      expect(screen.getByText("Newsletter")).toBeInTheDocument(),
    );
  });

  it("shows the no-mailing-lists message once loading finishes with no data", async () => {
    fetchMailingLists.mockImplementation(
      async (setLoading: (l: boolean) => void) => setLoading(false),
    );

    render(<ManageMailingListsDatatable />);

    expect(await screen.findByText("no_mailing_lists")).toBeInTheDocument();
  });

  it("opens the modal in edit mode when a row's 'edit' button is clicked", async () => {
    fetchMailingLists.mockImplementation(
      async (
        setLoading: (l: boolean) => void,
        setMailingLists: (lists: Mailinglist[]) => void,
      ) => {
        setMailingLists([{ id: 1, name: "Newsletter" } as Mailinglist]);
        setLoading(false);
      },
    );
    const user = userEvent.setup();
    render(<ManageMailingListsDatatable />);

    await screen.findByText("Newsletter");
    await user.click(screen.getByRole("button", { name: "edit" }));

    expect(screen.getByText("Overlay stub")).toBeInTheDocument();

    await user.click(screen.getByText("Overlay stub"));
    expect(handleMailingListEdited).toHaveBeenCalledWith(
      expect.objectContaining({
        editedList: expect.objectContaining({ id: 1 }),
      }),
    );
  });

  it("closes the modal when it is dismissed without saving", async () => {
    const user = userEvent.setup();
    render(<ManageMailingListsDatatable />);

    await user.click(
      screen.getAllByRole("button", { name: "add_mailing_list" })[0],
    );
    expect(screen.getByText("Overlay stub")).toBeInTheDocument();

    await user.keyboard("{Escape}");

    await waitFor(() =>
      expect(screen.queryByText("Overlay stub")).not.toBeInTheDocument(),
    );
  });
});
