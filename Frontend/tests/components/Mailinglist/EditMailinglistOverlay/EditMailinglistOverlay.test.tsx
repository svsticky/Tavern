import { render, screen, waitFor } from "@testing-library/react";
import userEvent from "@testing-library/user-event";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { CuratedMailinglistDto, MailinglistDto } from "~/api";
import EditMailinglistOverlay from "~/components/Mailinglist/EditMailinglistOverlay/EditMailinglistOverlay";

const {
  fetchAddableMailinglists,
  handleMailinglistDelete,
  handleMailinglistSubmit,
} = vi.hoisted(() => ({
  fetchAddableMailinglists: vi.fn(),
  handleMailinglistDelete: vi.fn(),
  handleMailinglistSubmit: vi.fn(),
}));

vi.mock(
  "~/components/Mailinglist/EditMailinglistOverlay/EditMailinglistOverlay.handlers",
  () => ({
    fetchAddableMailinglists,
    handleMailinglistDelete,
    handleMailinglistSubmit,
  }),
);

describe("EditMailinglistOverlay", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("fetches addable lists and shows a disabled add button when adding", () => {
    render(<EditMailinglistOverlay onMailinglistEdited={vi.fn()} />);

    expect(fetchAddableMailinglists).toHaveBeenCalledWith(
      expect.any(Function),
      expect.any(Function),
    );
    expect(
      screen.getByRole("button", { name: "add_mailing_list" }),
    ).toBeDisabled();
    expect(
      screen.queryByRole("button", { name: "delete" }),
    ).not.toBeInTheDocument();
  });

  it("shows edit mode for an existing curated list without fetching addable lists", () => {
    const curatedList = {
      id: 1,
      providerListId: "p1",
      name: "Newsletter",
      visibility: "General",
    } as CuratedMailinglistDto;

    render(
      <EditMailinglistOverlay
        onMailinglistEdited={vi.fn()}
        curatedList={curatedList}
      />,
    );

    expect(fetchAddableMailinglists).not.toHaveBeenCalled();
    expect(screen.getByText("Newsletter")).toBeInTheDocument();
    expect(screen.getByRole("button", { name: "save" })).toBeEnabled();
    expect(screen.getByRole("button", { name: "delete" })).toBeInTheDocument();
  });

  it("enables the add button once a provider list is picked", async () => {
    fetchAddableMailinglists.mockImplementation(
      async (
        setLoading: (l: boolean) => void,
        setAddableLists: (lists: MailinglistDto[]) => void,
      ) => {
        setAddableLists([{ id: "p1", name: "Newsletter" }]);
        setLoading(false);
      },
    );
    const user = userEvent.setup();
    render(<EditMailinglistOverlay onMailinglistEdited={vi.fn()} />);

    await waitFor(() =>
      expect(screen.getByLabelText(/name/)).toBeInTheDocument(),
    );
    await user.selectOptions(screen.getByLabelText(/name/), "p1");

    expect(
      screen.getByRole("button", { name: "add_mailing_list" }),
    ).toBeEnabled();
  });

  it("calls handleMailinglistSubmit with the picked list and visibility on submit", async () => {
    fetchAddableMailinglists.mockImplementation(
      async (
        setLoading: (l: boolean) => void,
        setAddableLists: (lists: MailinglistDto[]) => void,
      ) => {
        setAddableLists([{ id: "p1", name: "Newsletter" }]);
        setLoading(false);
      },
    );
    const user = userEvent.setup();
    const onMailinglistEdited = vi.fn();
    render(
      <EditMailinglistOverlay onMailinglistEdited={onMailinglistEdited} />,
    );

    await waitFor(() =>
      expect(screen.getByLabelText(/name/)).toBeInTheDocument(),
    );
    await user.selectOptions(screen.getByLabelText(/name/), "p1");
    await user.selectOptions(
      screen.getByLabelText(/visibility/),
      "YearlyRenewalOnly",
    );
    await user.click(screen.getByRole("button", { name: "add_mailing_list" }));

    expect(handleMailinglistSubmit).toHaveBeenCalledWith(
      expect.objectContaining({
        providerListId: "p1",
        visibility: "YearlyRenewalOnly",
        onComplete: onMailinglistEdited,
      }),
    );
  });

  it("calls handleMailinglistDelete when delete is clicked", async () => {
    const user = userEvent.setup();
    const curatedList = {
      id: 5,
      providerListId: "p1",
    } as CuratedMailinglistDto;
    const onMailinglistEdited = vi.fn();
    render(
      <EditMailinglistOverlay
        onMailinglistEdited={onMailinglistEdited}
        curatedList={curatedList}
      />,
    );

    await user.click(screen.getByRole("button", { name: "delete" }));

    expect(handleMailinglistDelete).toHaveBeenCalledWith({
      curatedList,
      setLoading: expect.any(Function),
      onComplete: onMailinglistEdited,
      confirm: expect.any(Function),
    });
  });

  it("shows a message when there are no addable lists left", async () => {
    fetchAddableMailinglists.mockImplementation(
      async (
        setLoading: (l: boolean) => void,
        setAddableLists: (lists: MailinglistDto[]) => void,
      ) => {
        setAddableLists([]);
        setLoading(false);
      },
    );
    render(<EditMailinglistOverlay onMailinglistEdited={vi.fn()} />);

    expect(
      await screen.findByText("no_addable_mailing_lists"),
    ).toBeInTheDocument();
  });
});
