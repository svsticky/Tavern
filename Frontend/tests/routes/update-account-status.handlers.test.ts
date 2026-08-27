import { beforeEach, describe, expect, it, vi } from "vitest";
import type { MemberMailinglistDto } from "~/api";
import {
  fetchYearlyMailinglists,
  handleSaveYearlyMailinglists,
  handleYearlyMailinglistToggle,
} from "~/routes/update-account-status.handlers";

const { getMembersByIdMailinglists, putMembersByIdMailinglists } = vi.hoisted(
  () => ({
    getMembersByIdMailinglists: vi.fn(),
    putMembersByIdMailinglists: vi.fn(),
  }),
);

vi.mock("~/api", () => ({
  getMembersByIdMailinglists,
  putMembersByIdMailinglists,
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

describe("fetchYearlyMailinglists", () => {
  beforeEach(() => {
    vi.clearAllMocks();
    vi.spyOn(console, "error").mockImplementation(() => {});
  });

  it("requests the yearly-renewal context and populates subscribed ids", async () => {
    const data: MemberMailinglistDto[] = [
      { id: "list-1", name: "Newsletter", subscribed: false },
      { id: "alumni", name: "Alumni", subscribed: true },
    ];
    getMembersByIdMailinglists.mockResolvedValue({ data });
    const setMailingLists = vi.fn();
    const setSubscribedIds = vi.fn();
    const setUnavailable = vi.fn();

    await fetchYearlyMailinglists(
      "member-1",
      vi.fn(),
      setMailingLists,
      setSubscribedIds,
      setUnavailable,
    );

    expect(getMembersByIdMailinglists).toHaveBeenCalledWith({
      path: { id: "member-1" },
      query: { includeYearlyRenewal: true },
    });
    expect(setMailingLists).toHaveBeenCalledWith(data);
    expect(setSubscribedIds).toHaveBeenCalledWith(new Set(["alumni"]));
    expect(setUnavailable).toHaveBeenCalledWith(false);
  });

  it("marks the section unavailable and shows a toast on failure", async () => {
    getMembersByIdMailinglists.mockResolvedValue({ error: "fail" });
    const setUnavailable = vi.fn();
    const setMailingLists = vi.fn();

    await fetchYearlyMailinglists(
      "member-1",
      vi.fn(),
      setMailingLists,
      vi.fn(),
      setUnavailable,
    );

    expect(setUnavailable).toHaveBeenCalledWith(true);
    expect(setMailingLists).toHaveBeenCalledWith([]);
    expect(toastErrorFn).toHaveBeenCalled();
  });
});

describe("handleYearlyMailinglistToggle", () => {
  it("adds the id when checked", () => {
    const setSubscribedIds = vi.fn();
    handleYearlyMailinglistToggle("alumni", true, setSubscribedIds);

    const updater = setSubscribedIds.mock.calls[0][0];
    expect(updater(new Set(["list-1"]))).toEqual(new Set(["list-1", "alumni"]));
  });

  it("removes the id when unchecked", () => {
    const setSubscribedIds = vi.fn();
    handleYearlyMailinglistToggle("alumni", false, setSubscribedIds);

    const updater = setSubscribedIds.mock.calls[0][0];
    expect(updater(new Set(["list-1", "alumni"]))).toEqual(new Set(["list-1"]));
  });
});

describe("handleSaveYearlyMailinglists", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("submits within the yearly-renewal context", async () => {
    putMembersByIdMailinglists.mockResolvedValue({});
    const setSaving = vi.fn();

    await handleSaveYearlyMailinglists(
      "member-1",
      new Set(["list-1", "alumni"]),
      setSaving,
    );

    expect(putMembersByIdMailinglists).toHaveBeenCalledWith({
      path: { id: "member-1" },
      query: { includeYearlyRenewal: true },
      body: expect.arrayContaining(["list-1", "alumni"]),
    });
    expect(setSaving).toHaveBeenCalledWith(true);
    expect(setSaving).toHaveBeenCalledWith(false);
  });
});
