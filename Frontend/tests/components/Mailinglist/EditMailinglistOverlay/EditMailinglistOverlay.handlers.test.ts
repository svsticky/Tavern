import { beforeEach, describe, expect, it, vi } from "vitest";
import type { CuratedMailinglistDto, MailinglistDto } from "~/api";
import {
  fetchAddableMailinglists,
  handleMailinglistDelete,
  handleMailinglistSubmit,
} from "~/components/Mailinglist/EditMailinglistOverlay/EditMailinglistOverlay.handlers";

const {
  getMailinglistsAddable,
  postMailinglists,
  patchMailinglistsById,
  deleteMailinglistsById,
} = vi.hoisted(() => ({
  getMailinglistsAddable: vi.fn(),
  postMailinglists: vi.fn(),
  patchMailinglistsById: vi.fn(),
  deleteMailinglistsById: vi.fn(),
}));

vi.mock("~/api", () => ({
  getMailinglistsAddable,
  postMailinglists,
  patchMailinglistsById,
  deleteMailinglistsById,
}));

const toastErrorFn = vi.fn();
vi.mock("react-hot-toast", () => ({
  default: {
    error: (...args: unknown[]) => toastErrorFn(...args),
    promise: vi.fn((p: Promise<unknown>, opts: any) => {
      p.then(
        (data) => opts.success?.(data),
        (err) => opts.error?.(err),
      ).catch(() => {});
      return p;
    }),
  },
}));

function makeEvent() {
  return { preventDefault: vi.fn() } as unknown as React.FormEvent;
}

describe("fetchAddableMailinglists", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("sets addable lists on success", async () => {
    const lists: MailinglistDto[] = [{ id: "p1", name: "Newsletter" }];
    getMailinglistsAddable.mockResolvedValue({ data: lists });
    const setAddableLists = vi.fn();

    await fetchAddableMailinglists(vi.fn(), setAddableLists);

    expect(setAddableLists).toHaveBeenCalledWith(lists);
  });

  it("shows an error toast on failure", async () => {
    getMailinglistsAddable.mockResolvedValue({ error: "fail" });
    const setAddableLists = vi.fn();

    await fetchAddableMailinglists(vi.fn(), setAddableLists);

    expect(toastErrorFn).toHaveBeenCalled();
    expect(setAddableLists).not.toHaveBeenCalled();
  });
});

describe("handleMailinglistSubmit", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("posts a new curated list when no curatedList is given", async () => {
    const created = { id: 1, providerListId: "p1" } as CuratedMailinglistDto;
    postMailinglists.mockResolvedValue({ data: created });
    const onComplete = vi.fn();

    await handleMailinglistSubmit({
      e: makeEvent(),
      providerListId: "p1",
      visibility: "General",
      setLoading: vi.fn(),
      onComplete,
    });

    expect(postMailinglists).toHaveBeenCalledWith({
      body: { providerListId: "p1", visibility: "General" },
    });
    await vi.waitFor(() => expect(onComplete).toHaveBeenCalledWith(created));
  });

  it("patches the visibility of an existing curated list", async () => {
    patchMailinglistsById.mockResolvedValue({});
    const curatedList = {
      id: 5,
      providerListId: "p1",
      visibility: "General",
    } as CuratedMailinglistDto;
    const onComplete = vi.fn();

    await handleMailinglistSubmit({
      e: makeEvent(),
      curatedList,
      providerListId: "",
      visibility: "YearlyRenewalOnly",
      setLoading: vi.fn(),
      onComplete,
    });

    expect(patchMailinglistsById).toHaveBeenCalledWith({
      path: { id: 5 },
      body: { visibility: "YearlyRenewalOnly" },
    });
    expect(postMailinglists).not.toHaveBeenCalled();
    await vi.waitFor(() =>
      expect(onComplete).toHaveBeenCalledWith({
        ...curatedList,
        visibility: "YearlyRenewalOnly",
      }),
    );
  });
});

describe("handleMailinglistDelete", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("does nothing without a curatedList id", async () => {
    const confirm = vi.fn().mockResolvedValue(true);

    await handleMailinglistDelete({
      curatedList: undefined,
      setLoading: vi.fn(),
      onComplete: vi.fn(),
      confirm,
    });

    expect(confirm).not.toHaveBeenCalled();
    expect(deleteMailinglistsById).not.toHaveBeenCalled();
  });

  it("does nothing when the user cancels the confirmation", async () => {
    const confirm = vi.fn().mockResolvedValue(false);

    await handleMailinglistDelete({
      curatedList: { id: 5 } as CuratedMailinglistDto,
      setLoading: vi.fn(),
      onComplete: vi.fn(),
      confirm,
    });

    expect(deleteMailinglistsById).not.toHaveBeenCalled();
  });

  it("deletes the curated list and calls onComplete", async () => {
    deleteMailinglistsById.mockResolvedValue({});
    const onComplete = vi.fn();
    const confirm = vi.fn().mockResolvedValue(true);

    await handleMailinglistDelete({
      curatedList: { id: 5 } as CuratedMailinglistDto,
      setLoading: vi.fn(),
      onComplete,
      confirm,
    });

    expect(deleteMailinglistsById).toHaveBeenCalledWith({ path: { id: 5 } });
    await vi.waitFor(() => expect(onComplete).toHaveBeenCalledWith(undefined));
  });
});
