import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { describe, expect, it, vi } from "vitest";
import type { CuratedMailinglistDto } from "~/api";
import ManageMailinglistsDatatable from "~/components/Mailinglist/ManageMailinglistsDatatable/ManageMailinglistsDatatable";

const { fetchCuratedMailinglists, handleMailinglistEdited } = vi.hoisted(
  () => ({
    fetchCuratedMailinglists: vi.fn(),
    handleMailinglistEdited: vi.fn(),
  }),
);

vi.mock(
  "~/components/Mailinglist/ManageMailinglistsDatatable/ManageMailinglistsDatatable.handlers",
  () => ({
    fetchCuratedMailinglists,
    handleMailinglistEdited,
  }),
);

vi.mock(
  "~/components/Mailinglist/EditMailinglistOverlay/EditMailinglistOverlay",
  () => ({
    default: ({ onMailinglistEdited }: { onMailinglistEdited: () => void }) => (
      <button onClick={() => onMailinglistEdited()}>Overlay stub</button>
    ),
  }),
);

describe("ManageMailinglistsDatatable", () => {
  it("fetches curated mailing lists on mount", () => {
    render(<ManageMailinglistsDatatable />);
    expect(fetchCuratedMailinglists).toHaveBeenCalledWith(
      expect.any(Function),
      expect.any(Function),
    );
  });

  it("shows a loading message while fetching", () => {
    render(<ManageMailinglistsDatatable />);
    expect(screen.getByText("loading")).toBeInTheDocument();
  });

  it("opens the modal in add mode when 'add_mailing_list' is clicked", async () => {
    const user = userEvent.setup();
    render(<ManageMailinglistsDatatable />);

    await user.click(
      screen.getAllByRole("button", { name: "add_mailing_list" })[0],
    );

    expect(screen.getByText("Overlay stub")).toBeInTheDocument();
  });

  it("calls handleMailinglistEdited when the overlay reports completion", async () => {
    const user = userEvent.setup();
    render(<ManageMailinglistsDatatable />);

    await user.click(
      screen.getAllByRole("button", { name: "add_mailing_list" })[0],
    );
    await user.click(screen.getByText("Overlay stub"));

    expect(handleMailinglistEdited).toHaveBeenCalledTimes(1);
  });

  it("renders curated list rows, their visibility, and an orphaned warning", async () => {
    fetchCuratedMailinglists.mockImplementation(
      async (
        setLoading: (l: boolean) => void,
        setCuratedLists: (lists: CuratedMailinglistDto[]) => void,
      ) => {
        setCuratedLists([
          {
            id: 1,
            providerListId: "p1",
            name: "Newsletter",
            visibility: "General",
            orphaned: false,
          } as CuratedMailinglistDto,
          {
            id: 2,
            providerListId: "p2",
            name: null,
            visibility: "YearlyRenewalOnly",
            orphaned: true,
          } as CuratedMailinglistDto,
        ]);
        setLoading(false);
      },
    );

    render(<ManageMailinglistsDatatable />);

    await waitFor(() =>
      expect(screen.getByText("Newsletter")).toBeInTheDocument(),
    );
    expect(screen.getByText("general")).toBeInTheDocument();
    expect(screen.getByText("yearly_renewal_only")).toBeInTheDocument();
    // Orphaned entry falls back to the raw provider ID when name is null.
    expect(screen.getByText("p2")).toBeInTheDocument();
  });

  it("opens the modal in edit mode with the clicked list when 'edit' is clicked", async () => {
    fetchCuratedMailinglists.mockImplementation(
      async (
        setLoading: (l: boolean) => void,
        setCuratedLists: (lists: CuratedMailinglistDto[]) => void,
      ) => {
        setCuratedLists([
          {
            id: 1,
            providerListId: "p1",
            name: "Newsletter",
            visibility: "General",
          } as CuratedMailinglistDto,
        ]);
        setLoading(false);
      },
    );
    const user = userEvent.setup();
    render(<ManageMailinglistsDatatable />);

    await user.click(await screen.findByRole("button", { name: "edit" }));

    expect(screen.getByText("Overlay stub")).toBeInTheDocument();
  });
});
