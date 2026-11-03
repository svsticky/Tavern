import { beforeEach, describe, expect, it, vi } from "vitest";
import {
  handleSaveYearlyMailinglists,
  handleYearlyMailinglistToggle,
} from "~/routes/update-account-status/update-account-status.handlers";

const { putMembersByIdMailinglists } = vi.hoisted(() => ({
  putMembersByIdMailinglists: vi.fn(),
}));

vi.mock("~/api", () => ({ putMembersByIdMailinglists }));

vi.mock("react-hot-toast", () => ({
  default: {
    promise: vi.fn((p: Promise<unknown>, opts: any) => {
      p.then(
        (data) => opts.success?.(data),
        (err) => opts.error?.(err),
      ).catch(() => {});
      return p;
    }),
  },
}));

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
