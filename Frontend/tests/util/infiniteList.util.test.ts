import type { ShouldRevalidateFunctionArgs } from "react-router";
import { describe, expect, it } from "vitest";
import {
  getLoadedPages,
  MAX_RESTORED_PAGES,
  shouldRevalidateIgnoringPages,
} from "~/util/infiniteList.util";

describe("getLoadedPages", () => {
  it.each([
    ["", 1],
    ["?pages=3", 3],
    ["?pages=0", 1],
    ["?pages=-2", 1],
    ["?pages=abc", 1],
    ["?pages=1000", MAX_RESTORED_PAGES],
  ])("reads %j as %i pages", (search, expected) => {
    expect(getLoadedPages(new URL(`https://example.com/list${search}`))).toBe(
      expected,
    );
  });

  it("accepts search parameters too", () => {
    expect(getLoadedPages(new URLSearchParams("pages=2"))).toBe(2);
  });
});

describe("shouldRevalidateIgnoringPages", () => {
  /** Asks whether going from one URL to another reruns the loader. */
  const revalidates = (from: string, to: string) =>
    shouldRevalidateIgnoringPages({
      currentUrl: new URL(`https://example.com${from}`),
      nextUrl: new URL(`https://example.com${to}`),
      defaultShouldRevalidate: true,
    } as ShouldRevalidateFunctionArgs);

  it("does not rerun the loader when only the loaded pages change", () => {
    expect(revalidates("/list?search=a", "/list?search=a&pages=2")).toBe(false);
  });

  it("reruns the loader when the search or a filter changes", () => {
    expect(revalidates("/list?search=a", "/list?search=b")).toBe(true);
    expect(revalidates("/list?year=2025", "/list?year=2024&pages=2")).toBe(
      true,
    );
  });

  it("reruns the loader when the same URL is opened again", () => {
    expect(revalidates("/list?pages=2", "/list?pages=2")).toBe(true);
  });
});
