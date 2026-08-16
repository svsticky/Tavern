import toast from "react-hot-toast";
import { beforeEach, describe, expect, it, vi } from "vitest";
import type { Mailinglist } from "~/api";
import {
  handleMailingListDelete,
  handleMailingListSubmit,
} from "~/components/Mailinglist/EditMailinglistOverlay/EditMailinglistOverlay.handlers";

const { deleteMailinglistsById, postMailinglists, putMailinglistsById } =
  vi.hoisted(() => ({
    deleteMailinglistsById: vi.fn(),
    postMailinglists: vi.fn(),
    putMailinglistsById: vi.fn(),
  }));

vi.mock("~/api", () => ({
  deleteMailinglistsById,
  postMailinglists,
  putMailinglistsById,
}));

vi.mock("react-hot-toast", () => ({
  default: { success: vi.fn(), error: vi.fn() },
}));

const formData: Omit<Mailinglist, "id"> = {
  name: "Newsletter",
  bitValue: 1,
} as Omit<Mailinglist, "id">;

describe("handleMailingListSubmit", () => {
  let setLoading: ReturnType<typeof vi.fn<(loading: boolean) => void>>;
  let onComplete: ReturnType<typeof vi.fn<(list?: Mailinglist) => void>>;

  beforeEach(() => {
    vi.clearAllMocks();
    setLoading = vi.fn();
    onComplete = vi.fn();
  });

  it("creates a new mailing list when none is passed", async () => {
    postMailinglists.mockResolvedValue({
      data: { id: 42, bitValue: 2 },
    });

    await handleMailingListSubmit({
      e: {} as React.FormEvent,
      formData,
      mailingList: undefined,
      setLoading,
      onComplete,
    });

    expect(postMailinglists).toHaveBeenCalledWith({ body: formData });
    expect(setLoading).toHaveBeenNthCalledWith(1, true);
    expect(setLoading).toHaveBeenNthCalledWith(2, false);
    expect(toast.success).toHaveBeenCalled();
    expect(onComplete).toHaveBeenCalledWith({
      ...formData,
      id: 42,
      bitValue: 2,
    });
  });

  it("updates an existing mailing list when one is passed", async () => {
    putMailinglistsById.mockResolvedValue({});
    const existing: Mailinglist = {
      id: 7,
      name: "Old name",
      bitValue: 1,
    } as Mailinglist;

    await handleMailingListSubmit({
      e: {} as React.FormEvent,
      formData,
      mailingList: existing,
      setLoading,
      onComplete,
    });

    expect(putMailinglistsById).toHaveBeenCalledWith({
      path: { id: 7 },
      body: formData,
    });
    expect(onComplete).toHaveBeenCalledWith({ ...formData, id: 7 });
    expect(toast.error).not.toHaveBeenCalled();
  });

  it("shows an error toast and still clears loading when creation fails", async () => {
    postMailinglists.mockResolvedValue({ error: { title: "Boom" } });

    await handleMailingListSubmit({
      e: {} as React.FormEvent,
      formData,
      mailingList: undefined,
      setLoading,
      onComplete,
    });

    expect(toast.error).toHaveBeenCalled();
    expect(onComplete).not.toHaveBeenCalled();
    expect(setLoading).toHaveBeenLastCalledWith(false);
  });

  it("shows an error toast when the update call returns an error", async () => {
    putMailinglistsById.mockResolvedValue({ error: { title: "Boom" } });

    await handleMailingListSubmit({
      e: {} as React.FormEvent,
      formData,
      mailingList: { id: 7, name: "x", bitValue: 1 } as Mailinglist,
      setLoading,
      onComplete,
    });

    expect(toast.error).toHaveBeenCalled();
    expect(onComplete).not.toHaveBeenCalled();
  });
});

describe("handleMailingListDelete", () => {
  let setLoading: ReturnType<typeof vi.fn<(loading: boolean) => void>>;
  let onComplete: ReturnType<typeof vi.fn<(list?: Mailinglist) => void>>;

  beforeEach(() => {
    vi.clearAllMocks();
    setLoading = vi.fn();
    onComplete = vi.fn();
  });

  it("deletes the mailing list and reports completion with no list", async () => {
    deleteMailinglistsById.mockResolvedValue({});
    const list: Mailinglist = { id: 9, name: "x", bitValue: 1 } as Mailinglist;

    await handleMailingListDelete({
      mailingList: list,
      setLoading,
      onComplete,
    });

    expect(deleteMailinglistsById).toHaveBeenCalledWith({ path: { id: 9 } });
    expect(toast.success).toHaveBeenCalled();
    expect(onComplete).toHaveBeenCalledWith(undefined);
    expect(setLoading).toHaveBeenNthCalledWith(1, true);
    expect(setLoading).toHaveBeenNthCalledWith(2, false);
  });

  it("shows an error toast and does not call onComplete when deletion fails", async () => {
    deleteMailinglistsById.mockResolvedValue({ error: { title: "Boom" } });
    const list: Mailinglist = { id: 9, name: "x", bitValue: 1 } as Mailinglist;

    await handleMailingListDelete({
      mailingList: list,
      setLoading,
      onComplete,
    });

    expect(toast.error).toHaveBeenCalled();
    expect(onComplete).not.toHaveBeenCalled();
  });
});
