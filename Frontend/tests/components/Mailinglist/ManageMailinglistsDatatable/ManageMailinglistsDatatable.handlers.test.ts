import { describe, expect, it, vi } from "vitest";
import type { Mailinglist } from "~/api";
import {
  fetchMailingLists,
  handleMailingListEdited,
} from "~/components/Mailinglist/ManageMailinglistsDatatable/ManageMailinglistsDatatable.handlers";

const { getMailinglists } = vi.hoisted(() => ({ getMailinglists: vi.fn() }));

vi.mock("~/api", () => ({ getMailinglists }));
vi.mock("react-hot-toast", () => ({ default: { error: vi.fn() } }));

describe("fetchMailingLists", () => {
  it("sets the mailing lists on success", async () => {
    const lists: Mailinglist[] = [{ id: 1, name: "News" } as Mailinglist];
    getMailinglists.mockResolvedValue({ data: lists });
    const setLoading = vi.fn();
    const setMailingLists = vi.fn();

    await fetchMailingLists(setLoading, setMailingLists);

    expect(setMailingLists).toHaveBeenCalledWith(lists);
    expect(setLoading).toHaveBeenNthCalledWith(1, true);
    expect(setLoading).toHaveBeenNthCalledWith(2, false);
  });

  it("shows an error toast and does not set lists on failure", async () => {
    getMailinglists.mockResolvedValue({ error: { title: "Boom" } });
    const toast = (await import("react-hot-toast")).default;
    const setMailingLists = vi.fn();

    await fetchMailingLists(vi.fn(), setMailingLists);

    expect(setMailingLists).not.toHaveBeenCalled();
    expect(toast.error).toHaveBeenCalled();
  });
});

describe("handleMailingListEdited", () => {
  const existing: Mailinglist[] = [
    { id: 1, name: "A" } as Mailinglist,
    { id: 2, name: "B" } as Mailinglist,
  ];

  it("removes the list when it was deleted (no list, but an editedList)", () => {
    const setMailingLists = vi.fn();
    handleMailingListEdited({
      list: undefined,
      editedList: existing[0],
      setMailingLists,
      setIsEditModalOpen: vi.fn(),
      setEditedList: vi.fn(),
    });

    const updater = setMailingLists.mock.calls[0][0];
    expect(updater(existing)).toEqual([existing[1]]);
  });

  it("replaces the matching list when editing an existing one", () => {
    const setMailingLists = vi.fn();
    const updated = { id: 1, name: "A updated" } as Mailinglist;
    handleMailingListEdited({
      list: updated,
      editedList: existing[0],
      setMailingLists,
      setIsEditModalOpen: vi.fn(),
      setEditedList: vi.fn(),
    });

    const updater = setMailingLists.mock.calls[0][0];
    expect(updater(existing)).toEqual([updated, existing[1]]);
  });

  it("appends the new list when creating (list present, no editedList)", () => {
    const setMailingLists = vi.fn();
    const created = { id: 3, name: "C" } as Mailinglist;
    handleMailingListEdited({
      list: created,
      editedList: undefined,
      setMailingLists,
      setIsEditModalOpen: vi.fn(),
      setEditedList: vi.fn(),
    });

    const updater = setMailingLists.mock.calls[0][0];
    expect(updater(existing)).toEqual([...existing, created]);
  });

  it("always closes the modal and clears the edited list", () => {
    const setIsEditModalOpen = vi.fn();
    const setEditedList = vi.fn();
    handleMailingListEdited({
      list: undefined,
      editedList: undefined,
      setMailingLists: vi.fn(),
      setIsEditModalOpen,
      setEditedList,
    });

    expect(setIsEditModalOpen).toHaveBeenCalledWith(false);
    expect(setEditedList).toHaveBeenCalledWith(undefined);
  });
});
