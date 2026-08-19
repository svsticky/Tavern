import { beforeEach, describe, expect, it, vi } from "vitest";
import type { CuratedMailinglistDto } from "~/api";
import {
  fetchCuratedMailinglists,
  handleMailinglistEdited,
} from "~/components/Mailinglist/ManageMailinglistsDatatable/ManageMailinglistsDatatable.handlers";

const { getMailinglistsCurated } = vi.hoisted(() => ({
  getMailinglistsCurated: vi.fn(),
}));

vi.mock("~/api", () => ({ getMailinglistsCurated }));

const toastErrorFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: { error: (...args: unknown[]) => toastErrorFn(...args) },
}));

describe("fetchCuratedMailinglists", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("sets curated lists on success", async () => {
    const lists: CuratedMailinglistDto[] = [
      {
        id: 1,
        providerListId: "p1",
        name: "Newsletter",
        visibility: "General",
      },
    ];
    getMailinglistsCurated.mockResolvedValue({ data: lists });
    const setCuratedLists = vi.fn();
    const setLoading = vi.fn();

    await fetchCuratedMailinglists(setLoading, setCuratedLists);

    expect(setCuratedLists).toHaveBeenCalledWith(lists);
    expect(setLoading).toHaveBeenCalledWith(true);
    expect(setLoading).toHaveBeenCalledWith(false);
  });

  it("shows an error toast on failure", async () => {
    getMailinglistsCurated.mockResolvedValue({ error: "fail" });
    const setCuratedLists = vi.fn();

    await fetchCuratedMailinglists(vi.fn(), setCuratedLists);

    expect(toastErrorFn).toHaveBeenCalled();
    expect(setCuratedLists).not.toHaveBeenCalled();
  });
});

describe("handleMailinglistEdited", () => {
  it("removes the edited list from the list when list is undefined (deletion)", () => {
    const setCuratedLists = vi.fn();
    const setIsEditModalOpen = vi.fn();
    const setEditedList = vi.fn();

    handleMailinglistEdited({
      list: undefined,
      editedList: { id: 1 } as CuratedMailinglistDto,
      setCuratedLists,
      setIsEditModalOpen,
      setEditedList,
    });

    const updater = setCuratedLists.mock.calls[0][0];
    expect(
      updater([
        { id: 1 } as CuratedMailinglistDto,
        { id: 2 } as CuratedMailinglistDto,
      ]),
    ).toEqual([{ id: 2 }]);
    expect(setIsEditModalOpen).toHaveBeenCalledWith(false);
    expect(setEditedList).toHaveBeenCalledWith(undefined);
  });

  it("does not touch the list when deleting without an editedList", () => {
    const setCuratedLists = vi.fn();
    handleMailinglistEdited({
      list: undefined,
      editedList: undefined,
      setCuratedLists,
      setIsEditModalOpen: vi.fn(),
      setEditedList: vi.fn(),
    });
    expect(setCuratedLists).not.toHaveBeenCalled();
  });

  it("replaces an existing entry when the id matches (visibility change)", () => {
    const setCuratedLists = vi.fn();
    const updated = {
      id: 1,
      providerListId: "p1",
      visibility: "YearlyRenewalOnly",
    } as CuratedMailinglistDto;

    handleMailinglistEdited({
      list: updated,
      editedList: { id: 1 } as CuratedMailinglistDto,
      setCuratedLists,
      setIsEditModalOpen: vi.fn(),
      setEditedList: vi.fn(),
    });

    const updater = setCuratedLists.mock.calls[0][0];
    expect(
      updater([{ id: 1, visibility: "General" } as CuratedMailinglistDto]),
    ).toEqual([updated]);
  });

  it("appends a new entry when adding a mailing list", () => {
    const setCuratedLists = vi.fn();
    const created = { id: 2, providerListId: "p2" } as CuratedMailinglistDto;

    handleMailinglistEdited({
      list: created,
      editedList: undefined,
      setCuratedLists,
      setIsEditModalOpen: vi.fn(),
      setEditedList: vi.fn(),
    });

    const updater = setCuratedLists.mock.calls[0][0];
    expect(
      updater([{ id: 1, providerListId: "p1" } as CuratedMailinglistDto]),
    ).toEqual([{ id: 1, providerListId: "p1" }, created]);
  });
});
