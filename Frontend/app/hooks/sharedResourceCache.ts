/**
 * A tiny cross-page cache for data multiple routes fetch identically (e.g.
 * upcoming activities shown on both the home page and /activities). Callers
 * seed their state from `getCachedResource` and skip their fetch when it's
 * already set, write results back with `setCachedResource`, and mutation
 * flows call `invalidateCachedResource` so stale data isn't served after a
 * create/edit/delete.
 */
const resourceCache = new Map<string, unknown>();

export const ACTIVITIES_CACHE_KEY = "upcoming-activities";
export const ANNOUNCEMENTS_CACHE_KEY = "announcements";

export function getCachedResource<T>(key: string): T | undefined {
  return resourceCache.get(key) as T | undefined;
}

export function setCachedResource<T>(key: string, value: T) {
  resourceCache.set(key, value);
}

export function invalidateCachedResource(key: string) {
  resourceCache.delete(key);
}
