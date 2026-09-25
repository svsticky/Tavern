import axios from "axios";
import { afterEach, beforeEach, describe, expect, it, vi } from "vitest";
import {
  ACTIVITIES_CACHE_KEY,
  ANNOUNCEMENTS_CACHE_KEY,
  cachedResource,
  clearResourceCache,
  ENROLLED_ACTIVITIES_CACHE_KEY,
  GROUP_MEMBERSHIPS_CACHE_KEY,
  getCachedResource,
  installCacheInvalidation,
  invalidateCachedResource,
  invalidateCacheForMutation,
  setCachedResource,
} from "~/util/resourceCache.util";

describe("resourceCache", () => {
  beforeEach(() => {
    vi.useFakeTimers();
    clearResourceCache();
  });
  afterEach(() => vi.useRealTimers());

  it("returns undefined for a miss", () => {
    expect(getCachedResource("nothing")).toBeUndefined();
  });

  it("returns what was set until it expires", () => {
    setCachedResource("k", [1, 2]);
    expect(getCachedResource("k")).toEqual([1, 2]);

    vi.advanceTimersByTime(59_000);
    expect(getCachedResource("k")).toEqual([1, 2]);

    vi.advanceTimersByTime(2_000);
    expect(getCachedResource("k")).toBeUndefined();
  });

  it("invalidates a key together with its per-user variants, but not unrelated keys", () => {
    setCachedResource(`${ENROLLED_ACTIVITIES_CACHE_KEY}:user-1`, "a");
    setCachedResource(`${ENROLLED_ACTIVITIES_CACHE_KEY}:user-2`, "b");
    setCachedResource(ACTIVITIES_CACHE_KEY, "c");

    invalidateCachedResource(ENROLLED_ACTIVITIES_CACHE_KEY);

    expect(
      getCachedResource(`${ENROLLED_ACTIVITIES_CACHE_KEY}:user-1`),
    ).toBeUndefined();
    expect(
      getCachedResource(`${ENROLLED_ACTIVITIES_CACHE_KEY}:user-2`),
    ).toBeUndefined();
    expect(getCachedResource(ACTIVITIES_CACHE_KEY)).toBe("c");
  });

  it("doesn't confuse a key that merely starts with another's name", () => {
    setCachedResource("announcements-archive", "x");
    invalidateCachedResource(ANNOUNCEMENTS_CACHE_KEY);
    expect(getCachedResource("announcements-archive")).toBe("x");
  });
});

describe("cachedResource", () => {
  beforeEach(() => clearResourceCache());

  it("fetches on a miss and serves the cached value afterwards", async () => {
    const fetcher = vi.fn(async () => ["fresh"]);

    expect(await cachedResource("k", fetcher)).toEqual(["fresh"]);
    expect(await cachedResource("k", fetcher)).toEqual(["fresh"]);

    expect(fetcher).toHaveBeenCalledTimes(1);
  });

  it("fetches again after the entry was invalidated", async () => {
    const fetcher = vi.fn(async () => ["fresh"]);
    await cachedResource(ACTIVITIES_CACHE_KEY, fetcher);

    invalidateCachedResource(ACTIVITIES_CACHE_KEY);
    await cachedResource(ACTIVITIES_CACHE_KEY, fetcher);

    expect(fetcher).toHaveBeenCalledTimes(2);
  });

  it("doesn't cache a failure, so the next call really retries", async () => {
    const fetcher = vi
      .fn<() => Promise<string>>()
      .mockRejectedValueOnce(new Error("boom"))
      .mockResolvedValueOnce("ok");

    await expect(cachedResource("k", fetcher)).rejects.toThrow("boom");
    expect(await cachedResource("k", fetcher)).toBe("ok");
  });

  it("doesn't write back a response that was in flight when its key was invalidated", async () => {
    let resolveFetch!: (value: string) => void;
    const slow = new Promise<string>((resolve) => {
      resolveFetch = resolve;
    });

    const pending = cachedResource(ACTIVITIES_CACHE_KEY, () => slow);
    // e.g. the user enrolls in something while the list is still loading
    invalidateCachedResource(ACTIVITIES_CACHE_KEY);
    resolveFetch("stale");

    expect(await pending).toBe("stale");
    expect(getCachedResource(ACTIVITIES_CACHE_KEY)).toBeUndefined();
  });

  it("applies the same protection to per-user keys", async () => {
    const key = `${GROUP_MEMBERSHIPS_CACHE_KEY}:user-1`;
    let resolveFetch!: (value: string) => void;
    const pending = cachedResource(
      key,
      () =>
        new Promise<string>((resolve) => {
          resolveFetch = resolve;
        }),
    );

    invalidateCachedResource(GROUP_MEMBERSHIPS_CACHE_KEY);
    resolveFetch("stale");
    await pending;

    expect(getCachedResource(key)).toBeUndefined();
  });
});

describe("invalidateCacheForMutation", () => {
  beforeEach(() => {
    clearResourceCache();
    for (const key of [
      ACTIVITIES_CACHE_KEY,
      `${ENROLLED_ACTIVITIES_CACHE_KEY}:u`,
      ANNOUNCEMENTS_CACHE_KEY,
      `${GROUP_MEMBERSHIPS_CACHE_KEY}:u`,
    ]) {
      setCachedResource(key, "cached");
    }
  });

  const cached = (key: string) => getCachedResource(key) !== undefined;

  it.each([
    ["post", "/activities"],
    ["patch", "/activities/42"],
    ["delete", "/activities/42"],
    ["POST", "/activities/42/poster"],
    ["post", "/enrollments"],
    ["put", "/enrollments/42/abc"],
    ["delete", "/enrollments/42/abc"],
  ])("%s %s drops activities and enrolled activities only", (method, url) => {
    invalidateCacheForMutation(method, url);

    expect(cached(ACTIVITIES_CACHE_KEY)).toBe(false);
    expect(cached(`${ENROLLED_ACTIVITIES_CACHE_KEY}:u`)).toBe(false);
    expect(cached(ANNOUNCEMENTS_CACHE_KEY)).toBe(true);
    expect(cached(`${GROUP_MEMBERSHIPS_CACHE_KEY}:u`)).toBe(true);
  });

  it.each([
    ["post", "/announcements"],
    ["put", "/announcements/7"],
    ["delete", "/announcements/7"],
  ])("%s %s drops announcements only", (method, url) => {
    invalidateCacheForMutation(method, url);

    expect(cached(ANNOUNCEMENTS_CACHE_KEY)).toBe(false);
    expect(cached(ACTIVITIES_CACHE_KEY)).toBe(true);
  });

  it.each([
    ["post", "/groupmemberships"],
    ["patch", "/groupmemberships/3"],
    ["delete", "/groupmemberships/3"],
    ["patch", "/groups/5"],
    ["post", "/groups/promote-board"],
  ])("%s %s drops group memberships only", (method, url) => {
    invalidateCacheForMutation(method, url);

    expect(cached(`${GROUP_MEMBERSHIPS_CACHE_KEY}:u`)).toBe(false);
    expect(cached(ACTIVITIES_CACHE_KEY)).toBe(true);
  });

  it("ignores reads", () => {
    invalidateCacheForMutation("get", "/activities");
    expect(cached(ACTIVITIES_CACHE_KEY)).toBe(true);
  });

  it("ignores mutations to unrelated resources", () => {
    invalidateCacheForMutation("post", "/payments");
    invalidateCacheForMutation("patch", "/members/abc");
    expect(cached(ACTIVITIES_CACHE_KEY)).toBe(true);
    expect(cached(ANNOUNCEMENTS_CACHE_KEY)).toBe(true);
  });

  it("copes with a query string, a missing leading slash and missing values", () => {
    invalidateCacheForMutation("delete", "activities/42?force=true");
    expect(cached(ACTIVITIES_CACHE_KEY)).toBe(false);

    setCachedResource(ACTIVITIES_CACHE_KEY, "again");
    invalidateCacheForMutation(undefined, "/activities");
    invalidateCacheForMutation("post", undefined);
    expect(cached(ACTIVITIES_CACHE_KEY)).toBe(true);
  });
});

describe("installCacheInvalidation", () => {
  const makeInstance = (status: number) => {
    const instance = axios.create({
      adapter: async (config) => {
        const response = {
          data: {},
          status,
          statusText: "",
          headers: {},
          config,
        };
        if (status >= 400)
          throw Object.assign(new Error("failed"), { response });
        return response;
      },
    });
    installCacheInvalidation(instance);
    return instance;
  };

  beforeEach(() => {
    clearResourceCache();
    setCachedResource(ACTIVITIES_CACHE_KEY, "cached");
  });

  it("drops the affected cache entries after a successful mutation", async () => {
    await makeInstance(200).request({ method: "post", url: "/activities/42" });
    expect(getCachedResource(ACTIVITIES_CACHE_KEY)).toBeUndefined();
  });

  it("leaves the cache alone for a successful read", async () => {
    await makeInstance(200).request({ method: "get", url: "/activities" });
    expect(getCachedResource(ACTIVITIES_CACHE_KEY)).toBe("cached");
  });

  it("leaves the cache alone when the mutation failed", async () => {
    await expect(
      makeInstance(500).request({ method: "delete", url: "/activities/42" }),
    ).rejects.toThrow();
    expect(getCachedResource(ACTIVITIES_CACHE_KEY)).toBe("cached");
  });
});
