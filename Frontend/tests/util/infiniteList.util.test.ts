import { describe, expect, it, vi } from "vitest";
import {
  fetchPages,
  MAX_PAGES,
  readPages,
  shouldRevalidateIgnoring,
} from "~/util/infiniteList.util";

describe("readPages", () => {
  it("defaults to 1 when the param is missing", () => {
    expect(readPages(new URLSearchParams())).toBe(1);
  });

  it("reads a valid page count", () => {
    expect(readPages(new URLSearchParams("pages=4"))).toBe(4);
  });

  for (const value of ["0", "-3", "abc", "NaN", ""]) {
    it(`falls back to 1 for the invalid value ${JSON.stringify(value)}`, () => {
      expect(readPages(new URLSearchParams({ pages: value }))).toBe(1);
    });
  }

  it("clamps a hand-edited huge value so a URL can't trigger a request storm", () => {
    expect(readPages(new URLSearchParams("pages=9999"))).toBe(MAX_PAGES);
  });

  it("floors fractional values", () => {
    expect(readPages(new URLSearchParams("pages=2.9"))).toBe(2);
  });
});

describe("fetchPages", () => {
  it("fetches pages 1..count and joins them in order", async () => {
    const fetchPage = vi.fn(async (page: number) => [page * 10, page * 10 + 1]);

    const result = await fetchPages(3, 2, fetchPage);

    expect(fetchPage.mock.calls.map(([page]) => page)).toEqual([1, 2, 3]);
    expect(result.items).toEqual([10, 11, 20, 21, 30, 31]);
  });

  it("reports hasMore when the last page is full", async () => {
    const result = await fetchPages(2, 2, async () => [1, 2]);
    expect(result.hasMore).toBe(true);
  });

  it("reports no more when the last page is partial", async () => {
    const result = await fetchPages(2, 2, async (page) =>
      page === 1 ? [1, 2] : [3],
    );
    expect(result.hasMore).toBe(false);
  });

  it("rejects when any page fails", async () => {
    await expect(
      fetchPages(2, 2, async (page) => {
        if (page === 2) throw new Error("boom");
        return [1, 2];
      }),
    ).rejects.toThrow("boom");
  });
});

describe("shouldRevalidateIgnoring", () => {
  const args = (from: string, to: string, defaultShouldRevalidate = true) =>
    ({
      currentUrl: new URL(`http://localhost${from}`),
      nextUrl: new URL(`http://localhost${to}`),
      defaultShouldRevalidate,
    }) as Parameters<ReturnType<typeof shouldRevalidateIgnoring>>[0];

  const shouldRevalidate = shouldRevalidateIgnoring("pages");

  it("skips revalidation when only an ignored param changed", () => {
    expect(shouldRevalidate(args("/list?q=a", "/list?q=a&pages=2"))).toBe(
      false,
    );
    expect(shouldRevalidate(args("/list?pages=2", "/list?pages=3"))).toBe(
      false,
    );
  });

  it("revalidates when something else changed, even alongside an ignored param", () => {
    expect(shouldRevalidate(args("/list?pages=3", "/list?q=b"))).toBe(true);
    expect(
      shouldRevalidate(args("/list?q=a&pages=3", "/list?q=b&pages=3")),
    ).toBe(true);
  });

  it("falls through to the default for an explicit revalidation with an unchanged URL", () => {
    expect(shouldRevalidate(args("/list?pages=2", "/list?pages=2"))).toBe(true);
    expect(
      shouldRevalidate(args("/list?pages=2", "/list?pages=2", false)),
    ).toBe(false);
  });

  it("falls through to the default when the path changed", () => {
    expect(shouldRevalidate(args("/a?pages=2", "/b?pages=3"))).toBe(true);
  });

  it("ignores param order", () => {
    expect(
      shouldRevalidate(args("/list?a=1&b=2", "/list?b=2&a=1&pages=2")),
    ).toBe(false);
  });
});
